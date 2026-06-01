using Scour.Models;

namespace Scour.Services;

/// <summary>
/// The matching decision for a single scanned item name — direct port of AppPathFinder.specificCondition,
/// adapted to Windows identifiers and the three sensitivity levels.
/// </summary>
public static class NameMatcher
{
    /// <param name="normalizedName">The scanned item's name, already PearFormatted.</param>
    public static bool Matches(string normalizedName, AppIdentifiers id, SearchSensitivity level)
    {
        if (normalizedName.Length == 0) return false;
        bool strict = level == SearchSensitivity.Strict;

        // --- Display name ---
        if (id.FormattedName.Length > 0)
        {
            if (strict)
            {
                if (normalizedName == id.FormattedName) return true;
            }
            else if (id.FormattedName.Length >= AppIdentifiers.MinContainsLen &&
                     normalizedName.Contains(id.FormattedName))
            {
                return true;
            }
        }

        // --- Install folder name ---
        if (id.FormattedInstallFolder.Length > 0)
        {
            if (strict)
            {
                if (normalizedName == id.FormattedInstallFolder) return true;
            }
            else if (id.FormattedInstallFolder.Length >= AppIdentifiers.MinContainsLen &&
                     normalizedName.Contains(id.FormattedInstallFolder))
            {
                return true;
            }
        }

        // --- Letters-only name (e.g. "vlc3" -> "vlc") ---
        if (id.NameLettersOnly.Length > 0)
        {
            if (strict)
            {
                if (normalizedName == id.NameLettersOnly) return true;
            }
            else if (id.NameLettersOnly.Length >= AppIdentifiers.MinContainsLen &&
                     normalizedName.Contains(id.NameLettersOnly))
            {
                return true;
            }
        }

        if (strict) return false;

        // --- Enhanced/Deep: version-stripped name (e.g. "Bartender 6" -> "bartender") ---
        if (id.FormattedNameStripped is { Length: >= AppIdentifiers.MinContainsLen } stripped &&
            normalizedName.Contains(stripped))
            return true;

        // --- Deep only: publisher / company ---
        if (level == SearchSensitivity.Deep &&
            id.FormattedPublisher.Length >= AppIdentifiers.MinContainsLen &&
            !WindowsLocations.ProtectedFolderNames.Contains(id.FormattedPublisher) &&
            normalizedName.Contains(id.FormattedPublisher))
            return true;

        return false;
    }
}
