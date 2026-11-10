namespace StarterApp.Core;

/// <summary>
/// Где программа хранит свои файлы. Всё лежит в папке data рядом с exe —
/// программу можно переносить папкой, ничего не теряя.
/// </summary>
public sealed class AppPaths
{
    public AppPaths(string? root = null)
    {
        Root = root ?? Path.Combine(AppContext.BaseDirectory, "data");
        Directory.CreateDirectory(Root);
        Directory.CreateDirectory(Logs);
        Directory.CreateDirectory(Reports);
        Directory.CreateDirectory(Backups);
    }

    /// <summary>Папка data.</summary>
    public string Root { get; }

    public string Logs => Path.Combine(Root, "logs");

    /// <summary>Выгрузки в Excel и CSV.</summary>
    public string Reports => Path.Combine(Root, "reports");

    public string Backups => Path.Combine(Root, "backups");

    public string Settings => Path.Combine(Root, "settings.json");

    public string Database => Path.Combine(Root, "app.db");
}
