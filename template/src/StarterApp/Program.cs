using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StarterApp.Core;
using StarterApp.Core.Storage;
using StarterApp.Services;
#if (updates)
using Velopack;
#endif

namespace StarterApp;

/// <summary>
/// Точка входа: одна копия программы → сервисы (DI, логи, фоновый сбор) → база → окно.
/// </summary>
public static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
#if (updates)
        // Velopack: установка, удаление и применение обновлений. Должно быть первой строкой.
        VelopackApp.Build().Run();
#endif
        // вторая копия только показывает окно первой и выходит
        if (!SingleInstance.TryStart())
            return 0;

        var paths = new AppPaths();
        using var host = AppHost.Build(paths, args);
        var log = host.Services.GetRequiredService<ILogger<App>>();
        log.LogInformation("Запуск {Version}, данные: {Data}", AppInfo.Version, paths.Root);

        try
        {
            host.Services.GetRequiredService<ItemStore>().EnsureCreatedAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            log.LogCritical(ex, "База недоступна");
            MessageBox.Show("База данных недоступна:\n\n" + ex.Message + "\n\nПодробности — в папке data\\logs.",
                AppInfo.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            return 1;
        }

        host.Start();

        var app = new App { Services = host.Services };
        app.InitializeComponent();
        int code = app.Run();

        log.LogInformation("Выход");
        host.StopAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
        Serilog.Log.CloseAndFlush();
        return code;
    }
}
