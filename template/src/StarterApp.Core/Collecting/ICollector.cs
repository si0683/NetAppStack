using Microsoft.Extensions.Logging;
using StarterApp.Core.Settings;

namespace StarterApp.Core.Collecting;

/// <summary>
/// Источник данных. Новый источник = новый класс + одна строка регистрации в CoreServices.
/// Сборщик только читает источник и отдаёт черновики; запись в базу, повторы, логи и отмену
/// берёт на себя <see cref="CollectorService"/>.
/// </summary>
public interface ICollector
{
    /// <summary>Код источника для базы, латиницей. Потом не меняется.</summary>
    string Source { get; }

    /// <summary>Название для людей: «Госзакупки», «Демо».</summary>
    string Title { get; }

    /// <summary>Включён ли источник при этих настройках.</summary>
    bool IsEnabled(AppSettings settings);

    /// <summary>Отдаёт записи по мере чтения. Отмена — через <paramref name="ct"/>.</summary>
    IAsyncEnumerable<ItemDraft> CollectAsync(CollectContext context, CancellationToken ct);
}

/// <summary>Что сборщик получает от сервиса: настройки, лог и способ показать прогресс.</summary>
public sealed class CollectContext(AppSettings settings, ILogger log, Action<string, double?> report)
{
    public AppSettings Settings { get; } = settings;

    public ILogger Log { get; } = log;

    /// <summary>Строка над таблицей: «Страница 3 из 10», доля 0..1 или null, если неизвестно.</summary>
    public void Report(string message, double? fraction = null) => report(message, fraction);
}
