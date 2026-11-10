using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using H.NotifyIcon;
using H.NotifyIcon.Core;
using StarterApp.Core.Collecting;

namespace StarterApp.Services;

/// <summary>
/// Значок в трее: двойной щелчок — окно, меню — обновить/выход,
/// уведомление о новых записях, когда окно скрыто или не в фокусе.
/// </summary>
public sealed class TrayService(CollectorService collector) : IDisposable
{
    private TaskbarIcon? _icon;
    private Window? _window;
    private bool _hinted;

    public void Attach(MainWindow window, Action exit)
    {
        _window = window;
        var menu = new ContextMenu();
        menu.Items.Add(MenuItem("Открыть", window.ShowAndActivate));
        menu.Items.Add(MenuItem("Обновить сейчас", collector.RunNow));
        menu.Items.Add(new Separator());
        menu.Items.Add(MenuItem("Выход", exit));

        _icon = new TaskbarIcon
        {
            ToolTipText = AppInfo.Title,
            IconSource = BitmapFrame.Create(new Uri("pack://application:,,,/Assets/app.ico")),
            ContextMenu = menu,
        };
        _icon.TrayMouseDoubleClick += (_, _) => window.ShowAndActivate();
        _icon.ForceCreate(enablesEfficiencyMode: false);

        collector.Finished += r =>
        {
            if (r.AddedIds.Count > 0)
                Application.Current?.Dispatcher.InvokeAsync(() => NotifyNew(r.AddedIds.Count));
        };
        Application.Current!.Exit += (_, _) => Dispose();
    }

    /// <summary>Первый раз, когда окно ушло в трей, — подсказать, где программа.</summary>
    public void HintOnce()
    {
        if (_hinted)
            return;
        _hinted = true;
        _icon?.ShowNotification(AppInfo.Title, "Программа работает в трее и продолжает собирать данные.");
    }

    private void NotifyNew(int count)
    {
        if (_window is { IsVisible: true, IsActive: true })
            return;
        _icon?.ShowNotification("Новые записи", $"Найдено новых: {count}", NotificationIcon.Info);
    }

    private static MenuItem MenuItem(string header, Action action)
    {
        var item = new MenuItem { Header = header };
        item.Click += (_, _) => action();
        return item;
    }

    public void Dispose()
    {
        _icon?.Dispose();
        _icon = null;
    }
}
