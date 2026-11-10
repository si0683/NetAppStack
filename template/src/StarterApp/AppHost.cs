using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using StarterApp.Core;
using StarterApp.Logging;
using StarterApp.Services;
using StarterApp.ViewModels;

namespace StarterApp;

/// <summary>Сборка всех сервисов. Новое окно, сервис или ViewModel — регистрируется здесь.</summary>
public static class AppHost
{
    public static IHost Build(AppPaths paths, string[] args)
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            Args = args,
            ContentRootPath = AppContext.BaseDirectory,
            DisableDefaults = true,
        });

        // логи: файл data/logs/log-ГГГГММДД.txt (30 дней) + вкладка «Логи»
        var feed = new LogFeed();
        builder.Services.AddSingleton(feed);
        builder.Services.AddSerilog(lc => lc
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System.Net.Http", LogEventLevel.Warning)
            .MinimumLevel.Override("Polly", LogEventLevel.Warning)
            .WriteTo.File(Path.Combine(paths.Logs, "log-.txt"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
            .WriteTo.Sink(new LogFeedSink(feed)));

        builder.Services.AddCore(paths);

        // интерфейс
        builder.Services.AddSingleton<ThemeService>();
        builder.Services.AddSingleton<MainViewModel>();
        builder.Services.AddSingleton<ItemsViewModel>();
        builder.Services.AddSingleton<LogsViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();
        builder.Services.AddSingleton<MainWindow>();
#if (tray)
        builder.Services.AddSingleton<TrayService>();
#endif
#if (updates)
        builder.Services.AddSingleton<UpdateService>();
#endif

        return builder.Build();
    }
}
