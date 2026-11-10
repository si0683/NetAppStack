using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using ScottPlot;
using StarterApp.Services;
using StarterApp.ViewModels;

namespace StarterApp.Views;

/// <summary>График: сколько записей появилось по дням за 60 дней. Образец для своих графиков.</summary>
public partial class ChartView : UserControl
{
    private ThemeService? _theme;

    public ChartView()
    {
        InitializeComponent();
        DataContextChanged += (_, e) =>
        {
            if (e.OldValue is ItemsViewModel old)
                old.DataChanged -= Redraw;
            if (e.NewValue is ItemsViewModel vm)
                vm.DataChanged += Redraw;
            Redraw();
        };
        Loaded += (_, _) =>
        {
            if (_theme is null && System.Windows.Application.Current is App app)
            {
                _theme = app.Services.GetRequiredService<ThemeService>();
                _theme.Changed += Redraw;
            }
            Redraw();
        };
    }

    private void Redraw()
    {
        if (DataContext is not ItemsViewModel vm)
            return;

        var from = DateTime.Today.AddDays(-59);
        var counts = vm.Rows
            .Where(r => r.Date >= from)
            .GroupBy(r => r.Date.Date)
            .ToDictionary(g => g.Key, g => g.Count());
        var days = Enumerable.Range(0, 60).Select(i => from.AddDays(i)).ToArray();

        var plot = Plot.Plot;
        plot.Clear();
        var bars = plot.Add.Bars(
            days.Select(d => d.ToOADate()).ToArray(),
            days.Select(d => (double)counts.GetValueOrDefault(d)).ToArray());
        foreach (var bar in bars.Bars)
            bar.Size = 0.8;
        plot.Axes.DateTimeTicksBottom();
        plot.Axes.Left.TickGenerator = new ScottPlot.TickGenerators.NumericAutomatic { IntegerTicksOnly = true };
        plot.Axes.Margins(bottom: 0);
        plot.Title("Новые записи по дням");
        plot.Font.Automatic(); // шрифт с кириллицей

        bool dark = _theme?.IsDark ?? false;
        var text = dark ? Color.FromHex("#E6E6E6") : Color.FromHex("#202020");
        plot.FigureBackground.Color = Colors.Transparent;
        plot.DataBackground.Color = dark ? Color.FromHex("#1F1F1F") : Colors.White;
        plot.Axes.Color(text);
        plot.Grid.MajorLineColor = text.WithAlpha(0.12);
        Plot.Refresh();
    }
}
