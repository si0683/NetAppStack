using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using StarterApp.Core;
using StarterApp.Core.Collecting;
using StarterApp.Core.Export;
using StarterApp.Core.Settings;

namespace StarterApp.Tests;

public sealed class ParsingAndExportTests
{
    /// <summary>«Золотой» тест: результат разбора сравнивается со снимком в Snapshots/ (см. Snapshot.cs).</summary>
    [Fact]
    public void Html_list_is_parsed_by_selectors()
    {
        string html = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "list-page.html"));
        var selectors = new WebListSource { ItemSelector = "article.card", LinkSelector = "h2 a", DateSelector = "time", CategorySelector = ".tag" };

        var drafts = HtmlListParser.Parse(html, new Uri("https://example.com/list"), selectors);

        Snapshot.Match(drafts);
    }

    [Fact]
    public void Demo_collector_is_stable_within_ten_minutes_and_adds_new_later()
    {
        var now = new DateTime(2026, 11, 10, 12, 1, 0, DateTimeKind.Utc);

        var a = DemoCollector.Build(now);
        var b = DemoCollector.Build(now.AddMinutes(5));
        var c = DemoCollector.Build(now.AddMinutes(10));

        a.Select(d => d.ExternalId).ShouldBe(b.Select(d => d.ExternalId));
        c.Select(d => d.ExternalId).Except(a.Select(d => d.ExternalId)).Count().ShouldBe(2);
        a.Select(d => d.ExternalId).Distinct().Count().ShouldBe(a.Count);
    }

    [Fact]
    public void Csv_has_bom_semicolons_and_quotes_when_needed()
    {
        string path = Path.GetTempFileName();
        try
        {
            ExportColumn[] columns =
            [
                new("Название", r => ((string[])r)[0]),
                new("Сумма", r => decimal.Parse(((string[])r)[1])),
            ];
            TableExport.WriteCsv(path, columns, [new[] { "Дом; снос", "1500000" }, new[] { "Мост \"Северный\"", "2" }]);

            byte[] bytes = File.ReadAllBytes(path);
            bytes.Take(3).ShouldBe(new byte[] { 0xEF, 0xBB, 0xBF });
            File.ReadAllLines(path, Encoding.UTF8).ShouldBe(
            [
                "Название;Сумма",
                "\"Дом; снос\";1500000",
                "\"Мост \"\"Северный\"\"\";2",
            ]);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Xlsx_is_written()
    {
        string path = Path.Combine(Path.GetTempPath(), $"starterapp-{Guid.NewGuid():N}.xlsx");
        try
        {
            TableExport.WriteXlsx(path, [new ExportColumn("Дата", r => (DateTime)r)], [new DateTime(2026, 11, 10) as object]);
            new FileInfo(path).Length.ShouldBeGreaterThan(1000);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Settings_survive_save_and_broken_file_falls_back_to_defaults()
    {
        string dir = Directory.CreateTempSubdirectory("starterapp-settings-").FullName;
        try
        {
            var paths = new AppPaths(dir);
            var store = new SettingsStore(paths, NullLogger<SettingsStore>.Instance);
            store.Current.UpdateIntervalMinutes = 15;
            store.Current.WebList.Url = "https://example.com/список";
            store.Save();

            var reloaded = new SettingsStore(paths, NullLogger<SettingsStore>.Instance);
            reloaded.Current.UpdateIntervalMinutes.ShouldBe(15);
            reloaded.Current.WebList.Url.ShouldBe("https://example.com/список");
            File.ReadAllText(paths.Settings).ShouldContain("список"); // русские буквы без \u-кодов

            File.WriteAllText(paths.Settings, "{ испорчено");
            var broken = new SettingsStore(paths, NullLogger<SettingsStore>.Instance);
            broken.Current.UpdateIntervalMinutes.ShouldBe(new AppSettings().UpdateIntervalMinutes);
            Directory.GetFiles(paths.Backups, "settings-broken-*").ShouldNotBeEmpty();
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
