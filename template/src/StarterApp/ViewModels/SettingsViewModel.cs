using CommunityToolkit.Mvvm.ComponentModel;
using StarterApp.Core.Settings;
using StarterApp.Services;

namespace StarterApp.ViewModels;

/// <summary>
/// Окно «Настройки». Работает с копией значений: «Отмена» ничего не меняет.
/// Новая настройка: свойство в AppSettings → поле здесь (Load/Save) → строка в SettingsWindow.xaml.
/// </summary>
public sealed partial class SettingsViewModel(SettingsStore store, ThemeService theme) : ObservableObject
{
    public IReadOnlyList<ThemeOption> Themes { get; } =
    [
        new("system", "Как в Windows"),
        new("light", "Светлая"),
        new("dark", "Тёмная"),
    ];

    [ObservableProperty] private double? _updateIntervalMinutes; // NumberBox работает с double?
    [ObservableProperty] private ThemeOption? _theme;
    [ObservableProperty] private bool _demoEnabled;
    [ObservableProperty] private string _webUrl = "";
    [ObservableProperty] private string _webItemSelector = "";
    [ObservableProperty] private string _webLinkSelector = "";
    [ObservableProperty] private string _webDateSelector = "";
    [ObservableProperty] private string _webCategorySelector = "";
#if (postgres)
    [ObservableProperty] private string _postgresConnection = "";
#endif
#if (tray)
    [ObservableProperty] private bool _minimizeToTray;
#endif
#if (updates)
    [ObservableProperty] private bool _checkUpdates;
#endif

    public void Load()
    {
        var s = store.Current;
        UpdateIntervalMinutes = s.UpdateIntervalMinutes;
        Theme = Themes.FirstOrDefault(t => t.Key == s.Theme) ?? Themes[0];
        DemoEnabled = s.DemoEnabled;
        WebUrl = s.WebList.Url;
        WebItemSelector = s.WebList.ItemSelector;
        WebLinkSelector = s.WebList.LinkSelector;
        WebDateSelector = s.WebList.DateSelector;
        WebCategorySelector = s.WebList.CategorySelector;
#if (postgres)
        PostgresConnection = s.PostgresConnection;
#endif
#if (tray)
        MinimizeToTray = s.MinimizeToTray;
#endif
#if (updates)
        CheckUpdates = s.CheckUpdates;
#endif
    }

    public void Save()
    {
        var s = store.Current;
        s.UpdateIntervalMinutes = (int)Math.Clamp(Math.Round(UpdateIntervalMinutes ?? 60), 1, 24 * 60);
        s.Theme = Theme?.Key ?? "system";
        s.DemoEnabled = DemoEnabled;
        s.WebList.Url = WebUrl.Trim();
        s.WebList.ItemSelector = WebItemSelector.Trim();
        s.WebList.LinkSelector = WebLinkSelector.Trim();
        s.WebList.DateSelector = WebDateSelector.Trim();
        s.WebList.CategorySelector = WebCategorySelector.Trim();
#if (postgres)
        s.PostgresConnection = PostgresConnection.Trim();
#endif
#if (tray)
        s.MinimizeToTray = MinimizeToTray;
#endif
#if (updates)
        s.CheckUpdates = CheckUpdates;
#endif
        store.Save();
        theme.Apply();
    }
}

public sealed record ThemeOption(string Key, string Title);
