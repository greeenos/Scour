namespace Scour.Models;

/// <summary>
/// Mirrors Scour's SearchSensitivityLevel. Controls how aggressively the leftover
/// scanner matches file/folder/registry names against an app's identifiers.
/// </summary>
public enum SearchSensitivity
{
    /// <summary>Exact, whole-name matches only. Fewest false positives.</summary>
    Strict,

    /// <summary>"Contains" matching on name + publisher + key-name components.</summary>
    Enhanced,

    /// <summary>Adds publisher/company and version-stripped fuzzy matching. Most aggressive.</summary>
    Deep
}
