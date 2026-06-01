using Scour.Models;

namespace Scour.Services;

/// <summary>
/// Finds and clears developer tool caches (npm, pip, gradle, NuGet, Cargo, Go, Yarn, etc.) —
/// the Windows analogue of Scour's Development Environment Manager.
/// </summary>
public sealed class DevCacheCleaner
{
    private static string Home => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    private static string Local => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    private static string Roaming => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    private static string Env(string v) => Environment.GetEnvironmentVariable(v) ?? "";

    public List<CleanupCategory> BuildCategories()
    {
        string P(params string[] parts) => Path.Combine(parts);

        var cats = new List<CleanupCategory>
        {
            new() { Name = "npm cache", Description = "Node package manager cache", Paths = { P(Roaming, "npm-cache"), P(Local, "npm-cache") } },
            new() { Name = "Yarn cache", Description = "Yarn package cache", Paths = { P(Local, "Yarn", "Cache"), P(Home, "AppData", "Local", "Yarn", "Cache") } },
            new() { Name = "pnpm store", Description = "pnpm content-addressable store", Paths = { P(Local, "pnpm", "store") } },
            new() { Name = "pip cache", Description = "Python pip download cache", Paths = { P(Local, "pip", "Cache") } },
            new() { Name = "NuGet packages", Description = ".NET global package cache", Paths = { P(Home, ".nuget", "packages"), P(Local, "NuGet", "Cache") } },
            new() { Name = "Gradle caches", Description = "Java/Android Gradle caches", Paths = { P(Home, ".gradle", "caches") } },
            new() { Name = "Maven repository", Description = "Java Maven local repository", Paths = { P(Home, ".m2", "repository") } },
            new() { Name = "Cargo registry", Description = "Rust crate cache", Paths = { P(Home, ".cargo", "registry") } },
            new() { Name = "Go module cache", Description = "Go modules/build cache", Paths = { P(Home, "go", "pkg", "mod"), P(Local, "go-build") } },
            new() { Name = "Gem cache", Description = "Ruby gem cache", Paths = { P(Home, ".gem") } },
            new() { Name = "Composer cache", Description = "PHP Composer cache", Paths = { P(Local, "Composer") } },
            new() { Name = "Docker (CLI) cache", Description = "Docker config/cli cache", Paths = { P(Home, ".docker", "cli-plugins") } },
            new() { Name = "Visual Studio temp", Description = "VS component/MEF cache", Paths = { P(Local, "Microsoft", "VisualStudio") } },
            new() { Name = ".NET CLI temp", Description = "dotnet temp & NuGet http cache", Paths = { P(Local, "NuGet", "v3-cache"), P(Local, "Temp", ".net") } },
        };

        return cats.Where(c => c.ExistingPaths.Any()).ToList();
    }

    public async Task SizeAsync(IEnumerable<CleanupCategory> cats, CancellationToken ct = default)
    {
        foreach (var c in cats)
        {
            ct.ThrowIfCancellationRequested();
            c.IsScanning = true;
            var paths = c.ExistingPaths.ToList();
            c.Size = await Task.Run(() => paths.Sum(FsUtil.SizeOf), ct);
            c.IsScanning = false;
        }
    }

    public async Task<(long freed, int skipped)> CleanAsync(IEnumerable<CleanupCategory> cats, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            long freed = 0; int skipped = 0;
            foreach (var c in cats.Where(c => c.IsSelected))
            {
                ct.ThrowIfCancellationRequested();
                ConsoleManager.Shared.Append($"Clearing {c.Name}...");
                foreach (var p in c.ExistingPaths)
                {
                    var (f, s) = FsUtil.ClearContents(p, removeRoot: false);
                    freed += f; skipped += s;
                }
                c.Size = c.ExistingPaths.Sum(FsUtil.SizeOf);
            }
            ConsoleManager.Shared.Append($"Dev cleanup freed {Util.FormatBytes(freed)} ({skipped} skipped).");
            return (freed, skipped);
        }, ct);
    }
}
