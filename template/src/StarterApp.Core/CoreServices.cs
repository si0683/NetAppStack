using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StarterApp.Core.Collecting;
using StarterApp.Core.Settings;
using StarterApp.Core.Storage;

namespace StarterApp.Core;

/// <summary>Регистрация всего ядра. Новый источник данных — одна строка AddSingleton&lt;ICollector, …&gt;.</summary>
public static class CoreServices
{
    public static IServiceCollection AddCore(this IServiceCollection services, AppPaths paths)
    {
        services.AddSingleton(paths);
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<SettingsStore>();

#if (postgres)
        services.AddDbContextFactory<AppDbContext>((sp, o) =>
            o.UseNpgsql(sp.GetRequiredService<SettingsStore>().Current.PostgresConnection));
#else
        services.AddDbContextFactory<AppDbContext>(o => o.UseSqlite($"Data Source={paths.Database}"));
#endif
        services.AddSingleton<ItemStore>();

        // HttpClient для сборщиков: таймаут, повторы с паузой, ограничение частоты при отказах
        services.AddHttpClient(WebListCollector.HttpClientName, c =>
            {
                c.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) StarterApp/1.0");
                c.DefaultRequestHeaders.AcceptLanguage.ParseAdd("ru-RU,ru;q=0.9");
            })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AutomaticDecompression = DecompressionMethods.All })
            .AddStandardResilienceHandler();

        // источники данных
        services.AddSingleton<ICollector, DemoCollector>();
        services.AddSingleton<ICollector, WebListCollector>();

        services.AddSingleton<CollectorService>();
        services.AddHostedService(sp => sp.GetRequiredService<CollectorService>());
        return services;
    }
}
