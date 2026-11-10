namespace StarterApp.Services;

/// <summary>
/// Одна копия программы. Вторая копия посылает первой сигнал «покажись» и выходит —
/// работает, даже если первая свёрнута в трей.
/// </summary>
public static class SingleInstance
{
    // свой номер у каждого проекта (подставляется при создании из шаблона)
    private const string Name = "StarterApp-6a1f3c52-9d7e-4b8a-a1f0-3e2d5c7b9f10";

    private static Mutex? _mutex;
    private static EventWaitHandle? _show;

    public static bool TryStart()
    {
        _mutex = new Mutex(true, Name + "-mutex", out bool first);
        if (!first)
        {
            try { EventWaitHandle.OpenExisting(Name + "-show").Set(); } catch (WaitHandleCannotBeOpenedException) { }
            return false;
        }
        _show = new EventWaitHandle(false, EventResetMode.AutoReset, Name + "-show");
        return true;
    }

    /// <summary>Вызывает <paramref name="onShow"/> (из фонового потока), когда запускают вторую копию.</summary>
    public static void Listen(Action onShow)
    {
        var thread = new Thread(() =>
        {
            while (_show!.WaitOne())
                onShow();
        })
        { IsBackground = true, Name = "single-instance" };
        thread.Start();
    }
}
