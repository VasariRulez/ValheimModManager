namespace ValheimModManager.App.ViewModels;

using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using ValheimModManager.Core.Models;

public partial class CatalogModItemViewModel : ObservableObject
{
    public ModSummary Summary { get; }

    [ObservableProperty]
    private string _selectedVersion;

    [ObservableProperty]
    private bool _isInstalling;

    public string Name => Summary.Name;
    public string Owner => Summary.Owner;
    public string Description => Summary.Description;
    public string PackageUrl => Summary.PackageUrl;
    public string IconUrl => Summary.IconUrl;
    public string ProviderId => Summary.Key.ProviderId;
    public long TotalDownloads => Summary.TotalDownloads;
    public int RatingScore => Summary.RatingScore;
    public IReadOnlyList<string> Categories => Summary.Categories;
    public IReadOnlyList<string> AvailableVersions { get; }

    public CatalogModItemViewModel(ModSummary summary)
    {
        Summary = summary;
        AvailableVersions = summary.Versions.Select(v => v.VersionNumber).ToList();
        _selectedVersion = summary.LatestVersionNumber;
    }
}
