using System.Windows;
using Microsoft.Extensions.Logging;
using Velopack;
using Velopack.Sources;

namespace StarterApp.Services;

/// <summary>
/// Обновления через Velopack из GitHub Releases (релиз собирает .github/workflows/release.yml).
/// Работает, когда программа установлена через Setup.exe; запуск из папки сборки обновления пропускает.
/// </summary>
public sealed class UpdateService(ILogger<UpdateService> log)
{
    public async Task CheckAsync(bool interactive)
    {
        try
        {
            var manager = new UpdateManager(new GithubSource(AppInfo.RepositoryUrl, null, false));
            if (!manager.IsInstalled)
            {
                log.LogInformation("Обновления: программа запущена не из установки — проверка пропущена");
                if (interactive)
                    Message("Обновления работают в установленной программе (Setup.exe из GitHub Releases).");
                return;
            }

            var update = await manager.CheckForUpdatesAsync();
            if (update is null)
            {
                if (interactive)
                    Message($"У вас последняя версия: {AppInfo.Version}.");
                return;
            }

            string version = update.TargetFullRelease.Version.ToString();
            log.LogInformation("Доступна версия {Version}", version);
            var answer = MessageBox.Show($"Доступна версия {version}. Скачать и перезапустить программу?",
                AppInfo.Title, MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (answer != MessageBoxResult.Yes)
                return;

            await manager.DownloadUpdatesAsync(update);
            manager.ApplyUpdatesAndRestart(update);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Проверка обновлений не удалась");
            if (interactive)
                Message("Не удалось проверить обновления: " + ex.Message);
        }
    }

    private static void Message(string text) =>
        MessageBox.Show(text, AppInfo.Title, MessageBoxButton.OK, MessageBoxImage.Information);
}
