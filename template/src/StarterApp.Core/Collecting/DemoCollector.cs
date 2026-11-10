using System.Runtime.CompilerServices;
using StarterApp.Core.Settings;

namespace StarterApp.Core.Collecting;

/// <summary>
/// Демо-источник без интернета: 60 постоянных записей за последний месяц
/// и по 2 новых каждые 10 минут — видно, как новое появляется сверху.
/// Замените его своим источником (или выключите в «Настройках»).
/// </summary>
public sealed class DemoCollector(TimeProvider time) : ICollector
{
    private static readonly string[] Categories = ["Строительство", "Дороги", "Демонтаж", "Благоустройство", "Сети"];
    private static readonly string[] Works =
    [
        "Ремонт дороги", "Строительство школы", "Снос аварийного дома", "Благоустройство двора",
        "Замена водопровода", "Строительство склада", "Укладка асфальта", "Земляные работы",
        "Строительство моста", "Реконструкция стадиона",
    ];
    private static readonly string[] Places = ["Самара", "Тольятти", "Сызрань", "Новокуйбышевск", "Чапаевск", "Жигулёвск"];

    public string Source => "demo";

    public string Title => "Демо";

    public bool IsEnabled(AppSettings settings) => settings.DemoEnabled;

    public async IAsyncEnumerable<ItemDraft> CollectAsync(CollectContext context, [EnumeratorCancellation] CancellationToken ct)
    {
        var now = time.GetUtcNow().UtcDateTime;
        var drafts = Build(now);
        for (int i = 0; i < drafts.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            context.Report($"запись {i + 1} из {drafts.Count}", (double)(i + 1) / drafts.Count);
            await Task.Delay(15, ct); // как будто читаем сеть
            yield return drafts[i];
        }
    }

    /// <summary>Записи на момент <paramref name="now"/>. Одинаковые для одного и того же времени — удобно тестировать.</summary>
    internal static List<ItemDraft> Build(DateTime now)
    {
        var list = new List<ItemDraft>();
        var month = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var random = new Random(42);
        for (int i = 0; i < 60; i++)
            list.Add(Make($"base-{month:yyyyMM}-{i}", month.AddDays(-random.Next(0, 30)).AddHours(random.Next(8, 18)), random));

        // свежие: номер зависит от 10-минутного окна
        long slot = now.Ticks / TimeSpan.FromMinutes(10).Ticks;
        var fresh = new Random((int)(slot % int.MaxValue));
        for (int i = 0; i < 2; i++)
            list.Add(Make($"fresh-{slot}-{i}", new DateTime(slot * TimeSpan.FromMinutes(10).Ticks, DateTimeKind.Utc), fresh));

        return list;
    }

    private static ItemDraft Make(string id, DateTime published, Random random)
    {
        string place = Places[random.Next(Places.Length)];
        return new ItemDraft
        {
            ExternalId = id,
            Title = $"{Works[random.Next(Works.Length)]}, {place}",
            Category = Categories[random.Next(Categories.Length)],
            Location = place,
            Amount = random.Next(5, 900) * 100_000m,
            Phone = $"+7 900 000-{random.Next(10, 99)}-{random.Next(10, 99)}",
            Url = "https://example.com/demo/" + id,
            PublishedAt = published,
        };
    }
}
