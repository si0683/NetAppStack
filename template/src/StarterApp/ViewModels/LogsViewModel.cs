using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StarterApp.Core;
using StarterApp.Logging;
using StarterApp.Services;

namespace StarterApp.ViewModels;

/// <summary>Вкладка «Логи»: что программа делает. Полные логи — в data/logs.</summary>
public sealed partial class LogsViewModel : ObservableObject
{
    private const int MaxLines = 3000;

    private readonly LogFeed _feed;
    private readonly AppPaths _paths;

    public LogsViewModel(LogFeed feed, AppPaths paths)
    {
        _feed = feed;
        _paths = paths;
        View = CollectionViewSource.GetDefaultView(Lines);
        View.Filter = o => !OnlyProblems || ((LogLine)o).Level >= Serilog.Events.LogEventLevel.Warning;

        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        timer.Tick += (_, _) => Pull();
        timer.Start();
    }

    public ObservableCollection<LogLine> Lines { get; } = [];

    public ICollectionView View { get; }

    /// <summary>Добавились строки — вкладка прокручивает вниз.</summary>
    public event Action<LogLine>? LineAdded;

    [ObservableProperty]
    private bool _onlyProblems;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Header))]
    private int _errors;

    /// <summary>Заголовок вкладки: «Логи» или «Логи (2)», если есть ошибки.</summary>
    public string Header => Errors > 0 ? $"Логи ({Errors})" : "Логи";

    partial void OnOnlyProblemsChanged(bool value) => View.Refresh();

    private void Pull()
    {
        var lines = _feed.Drain();
        if (lines.Count == 0)
            return;

        foreach (var line in lines)
        {
            Lines.Add(line);
            if (line.IsError)
                Errors++;
        }
        while (Lines.Count > MaxLines)
            Lines.RemoveAt(0);
        LineAdded?.Invoke(lines[^1]);
    }

    [RelayCommand]
    private void Clear()
    {
        Lines.Clear();
        Errors = 0;
    }

    [RelayCommand]
    private void OpenFolder() => Shell.OpenFolder(_paths.Logs);
}
