using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using SEBlueprint.App.Services;
using SEBlueprint.App.ViewModels;
using SEBlueprint.Core;

namespace SEBlueprint.App.Pages;

public partial class BlueprintsPage : Page
{
    readonly ListCollectionView? _view;

    public BlueprintsPage()
    {
        InitializeComponent();
        DataContext = AppState.Current;

        _view = (ListCollectionView)CollectionViewSource.GetDefaultView(AppState.Current.Rows);
        _view.Filter = Matches;
        List.ItemsSource = _view;
        ApplySort();
        AppState.Current.Rows.CollectionChanged += (_, _) => UpdateCount();
        AppState.Current.PropertyChanged += OnAppChanged;
        UpdateCount();
    }

    void OnAppChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_view != null && e.PropertyName is nameof(AppState.IsBusy) or nameof(AppState.ActiveProfile))
        {
            _view.Refresh();
            UpdateCount();
            if (List.SelectedItem == null && AppState.Current.Selected == null && _view.Count > 0 && !AppState.Current.IsBusy)
                List.SelectedIndex = 0;
            else Detail.Show();
        }
    }

    public void Select(LibraryRow row)
    {
        List.SelectedItem = row;
        List.ScrollIntoView(row);
    }

    bool Matches(object o)
    {
        if (o is not LibraryRow r) return false;
        var text = Search?.Text?.Trim();
        if (!string.IsNullOrEmpty(text) && !r.Name.Contains(text, StringComparison.OrdinalIgnoreCase)) return false;
        if (GameFilter?.SelectedIndex is 1 && r.Game != "SE1") return false;
        if (GameFilter?.SelectedIndex is 2 && r.Game != "SE2") return false;
        var source = (SourceFilter?.SelectedItem as ComboBoxItem)?.Content as string;
        if (SourceFilter?.SelectedIndex > 0 && !string.Equals(r.Source, source, StringComparison.OrdinalIgnoreCase)) return false;
        return true;
    }

    void OnFilterChanged(object sender, RoutedEventArgs e)
    {
        _view?.Refresh();
        UpdateCount();
    }

    void OnSortChanged(object sender, SelectionChangedEventArgs e) => ApplySort();

    void ApplySort()
    {
        if (_view == null || SortBox == null) return;
        _view.CustomSort = SortBox.SelectedIndex switch
        {
            1 => Comparer<LibraryRow>(r => r.Name, false),
            2 => Comparer<LibraryRow>(r => r.Pcu ?? -1, true),
            3 => Comparer<LibraryRow>(r => r.Blocks ?? -1, true),
            4 => Comparer<LibraryRow>(r => r.Guns ?? -1, true),
            5 => Comparer<LibraryRow>(r => (int)r.Status, true),
            _ => Comparer<LibraryRow>(r => r.Modified, true),
        };
    }

    static System.Collections.IComparer Comparer<T>(Func<T, IComparable> key, bool descending) =>
        new KeyComparer<T>(key, descending);

    sealed class KeyComparer<T>(Func<T, IComparable> key, bool descending) : System.Collections.IComparer
    {
        public int Compare(object? x, object? y)
        {
            if (x is not T a || y is not T b) return 0;
            var c = key(a).CompareTo(key(b));
            return descending ? -c : c;
        }
    }

    void UpdateCount()
    {
        if (CountText != null && _view != null) CountText.Text = $"{_view.Count} / {AppState.Current.Rows.Count}";
    }

    async void OnRescan(object sender, RoutedEventArgs e) => await AppState.Current.InitializeAsync();

    void OnSelected(object sender, SelectionChangedEventArgs e)
    {
        if (List.SelectedItem is not LibraryRow row) return;
        AppState.Current.Selected = row;
        Detail.Show();
    }

    void OnOpenFolder(object sender, RoutedEventArgs e)
    {
        if (List.SelectedItem is not LibraryRow row) return;
        try { Process.Start(new ProcessStartInfo("explorer.exe", $"\"{row.Entry.Folder}\"") { UseShellExecute = true }); }
        catch (Exception ex) { Log.Write($"Cannot open folder: {ex.Message}"); }
    }
}
