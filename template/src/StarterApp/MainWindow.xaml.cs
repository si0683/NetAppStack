using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using StarterApp.Core.Settings;
using StarterApp.Services;
using StarterApp.ViewModels;
using StarterApp.Views;

namespace StarterApp;

public partial class MainWindow : Wpf.Ui.Controls.FluentWindow
{
    private readonly MainViewModel _vm;
    private readonly SettingsStore _settings;
    private readonly IServiceProvider _services;
#if (tray)
    private readonly TrayService _tray;
    private bool _exiting;
#endif

    public MainWindow(MainViewModel vm, SettingsStore settings, IServiceProvider services)
    {
        _vm = vm;
        _settings = settings;
        _services = services;
#if (tray)
        _tray = services.GetRequiredService<TrayService>();
#endif
        DataContext = vm;
        InitializeComponent();
        RestorePlacement();

        Loaded += OnLoaded;
        Closing += OnClosing;
        PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.F1)
            {
                OnHelp(this, new RoutedEventArgs());
                e.Handled = true;
            }
        };
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        ItemsTab.RestoreLayout(_settings.Current.Grid);
        await _vm.Items.LoadAsync();
#if (tray)
        _tray.Attach(this, () => { _exiting = true; Close(); });
#endif
#if (updates)
        if (_settings.Current.CheckUpdates)
            await _services.GetRequiredService<UpdateService>().CheckAsync(interactive: false);
#endif
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
#if (tray)
        if (_settings.Current.MinimizeToTray && !_exiting)
        {
            e.Cancel = true;
            Hide();
            _tray.HintOnce();
            return;
        }
#endif
        SavePlacement();
        ItemsTab.SaveLayout(_settings.Current.Grid);
        _settings.Save();
    }

    /// <summary>Показать окно поверх остальных (вторая копия программы, значок в трее).</summary>
    public void ShowAndActivate()
    {
        Show();
        if (WindowState == System.Windows.WindowState.Minimized)
            WindowState = System.Windows.WindowState.Normal;
        Activate();
        Topmost = true;
        Topmost = false;
        Focus();
    }

    private void OnSettings(object sender, RoutedEventArgs e)
    {
        var vm = _services.GetRequiredService<SettingsViewModel>();
        vm.Load();
        new SettingsWindow(vm) { Owner = this }.ShowDialog();
    }

    private void OnHelp(object sender, RoutedEventArgs e) => new HelpWindow { Owner = this }.ShowDialog();

    private void OnMore(object sender, RoutedEventArgs e)
    {
        var menu = MoreButton.ContextMenu!;
        menu.DataContext = DataContext;
        menu.PlacementTarget = MoreButton;
        menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
        menu.IsOpen = true;
    }
#if (updates)

    private async void OnCheckUpdates(object sender, RoutedEventArgs e) =>
        await _services.GetRequiredService<UpdateService>().CheckAsync(interactive: true);
#endif

    private void RestorePlacement()
    {
        var w = _settings.Current.Window;
        Width = w.Width;
        Height = w.Height;
        // окно на месте, только если оно видно на одном из мониторов
        if (!double.IsNaN(w.Left) && !double.IsNaN(w.Top)
            && w.Left >= SystemParameters.VirtualScreenLeft - 50
            && w.Top >= SystemParameters.VirtualScreenTop - 50
            && w.Left + 200 <= SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth
            && w.Top + 100 <= SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight)
        {
            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = w.Left;
            Top = w.Top;
        }
        if (w.Maximized)
            WindowState = System.Windows.WindowState.Maximized;
        Tabs.SelectedIndex = Math.Clamp(w.Tab, 0, Tabs.Items.Count - 1);
    }

    private void SavePlacement()
    {
        var w = _settings.Current.Window;
        var bounds = WindowState == System.Windows.WindowState.Normal ? new Rect(Left, Top, Width, Height) : RestoreBounds;
        w.Left = bounds.Left;
        w.Top = bounds.Top;
        w.Width = bounds.Width;
        w.Height = bounds.Height;
        w.Maximized = WindowState == System.Windows.WindowState.Maximized;
        w.Tab = Tabs.SelectedIndex;
    }
}
