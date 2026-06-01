namespace Scour.Services;

/// <summary>Shared filesystem helpers: size calculation and content-clearing used by the cleaners.</summary>
public static class FsUtil
{
    /// <summary>Recursive size of a file or directory; never throws (locked/denied items count as 0).</summary>
    public static long SizeOf(string path)
    {
        try
        {
            if (File.Exists(path)) return new FileInfo(path).Length;
            if (Directory.Exists(path)) return DirSize(new DirectoryInfo(path));
        }
        catch { }
        return 0;
    }

    public static long DirSize(DirectoryInfo dir)
    {
        long total = 0;
        try
        {
            foreach (var f in dir.EnumerateFiles())
            {
                try { total += f.Length; } catch { }
            }
            foreach (var d in dir.EnumerateDirectories())
            {
                try { if ((d.Attributes & FileAttributes.ReparsePoint) != 0) continue; } catch { continue; }
                total += DirSize(d);
            }
        }
        catch { }
        return total;
    }

    /// <summary>
    /// Deletes the contents of a directory (or the directory itself if <paramref name="removeRoot"/>),
    /// skipping anything locked or in use. Returns bytes freed and a count of items that couldn't be removed.
    /// </summary>
    public static (long freed, int skipped) ClearContents(string path, bool removeRoot)
    {
        long freed = 0;
        int skipped = 0;

        if (File.Exists(path))
        {
            try { var s = new FileInfo(path).Length; File.Delete(path); freed += s; }
            catch { skipped++; }
            return (freed, skipped);
        }

        if (!Directory.Exists(path)) return (0, 0);
        var dir = new DirectoryInfo(path);

        foreach (var f in SafeEnum(() => dir.EnumerateFiles()))
        {
            try { var s = f.Length; f.Delete(); freed += s; }
            catch { skipped++; }
        }
        foreach (var d in SafeEnum(() => dir.EnumerateDirectories()))
        {
            try
            {
                var s = DirSize(d);
                d.Delete(recursive: true);
                freed += s;
            }
            catch
            {
                // Partial: try to clear what we can inside it.
                var (innerFreed, innerSkipped) = ClearContents(d.FullName, removeRoot: false);
                freed += innerFreed;
                skipped += innerSkipped + 1;
            }
        }

        if (removeRoot)
        {
            try { dir.Delete(recursive: true); } catch { skipped++; }
        }
        return (freed, skipped);
    }

    private static IEnumerable<T> SafeEnum<T>(Func<IEnumerable<T>> source)
    {
        IEnumerable<T> items;
        try { items = source().ToList(); } catch { yield break; }
        foreach (var i in items) yield return i;
    }
}
