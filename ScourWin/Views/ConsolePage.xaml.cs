using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Scour.Services;

namespace Scour.Views;

public sealed partial class ConsolePage : Page
{
    public ConsolePage()
    {
        InitializeComponent();
        LinesControl.ItemsSource = ConsoleManager.Shared.Lines;
        ConsoleManager.Shared.LineAppended += OnLineAppended;
        Unloaded += (_, _) => ConsoleManager.Shared.LineAppended -= OnLineAppended;
    }

    private void OnLineAppended(string _) =>
        DispatcherQueue.TryEnqueue(() => Scroller.ChangeView(null, Scroller.ScrollableHeight, null));

    private void OnClear(object sender, RoutedEventArgs e) => ConsoleManager.Shared.Clear();
}
