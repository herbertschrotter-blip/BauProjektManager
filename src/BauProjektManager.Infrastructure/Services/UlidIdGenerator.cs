using BauProjektManager.Domain.Interfaces;

namespace BauProjektManager.Infrastructure.Services;

/// <summary>
/// ULID-based ID generator. Produces globally unique, chronologically sortable IDs.
/// Used by all database services for entity ID generation (ADR-039 v2).
/// </summary>
public class UlidIdGenerator : IIdGenerator
{
    public string NewId() => Ulid.NewUlid().ToString();
}
