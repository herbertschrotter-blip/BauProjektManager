using Serilog;

namespace BauProjektManager.Infrastructure.Persistence;

/// <summary>
/// Bauteil-Aliasse (DB-SCHEMA 4.11, ADR-065 P.14, BPM-121.03): merkt sich, welche Dateinamen-Schreibweise
/// (z. B. „H2“) auf welches Bauteil zeigt. Exakter Vergleich nach Normalisierung
/// (<see cref="Services.PlanValueNormalizer.NormalizeForMatch"/>), nur vom User bestaetigt — kein Fuzzy, kein Auto-Create.
/// </summary>
public partial class ProjectDatabase
{
    /// <summary>Normalisierte Alias-Schreibweise → building_part_id (nur aktive Aliasse).</summary>
    public IReadOnlyDictionary<string, string> GetBuildingPartAliases(string projectId)
    {
        var cmd = GetConnection().CreateCommand();
        cmd.CommandText = """
            SELECT normalized_alias_value, building_part_id FROM building_part_aliases
            WHERE project_id = @pid AND is_deleted = 0
            """;
        cmd.Parameters.AddWithValue("@pid", projectId);
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            result[reader.GetString(0)] = reader.GetString(1);
        return result;
    }

    /// <summary>
    /// building_part_id → gemerkte Schreibweisen im Original (z. B. „H2“), alphabetisch — fuer den Claude-Sortierauftrag
    /// (kontext.json, Feld <c>schreibweisen</c>). Nur aktive Aliasse.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> GetBuildingPartAliasValues(string projectId)
    {
        using var cmd = GetConnection().CreateCommand();
        cmd.CommandText = """
            SELECT building_part_id, alias_value FROM building_part_aliases
            WHERE project_id = @pid AND is_deleted = 0
            ORDER BY alias_value
            """;
        cmd.Parameters.AddWithValue("@pid", projectId);
        var result = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var partId = reader.GetString(0);
            if (!result.TryGetValue(partId, out var values))
                result[partId] = values = [];
            values.Add(reader.GetString(1));
        }
        return result.ToDictionary(e => e.Key, e => (IReadOnlyList<string>)e.Value, StringComparer.Ordinal);
    }

    /// <summary>
    /// Merkt einen Alias fuer ein Bauteil. True = neu angelegt, false = gab es schon fuer dieses Bauteil.
    /// Zeigt dieselbe Schreibweise schon auf ein anderes Bauteil, wirft das <see cref="InvalidOperationException"/>
    /// (UNIQUE je Projekt) — ein Alias wird nie still umgebogen.
    /// </summary>
    public bool AddBuildingPartAlias(string projectId, string buildingPartId, string aliasValue)
    {
        var normalized = _normalizer.NormalizeForMatch(aliasValue);
        if (normalized.Length == 0)
            throw new ArgumentException("Alias ist leer.", nameof(aliasValue));

        if (GetBuildingPartAliases(projectId).TryGetValue(normalized, out var existingPartId))
        {
            if (existingPartId == buildingPartId)
                return false;
            throw new InvalidOperationException($"„{aliasValue.Trim()}“ ist schon einem anderen Bauteil zugeordnet.");
        }

        var utcNow = DateTime.UtcNow.ToString("o");
        var user = _userContext.DisplayName;
        var cmd = GetConnection().CreateCommand();
        cmd.CommandText = """
            INSERT INTO building_part_aliases (id, project_id, building_part_id, alias_value, normalized_alias_value,
                created_at, created_by, last_modified_at, last_modified_by, sync_version)
            VALUES (@id, @pid, @bpid, @alias, @norm, @now, @user, @now, @user, 0)
            """;
        cmd.Parameters.AddWithValue("@id", _idGenerator.NewId());
        cmd.Parameters.AddWithValue("@pid", projectId);
        cmd.Parameters.AddWithValue("@bpid", buildingPartId);
        cmd.Parameters.AddWithValue("@alias", aliasValue.Trim());
        cmd.Parameters.AddWithValue("@norm", normalized);
        cmd.Parameters.AddWithValue("@now", utcNow);
        cmd.Parameters.AddWithValue("@user", user);
        Log.Verbose("Executing SQL: {Operation} on {Table}", "INSERT", "building_part_aliases");
        cmd.ExecuteNonQuery();
        return true;
    }
}
