using System.Windows;
using StarterApp.Core.Settings;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace StarterApp.Services;

/// <summary>Тема: как в Windows, светлая или тёмная. Цвета — из WPF-UI (Fluent).</summary>
public sealed class ThemeService(SettingsStore settings)
{
    private Window? _watched;

    public event Action? Changed;

    public bool IsDark => ApplicationThemeManager.GetAppTheme() == ApplicationTheme.Dark;

    /// <summary>Применить тему из настроек. Для «system» окно следит за темой Windows.</summary>
    public void Apply(Window? window = null)
    {
        window ??= _watched;
        if (_watched is not null)
        {
            SystemThemeWatcher.UnWatch(_watched);
        }

        switch (settings.Current.Theme)
        {
            case "light":
                ApplicationThemeManager.Apply(ApplicationTheme.Light, WindowBackdropType.Mica);
                break;
            case "dark":
                ApplicationThemeManager.Apply(ApplicationTheme.Dark, WindowBackdropType.Mica);
                break;
            default:
                ApplicationThemeManager.ApplySystemTheme();
                if (window is not null)
                    SystemThemeWatcher.Watch(window, WindowBackdropType.Mica);
                break;
        }
        _watched = window;
        Changed?.Invoke();
    }
}
