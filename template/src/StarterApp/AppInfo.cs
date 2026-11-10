using System.Reflection;

namespace StarterApp;

/// <summary>Название и версия программы — для заголовка, логов и справки.</summary>
public static class AppInfo
{
    public const string Title = "StarterApp";

    public const string RepositoryUrl = "https://github.com/OWNER/REPO";

    public static string Version { get; } =
        Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString(3)
        ?? "0.0.0";
}
