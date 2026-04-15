namespace BauProjektManager.Domain.Interfaces;

/// <summary>
/// Generates globally unique IDs (ULID) for all database entities.
/// ADR-039 v2: ULID as TEXT PRIMARY KEY for ALL tables.
/// </summary>
public interface IIdGenerator
{
    /// <summary>
    /// Returns a new ULID as string (26 characters, lexicographically sortable).
    /// </summary>
    string NewId();
}
