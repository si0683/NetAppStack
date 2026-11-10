using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace StarterApp.Core.Settings;

/// <summary>
/// Читает и сохраняет настройки. Запись атомарная (через временный файл) — при сбое
/// питания файл не обнулится. Испорченный файл откладывается в backups, берутся значения по умолчанию.
/// </summary>
public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        // русские буквы в файле остаются читаемыми
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        // положение окна по умолчанию — NaN («пусть Windows решит»)
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
    };

    private readonly AppPaths _paths;
    private readonly ILogger<SettingsStore> _log;
    private readonly Lock _sync = new();

    public SettingsStore(AppPaths paths, ILogger<SettingsStore> log)
    {
        _paths = paths;
        _log = log;
        Current = Load();
    }

    public AppSettings Current { get; private set; }

    /// <summary>Настройки изменились (после <see cref="Save"/>).</summary>
    public event Action<AppSettings>? Changed;

    public void Save()
    {
        lock (_sync)
        {
            string tmp = _paths.Settings + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(Current, Json));
            File.Move(tmp, _paths.Settings, overwrite: true);
        }
        Changed?.Invoke(Current);
    }

    private AppSettings Load()
    {
        if (!File.Exists(_paths.Settings))
            return new AppSettings();

        try
        {
            return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_paths.Settings), Json) ?? new AppSettings();
        }
        catch (Exception ex)
        {
            string backup = Path.Combine(_paths.Backups, $"settings-broken-{DateTime.Now:yyyyMMdd-HHmmss}.json");
            File.Copy(_paths.Settings, backup, overwrite: true);
            _log.LogWarning(ex, "Файл настроек испорчен — взяты настройки по умолчанию, старый файл: {Backup}", backup);
            return new AppSettings();
        }
    }
}
