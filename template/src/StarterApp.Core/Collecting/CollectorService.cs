using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StarterApp.Core.Settings;
using StarterApp.Core.Storage;

namespace StarterApp.Core.Collecting;

/// <summary>
/// Фоновый сбор: сразу после запуска, потом раз в <see cref="AppSettings.UpdateIntervalMinutes"/>,
/// и по кнопке «Обновить сейчас». Пишет в базу пачками — новые записи появляются в таблице по ходу сбора.
/// Ошибка одного источника не останавливает остальные.
/// </summary>
public sealed class CollectorService : BackgroundService
{
    private const int BatchSize = 100;

    private readonly IReadOnlyList<ICollector> _collectors;
    private readonly ItemStore _store;
    private readonly SettingsStore _settings;
    private readonly TimeProvider _time;
    private readonly ILogger<CollectorService> _log;
    private readonly SemaphoreSlim _wake = new(0, 1);
    private CancellationTokenSource? _runCts;
    private CancellationTokenSource? _rescheduleCts;
    private CollectorState _state = CollectorState.Idle;

    public CollectorService(IEnumerable<ICollector> collectors, ItemStore store, SettingsStore settings,
        TimeProvider time, ILogger<CollectorService> log)
    {
        _collectors = collectors.ToList();
        _store = store;
        _settings = settings;
        _time = time;
        _log = log;
        // поменяли интервал в настройках — пересчитать ожидание
        _settings.Changed += _ => _rescheduleCts?.Cancel();
    }

    public CollectorState State => _state;

    public IReadOnlyList<ICollector> Collectors => _collectors;

    /// <summary>Состояние изменилось. Вызывается из фонового потока.</summary>
    public event Action<CollectorState>? StateChanged;

    /// <summary>Пачка записана в базу: id новых и обновлённых. Из фонового потока.</summary>
    public event Action<UpsertResult>? ItemsChanged;

    /// <summary>Сбор закончен. Из фонового потока.</summary>
    public event Action<CollectResult>? Finished;

    /// <summary>Запустить сбор сейчас, не дожидаясь расписания.</summary>
    public void RunNow()
    {
        if (_state.IsRunning || _wake.CurrentCount > 0)
            return;
        try { _wake.Release(); } catch (SemaphoreFullException) { }
    }

    /// <summary>Остановить текущий сбор. Уже записанное остаётся.</summary>
    public void CancelRun() => _runCts?.Cancel();

    protected override async Task ExecuteAsync(CancellationToken stop)
    {
        await Task.Yield();
        while (!stop.IsCancellationRequested)
        {
            await RunOnceAsync(stop);
            if (!await WaitNextAsync(stop))
                break;
        }
    }

    /// <summary>Ждёт следующего сбора. false — программа закрывается.</summary>
    private async Task<bool> WaitNextAsync(CancellationToken stop)
    {
        var lastRun = _state.LastRunAt ?? _time.GetUtcNow().UtcDateTime;
        while (!stop.IsCancellationRequested)
        {
            var interval = TimeSpan.FromMinutes(Math.Max(1, _settings.Current.UpdateIntervalMinutes));
            var due = lastRun + interval;
            SetState(_state with { NextRunAt = due });

            var left = due - _time.GetUtcNow().UtcDateTime;
            if (left <= TimeSpan.Zero)
                return true;

            using var reschedule = CancellationTokenSource.CreateLinkedTokenSource(stop);
            _rescheduleCts = reschedule;
            try
            {
                await _wake.WaitAsync(left, reschedule.Token);
                return true; // по кнопке или по времени
            }
            catch (OperationCanceledException) when (!stop.IsCancellationRequested)
            {
                // изменился интервал — считаем заново
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            finally
            {
                _rescheduleCts = null;
            }
        }
        return false;
    }

    internal async Task<CollectResult> RunOnceAsync(CancellationToken stop)
    {
        var settings = _settings.Current;
        var enabled = _collectors.Where(c => c.IsEnabled(settings)).ToList();
        if (enabled.Count == 0)
        {
            SetState(_state with { IsRunning = false, Progress = null, Message = "Нет включённых источников — включите их в «Настройках»" });
            return new CollectResult([], 0, 0, false);
        }

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(stop);
        _runCts = cts;
        var added = new List<long>();
        int updated = 0, errors = 0;
        bool cancelled = false;

        SetState(_state with { IsRunning = true, Progress = 0, Message = "Сбор…", NextRunAt = null });
        _log.LogInformation("Сбор начат: {Sources}", string.Join(", ", enabled.Select(c => c.Title)));

        for (int i = 0; i < enabled.Count && !cancelled; i++)
        {
            var collector = enabled[i];
            int index = i;
            var batch = new List<ItemDraft>(BatchSize);
            int read = 0, addedHere = 0, updatedHere = 0;

            async Task FlushAsync()
            {
                if (batch.Count == 0)
                    return;
                var r = await _store.UpsertAsync(collector.Source, batch, stop);
                batch.Clear();
                added.AddRange(r.AddedIds);
                addedHere += r.AddedIds.Count;
                updatedHere += r.UpdatedIds.Count;
                if (r.AddedIds.Count + r.UpdatedIds.Count > 0)
                    ItemsChanged?.Invoke(r);
            }

            var context = new CollectContext(settings, _log, (message, fraction) =>
                SetState(_state with
                {
                    Message = $"{collector.Title}: {message}",
                    Progress = enabled.Count == 1 && fraction is null ? null : (index + (fraction ?? 0)) / enabled.Count,
                }));

            try
            {
                context.Report("чтение…");
                await foreach (var draft in collector.CollectAsync(context, cts.Token).WithCancellation(cts.Token))
                {
                    batch.Add(draft);
                    read++;
                    if (batch.Count >= BatchSize)
                        await FlushAsync();
                }
                await FlushAsync();
                _log.LogInformation("{Source}: прочитано {Read}, новых {Added}, обновлено {Updated}",
                    collector.Title, read, addedHere, updatedHere);
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested)
            {
                cancelled = true;
                if (!stop.IsCancellationRequested)
                    await FlushAsync();
                _log.LogWarning("{Source}: сбор отменён, записано {Added} новых", collector.Title, addedHere);
            }
            catch (Exception ex)
            {
                errors++;
                _log.LogError(ex, "{Source}: ошибка сбора", collector.Title);
            }
            updated += updatedHere;
        }

        _runCts = null;
        string message = cancelled ? "Сбор отменён"
            : errors > 0 ? $"Готово с ошибками ({errors}) — см. «Логи». Новых: {added.Count}"
            : $"Готово: новых {added.Count}, обновлено {updated}";
        SetState(_state with { IsRunning = false, Progress = null, Message = message, LastRunAt = _time.GetUtcNow().UtcDateTime });

        var result = new CollectResult(added, updated, errors, cancelled);
        Finished?.Invoke(result);
        return result;
    }

    private void SetState(CollectorState state)
    {
        _state = state;
        StateChanged?.Invoke(state);
    }
}
