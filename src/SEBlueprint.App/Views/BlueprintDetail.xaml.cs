using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using SEBlueprint.App.Services;
using SEBlueprint.App.ViewModels;
using SEBlueprint.Core;
using SEBlueprint.Core.Limits;

namespace SEBlueprint.App.Views;

public partial class BlueprintDetail : UserControl
{
    OverviewViewModel _vm = new();
    bool _loading;

    public BlueprintDetail()
    {
        InitializeComponent();
        ProfileBox.ItemsSource = AppState.Current.Profiles;
        Show();
    }

    public void Show()
    {
        _loading = true;
        _vm = new OverviewViewModel();
        DataContext = _vm;
        ProfileBox.SelectedItem = AppState.Current.ActiveProfile;
        _loading = false;
    }

    void OnProfileChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading || ProfileBox.SelectedItem is not LimitProfile p) return;
        AppState.Current.ActiveProfile = p;
        _vm.Rebuild();
    }

    void OnCopy(object sender, RoutedEventArgs e) => TryClipboard(_vm.CostAsText('\t'));

    void OnCopyMods(object sender, RoutedEventArgs e) =>
        TryClipboard(string.Join(Environment.NewLine, _vm.Report?.MissingMods ?? new List<string>()));

    void OnExport(object sender, RoutedEventArgs e)
    {
        var dlg = new SaveFileDialog
        {
            Filter = "CSV file (*.csv)|*.csv",
            FileName = $"{Sanitize(_vm.Title)} - cost.csv",
        };
        if (dlg.ShowDialog() != true) return;
        try { File.WriteAllText(dlg.FileName, _vm.CostAsText(','), new UTF8Encoding(true)); }
        catch (Exception ex)
        {
            Log.Write($"CSV export failed: {ex.Message}");
            AppState.Current.ErrorMessage = $"Could not save the file: {ex.Message}";
        }
    }

    static void TryClipboard(string text)
    {
        try { Clipboard.SetText(text); }
        catch (Exception ex) { Log.Write($"Clipboard failed: {ex.Message}"); }
    }

    static string Sanitize(string name) =>
        new(name.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c).ToArray());
}
