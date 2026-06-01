using Scour.Models;

namespace Scour.Services;

/// <summary>
/// Pre-computed, normalized identifiers for one app — the Windows analogue of AppPathFinder's
/// cachedIdentifiers tuple. Built once per scan and reused in the hot matching path.
/// </summary>
public sealed class AppIdentifiers
{
    public string FormattedName { get; }
    public string? FormattedNameStripped { get; }
    public string NameLettersOnly { get; }
    public string FormattedPublisher { get; }
    public string FormattedInstallFolder { get; }
    /// <summary>Distinct, meaningful word tokens from the display name (PearFormatted, len >= 3).</summary>
    public IReadOnlyList<string> NameTokens { get; }

    /// <summary>Minimum length a token must have to be used for "contains" matching, to curb
    /// false positives from short/generic names.</summary>
    public const int MinContainsLen = 4;

    public AppIdentifiers(AppInfo app)
    {
        FormattedName = app.DisplayName.PearFormat();
        NameLettersOnly = new string(FormattedName.Where(char.IsLetter).ToArray());

        var stripped = app.DisplayName.StripTrailingDigits().PearFormat();
        FormattedNameStripped = (stripped != FormattedName && stripped.Length > 0) ? stripped : null;

        FormattedPublisher = app.Publisher.PearFormat();

        var folder = string.IsNullOrEmpty(app.InstallLocation)
            ? ""
            : Path.GetFileName(app.InstallLocation.TrimEnd('\\', '/'));
        FormattedInstallFolder = folder.PearFormat();

        NameTokens = app.DisplayName
            .Split(new[] { ' ', '-', '_', '.', ',', '(', ')', '[', ']' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.PearFormat())
            .Where(t => t.Length >= 3)
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }
}
