using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using StarterApp.Core.Settings;
using StarterApp.Services;
using StarterApp.ViewModels;

namespace StarterApp.Views;

/// <summary>
/// Таблица записей. Здесь только то, что нельзя сделать привязками:
/// сохранение колонок и сортировки, меню «Колонки», открытие ссылок.
/// Ключ колонки — её SortMemberPath (он же ключ в ItemsViewModel.ExportColumns).
/// </summary>
public partial class ItemsView : UserControl
{
    public ItemsView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
        {
            if (DataContext is ItemsViewModel vm)
                vm.VisibleColumns = VisibleColumnKeys;
        };
    }

    private ItemsViewModel? Vm => DataContext as ItemsViewModel;

    private IReadOnlyList<string> VisibleColumnKeys() =>
        Grid.Columns.Where(c => c.Visibility == Visibility.Visible)
            .OrderBy(c => c.DisplayIndex)
            .Select(c => c.SortMemberPath)
            .ToList();

    public void RestoreLayout(GridState state)
    {
        foreach (var saved in state.Columns.OrderBy(c => c.DisplayIndex))
        {
            var column = Grid.Columns.FirstOrDefault(c => c.SortMemberPath == saved.Key);
            if (column is null)
                continue;
            column.Visibility = saved.Visible ? Visibility.Visible : Visibility.Collapsed;
            if (saved.Width > 20 && column.Width.UnitType != DataGridLengthUnitType.Star)
                column.Width = saved.Width;
            if (saved.DisplayIndex >= 0 && saved.DisplayIndex < Grid.Columns.Count)
                column.DisplayIndex = saved.DisplayIndex;
        }

        // по умолчанию — новые сверху
        string sortKey = state.SortColumn ?? "Date";
        var direction = state.SortColumn is null || state.SortDescending ? ListSortDirection.Descending : ListSortDirection.Ascending;
        var sortColumn = Grid.Columns.FirstOrDefault(c => c.SortMemberPath == sortKey);
        if (sortColumn is not null && Vm is { } vm)
        {
            vm.View.SortDescriptions.Clear();
            vm.View.SortDescriptions.Add(new SortDescription(sortKey, direction));
            sortColumn.SortDirection = direction;
        }
    }

    public void SaveLayout(GridState state)
    {
        state.Columns = Grid.Columns.Select(c => new ColumnState
        {
            Key = c.SortMemberPath,
            DisplayIndex = c.DisplayIndex,
            Width = c.Width.UnitType == DataGridLengthUnitType.Star ? 0 : c.ActualWidth,
            Visible = c.Visibility == Visibility.Visible,
        }).ToList();

        if (Vm?.View.SortDescriptions.FirstOrDefault() is { PropertyName: not null } sort)
        {
            state.SortColumn = sort.PropertyName;
            state.SortDescending = sort.Direction == ListSortDirection.Descending;
        }
    }

    private void OnColumns(object sender, RoutedEventArgs e)
    {
        var menu = new ContextMenu { PlacementTarget = ColumnsButton, Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom };
        foreach (var column in Grid.Columns.OrderBy(c => c.DisplayIndex))
        {
            var item = new MenuItem
            {
                Header = column.Header,
                IsCheckable = true,
                IsChecked = column.Visibility == Visibility.Visible,
                StaysOpenOnClick = true,
            };
            item.Click += (_, _) => column.Visibility = item.IsChecked ? Visibility.Visible : Visibility.Collapsed;
            menu.Items.Add(item);
        }
        menu.IsOpen = true;
    }

    private void OnLink(object sender, RoutedEventArgs e)
    {
        if (sender is Hyperlink { Tag: string url } && Uri.IsWellFormedUriString(url, UriKind.Absolute))
            Shell.Open(url);
    }

    private void OnRowDoubleClick(object sender, MouseButtonEventArgs e)
    {
        // двойной щелчок по строке (не по редактируемой ячейке) — открыть ссылку
        if (e.OriginalSource is FrameworkElement { DataContext: ItemRow row }
            && Grid.CurrentColumn is { IsReadOnly: true }
            && Uri.IsWellFormedUriString(row.Url, UriKind.Absolute))
            Shell.Open(row.Url!);
    }
}
