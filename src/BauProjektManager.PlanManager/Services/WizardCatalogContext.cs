using BauProjektManager.Domain.Interfaces;

namespace BauProjektManager.PlanManager.Services;

/// <summary>
/// Statischer Halter fuer den <see cref="ISegmentTypeCatalog"/>, damit
/// XAML-Converter (die nicht ueber DI erreichbar sind) Catalog-Lookups
/// durchfuehren koennen.
/// </summary>
/// <remarks>
/// Wird in <c>App.xaml.cs</c> nach dem DI-Build initialisiert
/// (<c>WizardCatalogContext.Initialize(catalog)</c>). Tests koennen den
/// Catalog explizit setzen oder zuruecksetzen.
/// BPM-108 Phase C.
/// </remarks>
public static class WizardCatalogContext
{
    /// <summary>Aktiver Katalog fuer Wizard-XAML-Converter. Null bis initialisiert.</summary>
    public static ISegmentTypeCatalog? Catalog { get; private set; }

    public static void Initialize(ISegmentTypeCatalog? catalog)
    {
        Catalog = catalog;
    }

    public static void Reset() => Catalog = null;
}
