using System.Runtime.CompilerServices;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace StarterApp.Tests;

/// <summary>
/// «Золотые» тесты: результат сравнивается с файлом Snapshots/Класс.Тест.verified.json рядом с тестом.
/// Файла нет или результат другой → рядом пишется *.received.json и тест падает.
/// Изменение намеренное — проверьте received и переименуйте его в verified.
/// </summary>
public static class Snapshot
{
    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static void Match(object? value, string? name = null,
        [CallerFilePath] string testFile = "", [CallerMemberName] string testName = "")
    {
        string dir = Path.Combine(Path.GetDirectoryName(testFile)!, "Snapshots");
        string baseName = $"{Path.GetFileNameWithoutExtension(testFile)}.{name ?? testName}";
        string verified = Path.Combine(dir, baseName + ".verified.json");
        string received = Path.Combine(dir, baseName + ".received.json");

        string actual = JsonSerializer.Serialize(value, Json).ReplaceLineEndings("\n") + "\n";
        string? expected = File.Exists(verified) ? File.ReadAllText(verified).ReplaceLineEndings("\n") : null;

        if (actual == expected)
        {
            File.Delete(received);
            return;
        }

        Directory.CreateDirectory(dir);
        File.WriteAllText(received, actual);
        Assert.Fail(expected is null
            ? $"Нет снимка {verified}. Проверьте {received} и переименуйте его в .verified.json."
            : $"Результат отличается от снимка {verified}. Новый результат — {received}.");
    }
}
