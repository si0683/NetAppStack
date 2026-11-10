using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using StarterApp.Core;
using StarterApp.Core.Collecting;
using StarterApp.Core.Storage;
using StarterApp.Services;

namespace StarterApp.ViewModels;

/// <summary>Главное окно: кнопка сбора, строка состояния, меню.</summary>
public sealed partial class MainViewModel : ObservableObject
{
    private readonly CollectorService _collector;
    private readonly ItemStore _store;
    private readonly AppPaths _paths;
    private readonly ILogger<MainViewModel> _log;

    public MainViewModel(CollectorService collector, ItemStore store, AppPaths paths,
        ItemsViewModel items, LogsViewModel logs, ILogger<MainViewModel> log)
    {
        _collector = collector;
        _store = store;
        _paths = paths;
        _log = log;
        Items = items;
        Logs = logs;

        Apply(collector.State);
        collector.StateChanged += s => Application.Current?.Dispatcher.InvokeAsync(() => Apply(s));
    }

    public string Title => AppInfo.Title;

    public ItemsViewModel Items { get; }

    public LogsViewModel Logs { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RunButtonText))]
    private bool _isRunning;

    [ObservableProperty]
    private string _statusText = "";

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private bool _isIndeterminate;

    public string RunButtonText => IsRunning ? "Отмена" : "Обновить сейчас";

    private void Apply(CollectorState s)
    {
        IsRunning = s.IsRunning;
        Progress = (s.Progress ?? 0) * 100;
        IsIndeterminate = s.IsRunning && s.Progress is null;

        string text = s.Message;
        if (!s.IsRunning && s.LastRunAt is { } last)
            text += $" · {last.ToLocalTime():HH:mm}";
        if (!s.IsRunning && s.NextRunAt is { } next)
            text += $" · следующий сбор в {next.ToLocalTime():HH:mm}";
        StatusText = text;
    }

    [RelayCommand]
    private void RunOrCancel()
    {
        if (IsRunning)
            _collector.CancelRun();
        else
            _collector.RunNow();
    }

    [RelayCommand]
    private void OpenDataFolder() => Shell.OpenFolder(_paths.Root);

    [RelayCommand]
    private void ExportExcel() => Export("xlsx");

    [RelayCommand]
    private void ExportCsv() => Export("csv");

    private void Export(string extension)
    {
        string path = Path.Combine(_paths.Reports, $"data-{DateTime.Now:yyyyMMdd-HHmm}.{extension}");
        try
        {
            Items.Export(path);
            Shell.SelectFile(path);
        }
        catch (IOException ex)
        {
            _log.LogError(ex, "Выгрузка не удалась");
            MessageBox.Show("Файл не записан — возможно, он открыт в Excel.\n\n" + ex.Message, AppInfo.Title,
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    [RelayCommand]
    private async Task RecollectAll()
    {
        var answer = MessageBox.Show(
            "Удалить все собранные записи вместе со статусами и заметками и собрать заново?",
            AppInfo.Title, MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
        if (answer != MessageBoxResult.Yes)
            return;

        _collector.CancelRun();
        await _store.ClearAsync();
        _log.LogWarning("Все данные удалены — сбор заново");
        await Items.LoadAsync();
        _collector.RunNow();
    }
}
