using BauProjektManager.Domain.Models.PlanManager;

namespace BauProjektManager.Domain.Interfaces;

/// <summary>
/// Manages RecognitionProfiles per project.
/// Profiles are stored as individual JSON files in .bpm/profiles/ (ADR-046).
/// </summary>
public interface IProfileManager
{
    /// <summary>
    /// Loads all profiles for a project from .bpm/profiles/*.json.
    /// </summary>
    List<RecognitionProfile> LoadAll(string projectRootPath);

    /// <summary>
    /// Loads a single profile by ID.
    /// </summary>
    RecognitionProfile? LoadById(string projectRootPath, string profileId);

    /// <summary>
    /// Saves a profile to .bpm/profiles/{id}.json.
    /// Generates a new ULID if the profile has no ID yet.
    /// </summary>
    void Save(string projectRootPath, RecognitionProfile profile);

    /// <summary>
    /// Deletes a profile by ID.
    /// </summary>
    bool Delete(string projectRootPath, string profileId);

    /// <summary>
    /// Builds a RecognitionProfile from the current wizard state.
    /// Called by ProfileWizardViewModel.SaveProfile().
    /// </summary>
    RecognitionProfile BuildFromWizard(
        string documentTypeName,
        string targetFolder,
        IndexSourceType indexSource,
        bool indexModeOptional,
        bool indexCaseInsensitive,
        List<FileNameSegment> segments,
        List<string> delimiters,
        List<string> folderHierarchy,
        List<RecognitionRule> recognition,
        int recognitionPriority,
        string? existingProfileId = null);
}
