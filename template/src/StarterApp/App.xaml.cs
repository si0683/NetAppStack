using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using StarterApp.Services;

namespace StarterApp;

public partial class App : Application
{
    /// <summary>Сервисы из AppHost. Задаёт Program до запуска.</summary>
    public IServiceProvider Services { get; init; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // необработанные ошибки — в лог и понятное сообщение, а не молчаливое падение
        DispatcherUnhandledException += OnUiException;
        AppDomain.CurrentDomain.UnhandledException += (_, a) => Log.Fatal(a.ExceptionObject as Exception, "Необработанная ошибка");
        TaskScheduler.UnobservedTaskException += (_, a) =>
        {
            Log.Error(a.Exception, "Необработанная ошибка в задаче");
            a.SetObserved();
        };

        var window = Services.GetRequiredService<MainWindow>();
        MainWindow = window;
        Services.GetRequiredService<ThemeService>().Apply(window);
        window.Show();

        SingleInstance.Listen(() => Dispatcher.InvokeAsync(window.ShowAndActivate));
    }

    private void OnUiException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Error(e.Exception, "Ошибка в интерфейсе");
        MessageBox.Show("Что-то пошло не так:\n\n" + e.Exception.Message + "\n\nПрограмма продолжит работу. Подробности — во вкладке «Логи».",
            AppInfo.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
        e.Handled = true;
    }
}
