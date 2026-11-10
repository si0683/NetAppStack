using System.Globalization;
using System.Text;
using ClosedXML.Excel;

namespace StarterApp.Core.Export;

/// <summary>Колонка выгрузки: заголовок и как достать значение из строки.</summary>
public sealed record ExportColumn(string Header, Func<object, object?> Value);

/// <summary>
/// Выгрузка таблицы «как на экране» в Excel (.xlsx) и CSV.
/// CSV — с «;» и BOM: русский Excel открывает его двойным щелчком без кракозябр.
/// </summary>
public static class TableExport
{
    private static readonly CultureInfo Ru = CultureInfo.GetCultureInfo("ru-RU");

    public static void WriteCsv(string path, IReadOnlyList<ExportColumn> columns, IEnumerable<object> rows)
    {
        using var writer = new StreamWriter(path, false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        writer.WriteLine(string.Join(';', columns.Select(c => Quote(c.Header))));
        foreach (var row in rows)
            writer.WriteLine(string.Join(';', columns.Select(c => Quote(Format(c.Value(row))))));
    }

    /// <summary>Кавычки — только если в значении есть «;», кавычка или перенос строки.</summary>
    private static string Quote(string value) =>
        value.IndexOfAny([';', '"', '\n', '\r']) >= 0 ? '"' + value.Replace("\"", "\"\"") + '"' : value;

    public static void WriteXlsx(string path, IReadOnlyList<ExportColumn> columns, IEnumerable<object> rows, string sheet = "Данные")
    {
        using var book = new XLWorkbook();
        var ws = book.Worksheets.Add(sheet);

        for (int c = 0; c < columns.Count; c++)
            ws.Cell(1, c + 1).Value = columns[c].Header;
        ws.Row(1).Style.Font.Bold = true;

        int r = 2;
        foreach (var row in rows)
        {
            for (int c = 0; c < columns.Count; c++)
            {
                var cell = ws.Cell(r, c + 1);
                switch (columns[c].Value(row))
                {
                    case null: break;
                    case DateTime d: cell.Value = d; cell.Style.DateFormat.Format = "dd.MM.yyyy HH:mm"; break;
                    case decimal m: cell.Value = m; cell.Style.NumberFormat.Format = "# ##0.00"; break;
                    case int or long or double: cell.Value = Convert.ToDouble(columns[c].Value(row), CultureInfo.InvariantCulture); break;
                    case Uri u: cell.Value = u.ToString(); cell.SetHyperlink(new XLHyperlink(u)); break;
                    case var v: cell.Value = v.ToString(); break;
                }
            }
            r++;
        }

        var range = ws.Range(1, 1, Math.Max(1, r - 1), columns.Count);
        range.SetAutoFilter();
        ws.SheetView.FreezeRows(1);
        ws.Columns().AdjustToContents(1, Math.Min(r, 500), 8, 60);
        book.SaveAs(path);
    }

    private static string Format(object? value) => value switch
    {
        null => "",
        DateTime d => d.ToString("dd.MM.yyyy HH:mm", Ru),
        IFormattable f => f.ToString(null, Ru),
        _ => value.ToString() ?? "",
    };
}
