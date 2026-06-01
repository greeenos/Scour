using System.Text;
using System.Text.RegularExpressions;

namespace Scour;

/// <summary>
/// Cross-cutting helpers. <see cref="PearFormat"/> is the direct port of the macOS String.pearFormat()
/// extension: keep only alphanumerics, lowercase. It is the basis of all name matching.
/// </summary>
public static partial class Util
{
    /// <summary>
    /// Normalize a name for matching: strip every non-alphanumeric character and lowercase.
    /// "Visual Studio Code" -> "visualstudiocode", "com.foo.Bar-1" -> "comfoobar1".
    /// If the input is non-empty but normalizes to empty, returns the original (avoids
    /// false matches against an empty string, matching the Swift behavior).
    /// </summary>
    public static string PearFormat(this string? input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        var sb = new StringBuilder(input.Length);
        foreach (var ch in input)
            if (char.IsLetterOrDigit(ch))
                sb.Append(char.ToLowerInvariant(ch));
        var result = sb.ToString();
        return result.Length == 0 ? input : result;
    }

    [GeneratedRegex(@"\s+\d+(\.\d+)*\s*$")]
    private static partial Regex TrailingVersionRegex();

    /// <summary>
    /// Strips a trailing version/number from a display name.
    /// "Bartender 6" -> "Bartender", "Firefox 120.0" -> "Firefox".
    /// </summary>
    public static string StripTrailingDigits(this string input) =>
        TrailingVersionRegex().Replace(input ?? "", "").Trim();

    /// <summary>Human-readable byte count, file style (1024-based), e.g. "12.4 MB".</summary>
    public static string FormatBytes(long bytes)
    {
        if (bytes <= 0) return "Zero KB";
        string[] units = { "bytes", "KB", "MB", "GB", "TB", "PB" };
        double size = bytes;
        int unit = 0;
        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }
        return unit == 0 ? $"{(long)size} {units[unit]}" : $"{size:0.#} {units[unit]}";
    }

    /// <summary>Expand a leading environment token; tolerant of nulls.</summary>
    public static string Expand(string? path) =>
        string.IsNullOrEmpty(path) ? "" : Environment.ExpandEnvironmentVariables(path);
}
