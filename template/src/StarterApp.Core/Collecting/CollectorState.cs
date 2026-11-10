namespace StarterApp.Core.Collecting;

/// <summary>Что сейчас делает сборщик — для строки над таблицей.</summary>
public sealed record CollectorState
{
    public bool IsRunning { get; init; }

    /// <summary>«Собираю: Демо — 12 записей», «Готово: +3 новых».</summary>
    public string Message { get; init; } = "";

    /// <summary>0..1 или null — прогресс неизвестен.</summary>
    public double? Progress { get; init; }

    public DateTime? LastRunAt { get; init; }

    public DateTime? NextRunAt { get; init; }

    public static CollectorState Idle { get; } = new() { Message = "Ожидание" };
}

/// <summary>Итог одного сбора.</summary>
public sealed record CollectResult(IReadOnlyList<long> AddedIds, int Updated, int Errors, bool Cancelled);
