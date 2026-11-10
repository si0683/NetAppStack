using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using StarterApp.Logging;
using StarterApp.ViewModels;

namespace StarterApp.Views;

public partial class LogsView : UserControl
{
    public LogsView()
    {
        InitializeComponent();
        DataContextChanged += (_, e) =>
        {
            if (e.OldValue is LogsViewModel old)
                old.LineAdded -= ScrollToEnd;
            if (e.NewValue is LogsViewModel vm)
                vm.LineAdded += ScrollToEnd;
        };
        List.CommandBindings.Add(new CommandBinding(ApplicationCommands.Copy, (_, _) => CopySelected()));
    }

    private void ScrollToEnd(LogLine line)
    {
        // прокручиваем, только если ничего не выделено — иначе мешаем читать
        if (List.SelectedItems.Count == 0 && IsVisible)
            List.ScrollIntoView(line);
    }

    private void CopySelected()
    {
        var text = new StringBuilder();
        foreach (LogLine line in List.SelectedItems.Cast<LogLine>().OrderBy(l => l.Time))
            text.AppendLine($"{line.Time:yyyy-MM-dd HH:mm:ss} {line.LevelText} {line.Source}: {line.Message}");
        if (text.Length > 0)
            Clipboard.SetText(text.ToString());
    }
}
