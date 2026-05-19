namespace BauProjektManager.Domain.Enums.PlanManager;

/// <summary>
/// Health-Marker eines <see cref="Models.PlanManager.RecognitionProfile"/> (BPM-108 Phase B).
/// Wird beim Laden ermittelt und bestimmt ob Auto-Import erlaubt ist.
/// </summary>
public enum ProfileHealth
{
    /// <summary>Profil ist vollstaendig nutzbar.</summary>
    Valid = 0,

    /// <summary>Profil referenziert eine oder mehrere unbekannte <c>fieldTypeId</c>s.
    /// Recognizer darf matchen, Auto-Import ist blockiert falls die fehlende ID in
    /// <c>identityFields</c>, <c>folderHierarchy</c>, <c>renameSchema</c> oder
    /// <c>indexExtraction</c> verwendet wird.</summary>
    MissingSegmentTypes = 1,

    /// <summary>Profil hat <c>schemaVersion != 4</c>. Wird vom Loader verworfen.
    /// Reserviert fuer Diagnose-Zwecke (z. B. DevTool-Archive-Bericht).</summary>
    OutdatedSchema = 2,

    /// <summary>Profil enthaelt mindestens eine <see cref="Models.PlanManager.RecognitionRule"/>,
    /// die <c>IsValid()</c> nicht erfuellt. Wird vom Loader verworfen.</summary>
    InvalidRecognitionRules = 3
}
