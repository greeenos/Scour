using System.Text.Json;

namespace Scour.Services;

/// <summary>
/// Records what was removed so the user can review (and, for recycled items, restore from the
/// Recycle Bin). Windows analogue of Scour's Delete/Undo history.
/// </summary>
public sealed class HistoryService
{
    private static readonly string Dir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Scour");
    private static readonly string FilePath = Path.Combine(Dir, "history.json");
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    public sealed class Entry
    {
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
        public string AppName { get; set; } = "";
        public int ItemCount { get; set; }
        public long BytesFreed { get; set; }
        public bool Recycled { get; set; }
        public List<string> Paths { get; set; } = new();
    }

    public static HistoryService Shared { get; } = new();

    public List<Entry> Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<List<Entry>>(File.ReadAllText(FilePath)) ?? new();
        }
        catch (Exception ex) { ConsoleManager.Shared.Append($"Failed to read history: {ex.Message}"); }
        return new();
    }

    public void Add(Entry entry)
    {
        try
        {
            var all = Load();
            all.Insert(0, entry);
            if (all.Count > 500) all = all.Take(500).ToList();
            Directory.CreateDirectory(Dir);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(all, JsonOpts));
        }
        catch (Exception ex) { ConsoleManager.Shared.Append($"Failed to write history: {ex.Message}"); }
    }

    public void Clear()
    {
        try { if (File.Exists(FilePath)) File.Delete(FilePath); } catch { }
    }
}
