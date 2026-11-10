using System.Diagnostics;

namespace StarterApp.Services;

/// <summary>Открыть папку в Проводнике, ссылку в браузере, файл в программе по умолчанию.</summary>
public static class Shell
{
    public static void OpenFolder(string path) =>
        Process.Start(new ProcessStartInfo("explorer.exe", $"\"{path}\"") { UseShellExecute = true });

    /// <summary>Показать файл выделенным в Проводнике.</summary>
    public static void SelectFile(string path) =>
        Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{path}\"") { UseShellExecute = true });

    public static void Open(string pathOrUrl) =>
        Process.Start(new ProcessStartInfo(pathOrUrl) { UseShellExecute = true });
}
