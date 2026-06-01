using System.Collections.ObjectModel;
using Microsoft.UI.Dispatching;

namespace Scour.Services;

/// <summary>
/// Global, observable activity log. Windows analogue of GlobalConsoleManager — surfaces
/// what the scanner/remover is doing so the UI can show a live console.
/// </summary>
public sealed class ConsoleManager
{
    public static ConsoleManager Shared { get; } = new();

    private DispatcherQueue? _dispatcher;

    public ObservableCollection<string> Lines { get; } = new();

    public event Action<string>? LineAppended;

    private ConsoleManager() { }

    /// <summary>Bind the UI dispatcher so appends from background threads marshal correctly.</summary>
    public void AttachDispatcher(DispatcherQueue dispatcher) => _dispatcher = dispatcher;

    public void Append(string message)
    {
        var stamped = $"[{DateTime.Now:HH:mm:ss}] {message.TrimEnd()}";
        void Add()
        {
            Lines.Add(stamped);
            if (Lines.Count > 2000) Lines.RemoveAt(0);
            LineAppended?.Invoke(stamped);
        }

        if (_dispatcher is { } d && !d.HasThreadAccess)
            d.TryEnqueue(Add);
        else
            Add();
    }

    public void Clear()
    {
        if (_dispatcher is { } d && !d.HasThreadAccess)
            d.TryEnqueue(Lines.Clear);
        else
            Lines.Clear();
    }
}
