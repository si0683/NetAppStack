using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using StarterApp.Core.Settings;

namespace StarterApp.Core.Collecting;

/// <summary>
/// Пример настоящего источника: скачивает страницу и разбирает список по селекторам из настроек.
/// Повторы при сбоях и таймауты — в HttpClient «web» (CoreServices). Образец для своих сборщиков.
/// </summary>
public sealed class WebListCollector(IHttpClientFactory http) : ICollector
{
    public const string HttpClientName = "web";

    public string Source => "weblist";

    public string Title => "Веб-страница";

    public bool IsEnabled(AppSettings settings) => Uri.TryCreate(settings.WebList.Url, UriKind.Absolute, out _);

    public async IAsyncEnumerable<ItemDraft> CollectAsync(CollectContext context, [EnumeratorCancellation] CancellationToken ct)
    {
        var source = context.Settings.WebList;
        var url = new Uri(source.Url);

        context.Report($"загрузка {url.Host}…");
        var client = http.CreateClient(HttpClientName);
        string html = await client.GetStringAsync(url, ct);

        var drafts = HtmlListParser.Parse(html, url, source);
        if (drafts.Count == 0)
            context.Log.LogWarning("На странице {Url} нет ни одного элемента «{Selector}» — проверьте селекторы в «Настройках»", url, source.ItemSelector);

        foreach (var draft in drafts)
            yield return draft;
    }
}

