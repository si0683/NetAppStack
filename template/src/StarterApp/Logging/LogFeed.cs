using System.Collections.Concurrent;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Display;

namespace StarterApp.Logging;

/// <summary>Строка вкладки «Логи».</summary>
public sealed record LogLine(DateTime Time, LogEventLevel Level, string Source, string Message)
{
    public bool IsError => Level >= LogEventLevel.Error;

    public bool IsWarning => Level == LogEventLevel.Warning;

    public string LevelText => Level switch
    {
        LogEventLevel.Fatal => "КРИТ",
        LogEventLevel.Error => "ОШИБКА",
        LogEventLevel.Warning => "ВНИМАНИЕ",
        LogEventLevel.Information => "инфо",
        _ => "отладка",
    };
}

/// <summary>Очередь строк лога от любых потоков; вкладка «Логи» забирает их таймером.</summary>
public sealed class LogFeed
{
    private readonly ConcurrentQueue<LogLine> _pending = new();

    public void Add(LogLine line) => _pending.Enqueue(line);

    public List<LogLine> Drain()
    {
        var list = new List<LogLine>();
        while (_pending.TryDequeue(out var line))
            list.Add(line);
        return list;
    }
}

/// <summary>Приёмник Serilog → <see cref="LogFeed"/>.</summary>
public sealed class LogFeedSink(LogFeed feed) : ILogEventSink
{
    // строки без кавычек: «Сбор начат: Демо», а не «Сбор начат: "Демо"»
    private static readonly MessageTemplateTextFormatter Formatter = new("{Message:lj}");

    public void Emit(LogEvent e)
    {
        string source = e.Properties.TryGetValue("SourceContext", out var ctx)
            ? ctx.ToString().Trim('"').Split('.').Last()
            : "";
        var writer = new StringWriter();
        Formatter.Format(e, writer);
        string message = writer.ToString();
        if (e.Exception is not null)
            message += " — " + e.Exception.GetType().Name + ": " + e.Exception.Message;
        feed.Add(new LogLine(e.Timestamp.LocalDateTime, e.Level, source, message));
    }
}
