using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using SEBlueprint.App.Services;
using SEBlueprint.App.ViewModels;
using SEBlueprint.Core;
using SEBlueprint.Core.Limits;

namespace SEBlueprint.App.Pages;

public partial class ProfilesPage : Page
{
    static AppState App => AppState.Current;
    ProfileEditorViewModel? _editor;

    public ProfilesPage()
    {
        InitializeComponent();
        ProfileList.ItemsSource = App.Profiles;
        ProfileList.SelectedItem = App.ActiveProfile;
    }

    void OnProfileSelected(object sender, SelectionChangedEventArgs e)
    {
        if (ProfileList.SelectedItem is not LimitProfile p) { EditorCard.DataContext = null; return; }
        _editor = new ProfileEditorViewModel(p);
        EditorCard.DataContext = _editor;
    }

    void OnNew(object sender, RoutedEventArgs e) => AddUserProfile(new LimitProfile { Name = UniqueName("My server") });

    void OnDuplicate(object sender, RoutedEventArgs e)
    {
        if (ProfileList.SelectedItem is LimitProfile p) AddUserProfile(p.Clone(UniqueName(p.Name.Replace("Vanilla – ", "") + " (copy)")));
    }

    void OnDelete(object sender, RoutedEventArgs e)
    {
        if (ProfileList.SelectedItem is not LimitProfile { BuiltIn: false } p) return;
        Try(() => ProfileStore.Delete(p.Name));
        App.Profiles.Remove(p);
        if (App.ActiveProfile == p) App.ActiveProfile = App.Profiles.FirstOrDefault();
        ProfileList.SelectedItem = App.Profiles.FirstOrDefault();
    }

    void OnSave(object sender, RoutedEventArgs e)
    {
        if (_editor == null || _editor.IsReadOnly) return;
        if (!_editor.TryToProfile(out var updated, out var error))
        {
            App.ErrorMessage = error;
            return;
        }
        var old = _editor.Original;
        if (updated.Name != old.Name && App.Profiles.Any(p => p != old && p.Name == updated.Name))
            updated.Name = UniqueName(updated.Name);
        Try(() =>
        {
            if (updated.Name != old.Name) ProfileStore.Delete(old.Name);
            ProfileStore.Save(updated);
        });
        var index = App.Profiles.IndexOf(old);
        var wasActive = App.ActiveProfile == old;
        if (index >= 0) App.Profiles[index] = updated; else App.Profiles.Add(updated);
        if (wasActive) App.ActiveProfile = updated;
        ProfileList.SelectedItem = updated;
    }

    void OnImport(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog { Filter = "Limit profile (*.json)|*.json" };
        if (dlg.ShowDialog() != true) return;
        try
        {
            var p = ProfileStore.FromJson(File.ReadAllText(dlg.FileName));
            p.Name = UniqueName(p.Name);
            AddUserProfile(p);
        }
        catch (Exception ex) { App.ErrorMessage = $"That file is not a valid limit profile: {ex.Message}"; }
    }

    void OnExport(object sender, RoutedEventArgs e)
    {
        if (ProfileList.SelectedItem is not LimitProfile p) return;
        var dlg = new SaveFileDialog { Filter = "Limit profile (*.json)|*.json", FileName = p.Name + ".json" };
        if (dlg.ShowDialog() != true) return;
        Try(() => File.WriteAllText(dlg.FileName, ProfileStore.ToJson(p)));
    }

    void AddUserProfile(LimitProfile p)
    {
        p.BuiltIn = false;
        Try(() => ProfileStore.Save(p));
        App.Profiles.Add(p);
        ProfileList.SelectedItem = p;
    }

    static string UniqueName(string name)
    {
        var candidate = name;
        for (var i = 2; App.Profiles.Any(p => p.Name == candidate); i++) candidate = $"{name} {i}";
        return candidate;
    }

    static void Try(Action a)
    {
        try { a(); }
        catch (Exception ex)
        {
            Log.Write($"Settings action failed: {ex.Message}");
            App.ErrorMessage = ex.Message;
        }
    }
}
