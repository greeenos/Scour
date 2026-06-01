using Scour.Models;

namespace Scour.Services;

/// <summary>
/// Free-form filesystem search by name fragment — the Windows analogue of Scour's File Search.
/// Streams matches back via a callback so the UI can fill incrementally.
/// </summary>
public sealed class FileSearchService
{
    public async Task SearchAsync(string root, string query, bool computeSize,
        Action<FileItem> onMatch, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query) || !Directory.Exists(root)) return;
        var needle = query.PearFormat();

        await Task.Run(() =>
        {
            int count = 0;
            foreach (var path in EnumerateAll(root, ct))
            {
                ct.ThrowIfCancellationRequested();
                var name = Path.GetFileName(path.TrimEnd('\\'));
                if (name.PearFormat().Contains(needle))
                {
                    bool isDir = Directory.Exists(path);
                    var item = new FileItem
                    {
                        Path = path,
                        Kind = isDir ? LeftoverKind.Folder : LeftoverKind.File,
                        IsSelected = false,
                        Size = computeSize ? FsUtil.SizeOf(path) : 0
                    };
                    onMatch(item);
                    if (++count >= 2000) { ConsoleManager.Shared.Append("Search capped at 2000 results."); break; }
                }
            }
            ConsoleManager.Shared.Append($"Search complete: {count} match(es).");
        }, ct);
    }

    private static IEnumerable<string> EnumerateAll(string root, CancellationToken ct)
    {
        var stack = new Stack<string>();
        stack.Push(root);
        while (stack.Count > 0)
        {
            if (ct.IsCancellationRequested) yield break;
            var dir = stack.Pop();

            string[] subDirs = Array.Empty<string>();
            try { subDirs = Directory.GetDirectories(dir); } catch { }
            foreach (var d in subDirs)
            {
                try { if ((File.GetAttributes(d) & FileAttributes.ReparsePoint) != 0) continue; } catch { continue; }
                stack.Push(d);
                yield return d;
            }

            string[] files = Array.Empty<string>();
            try { files = Directory.GetFiles(dir); } catch { }
            foreach (var f in files) yield return f;
        }
    }
}
