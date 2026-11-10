using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using StarterApp.Core.Settings;

namespace StarterApp.Core.Collecting;

/// <summary>
/// Разбирает HTML-страницу со списком по CSS-селекторам. Отдельно от сети — чтобы проверять
/// тестом на сохранённой странице (tests/…/Fixtures).
/// </summary>
public static class HtmlListParser
{
    public static List<ItemDraft> Parse(string html, Uri pageUrl, WebListSource selectors)
    {
        var document = new HtmlParser().ParseDocument(html);
        var result = new List<ItemDraft>();

        foreach (var element in document.QuerySelectorAll(selectors.ItemSelector))
        {
            var link = element.QuerySelector(selectors.LinkSelector);
            string title = Clean(link?.TextContent ?? element.TextContent);
            if (title.Length == 0)
                continue;

            string? href = link?.GetAttribute("href");
            string? url = href is null ? null : new Uri(pageUrl, href).ToString();

            result.Add(new ItemDraft
            {
                // у ссылки адрес стабилен; нет ссылки — берём хэш названия
                ExternalId = url ?? Hash(title),
                Title = title,
                Url = url,
                Category = Text(element, selectors.CategorySelector),
                PublishedAt = Date(element, selectors.DateSelector),
            });
        }
        return result;
    }

    private static string? Text(IElement element, string selector)
    {
        if (string.IsNullOrWhiteSpace(selector))
            return null;
        string text = Clean(element.QuerySelector(selector)?.TextContent ?? "");
        return text.Length == 0 ? null : text;
    }

    private static DateTime? Date(IElement element, string selector)
    {
        if (string.IsNullOrWhiteSpace(selector))
            return null;
        var node = element.QuerySelector(selector);
        string? raw = node?.GetAttribute("datetime") ?? node?.TextContent;
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var styles = DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal;
        // сначала русский формат (05.11.2026 — 5 ноября); ISO 2026-11-05T… он тоже понимает
        if (DateTime.TryParse(raw.Trim(), CultureInfo.GetCultureInfo("ru-RU"), styles, out var d)
            || DateTime.TryParse(raw.Trim(), CultureInfo.InvariantCulture, styles, out d))
            return d;
        return null;
    }

    private static string Clean(string text) => string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static string Hash(string text) => Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(text)))[..16];
}
