namespace ValheimModManager.App.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using ValheimModManager.Core.Models;

public partial class InstalledModItemViewModel : ObservableObject
{
    public InstalledMod Model { get; }

    [ObservableProperty]
    private bool _isEnabled;

    [ObservableProperty]
    private bool _hasUpdate;

    [ObservableProperty]
    private string _latestVersion;

    public string Name => Model.CanonicalId.Name;
    public string Owner => Model.CanonicalId.Namespace;
    public string InstalledVersion => Model.InstalledVersion;
    public string ProviderId => Model.Key.ProviderId;
    public string KeyString => Model.Key.ToString();

    public InstalledModItemViewModel(InstalledMod model, string latestVersion = "", bool hasUpdate = false)
    {
        Model = model;
        _isEnabled = model.IsEnabled;
        _latestVersion = string.IsNullOrEmpty(latestVersion) ? model.InstalledVersion : latestVersion;
        _hasUpdate = hasUpdate;
    }
}
