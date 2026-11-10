using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using StarterApp.Core;
using StarterApp.Core.Collecting;
using StarterApp.Core.Settings;
using StarterApp.Core.Storage;

namespace StarterApp.Tests;

public sealed class StoreAndCollectorTests : IDisposable
{
    private readonly TestDb _db = new();
    private readonly TestClock _clock = new(new DateTime(2026, 11, 10, 12, 0, 0, DateTimeKind.Utc));
    private readonly string _dir = Directory.CreateTempSubdirectory("starterapp-tests-").FullName;

    public void Dispose()
    {
        _db.Dispose();
        Directory.Delete(_dir, recursive: true);
    }

    private ItemStore Store() => new(_db, _clock);

    private static ItemDraft Draft(string id, string title = "Объект") => new() { ExternalId = id, Title = title };

    [Fact]
    public async Task Upsert_adds_new_and_updates_changed_only()
    {
        var store = Store();

        var first = await store.UpsertAsync("src", [Draft("1"), Draft("2")]);
        first.AddedIds.Count.ShouldBe(2);

        var second = await store.UpsertAsync("src", [Draft("1"), Draft("2", "Новое название"), Draft("3")]);
        second.AddedIds.Count.ShouldBe(1);
        second.UpdatedIds.Count.ShouldBe(1);
        (await store.GetAllAsync()).Count.ShouldBe(3);
    }

    [Fact]
    public async Task Upsert_keeps_user_status_and_note()
    {
        var store = Store();
        var id = (await store.UpsertAsync("src", [Draft("1")])).AddedIds[0];
        await store.SaveWorkAsync(id, ItemStatus.InWork, "перезвонить в пятницу");

        await store.UpsertAsync("src", [Draft("1", "Изменилось у источника")]);

        var item = (await store.GetAllAsync()).Single();
        item.Title.ShouldBe("Изменилось у источника");
        item.Status.ShouldBe(ItemStatus.InWork);
        item.Note.ShouldBe("перезвонить в пятницу");
        item.StatusChangedAt.ShouldBe(_clock.Now);
    }

    [Fact]
    public async Task Same_external_id_in_different_sources_is_different_items()
    {
        var store = Store();
        await store.UpsertAsync("a", [Draft("1")]);
        await store.UpsertAsync("b", [Draft("1")]);
        (await store.GetAllAsync()).Count.ShouldBe(2);
    }

    [Fact]
    public async Task Collector_error_does_not_stop_other_sources()
    {
        var service = Service(new FakeCollector("ok", 250), new FailingCollector());

        var result = await service.RunOnceAsync(CancellationToken.None);

        result.AddedIds.Count.ShouldBe(250); // три пачки по 100/100/50
        result.Errors.ShouldBe(1);
        service.State.IsRunning.ShouldBeFalse();
        service.State.Message.ShouldContain("ошибками");
    }

    [Fact]
    public async Task Second_run_finds_nothing_new()
    {
        var service = Service(new FakeCollector("ok", 10));
        await service.RunOnceAsync(CancellationToken.None);

        var again = await service.RunOnceAsync(CancellationToken.None);

        again.AddedIds.ShouldBeEmpty();
        again.Updated.ShouldBe(0);
    }

    [Fact]
    public async Task Cancel_keeps_what_was_read()
    {
        var collector = new FakeCollector("slow", 1000, delayEvery: 50);
        var service = Service(collector);
        collector.OnItem = n => { if (n == 120) service.CancelRun(); };

        var result = await service.RunOnceAsync(CancellationToken.None);

        result.Cancelled.ShouldBeTrue();
        (await Store().GetAllAsync()).Count.ShouldBeInRange(100, 121);
    }

    [Fact]
    public async Task Disabled_sources_are_skipped()
    {
        var service = Service(new FakeCollector("off", 5) { Enabled = false });
        var result = await service.RunOnceAsync(CancellationToken.None);
        result.AddedIds.ShouldBeEmpty();
        service.State.Message.ShouldContain("Нет включённых источников");
    }

    private CollectorService Service(params ICollector[] collectors) =>
        new(collectors, Store(), new SettingsStore(new AppPaths(_dir), NullLogger<SettingsStore>.Instance),
            _clock, NullLogger<CollectorService>.Instance);

    private sealed class FakeCollector(string source, int count, int delayEvery = 0) : ICollector
    {
        public bool Enabled { get; init; } = true;
        public Action<int>? OnItem { get; set; }
        public string Source => source;
        public string Title => source;
        public bool IsEnabled(AppSettings settings) => Enabled;

        public async IAsyncEnumerable<ItemDraft> CollectAsync(CollectContext context, [EnumeratorCancellation] CancellationToken ct)
        {
            for (int i = 1; i <= count; i++)
            {
                ct.ThrowIfCancellationRequested();
                if (delayEvery > 0 && i % delayEvery == 0)
                    await Task.Delay(1, ct);
                OnItem?.Invoke(i);
                yield return Draft(i.ToString());
            }
        }
    }

    private sealed class FailingCollector : ICollector
    {
        public string Source => "bad";
        public string Title => "Сломанный";
        public bool IsEnabled(AppSettings settings) => true;

        public async IAsyncEnumerable<ItemDraft> CollectAsync(CollectContext context, [EnumeratorCancellation] CancellationToken ct)
        {
            await Task.Yield();
            yield return Draft("x");
            throw new HttpRequestException("источник недоступен");
        }
    }
}
