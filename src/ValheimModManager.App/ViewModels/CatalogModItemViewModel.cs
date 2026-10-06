namespace ValheimModManager.App.ViewModels;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using ValheimModManager.Core.Models;

public partial class CatalogModItemViewModel : ObservableObject
{
    public GroupedModSummary Grouped { get; }

    [ObservableProperty]
    private ModSourceRelease _selectedSource;

    [ObservableProperty]
    private string _selectedVersion;

    [ObservableProperty]
    private ObservableCollection<string> _availableVersions = [];

    [ObservableProperty]
    private bool _isInstalling;

    public string Name => Grouped.Name;
    public string Owner => Grouped.Owner;
    public string Description => Grouped.Description;
    public string IconUrl => Grouped.IconUrl;
    public long TotalDownloads => Grouped.TotalDownloadsOverall;
    public int RatingScore => Grouped.HighestRatingOverall;
    public IReadOnlyList<string> Categories => Grouped.Categories;
    public IReadOnlyList<ModSourceRelease> AvailableSources => Grouped.AvailableSources;
    public bool HasMultipleSources => AvailableSources.Count > 1;

    public ModSummary Summary => SelectedSource.Summary;
    public string ProviderId => SelectedSource.ProviderId;

    public CatalogModItemViewModel(GroupedModSummary grouped)
    {
        Grouped = grouped;
        _selectedSource = grouped.AvailableSources[0];
        _selectedVersion = _selectedSource.LatestVersion;
        RefreshVersionsList();
    }

    public CatalogModItemViewModel(ModSummary singleSummary)
        : this(new GroupedModSummary(
            CanonicalId: singleSummary.CanonicalId,
            Name: singleSummary.Name,
            Owner: singleSummary.Owner,
            Description: singleSummary.Description,
            IconUrl: singleSummary.IconUrl,
            HighestVersionOverall: singleSummary.LatestVersionNumber,
            TotalDownloadsOverall: singleSummary.TotalDownloads,
            HighestRatingOverall: singleSummary.RatingScore,
            IsPinned: singleSummary.IsPinned,
            IsDeprecated: singleSummary.IsDeprecated,
            Categories: singleSummary.Categories,
            AvailableSources: [
                new ModSourceRelease(
                    singleSummary.Key.ProviderId,
                    singleSummary.Key.ProviderId.Equals("hexium", System.StringComparison.OrdinalIgnoreCase) ? "Hexium" : "Thunderstore",
                    singleSummary,
                    singleSummary.LatestVersionNumber,
                    true
                )
            ]
        ))
    {
    }

    partial void OnSelectedSourceChanged(ModSourceRelease value)
    {
        if (value == null) return;
        RefreshVersionsList();
        SelectedVersion = value.LatestVersion;
        OnPropertyChanged(nameof(Summary));
        OnPropertyChanged(nameof(ProviderId));
    }

    private void RefreshVersionsList()
    {
        AvailableVersions.Clear();
        if (SelectedSource?.Summary?.Versions != null)
        {
            foreach (var v in SelectedSource.Summary.Versions)
            {
                AvailableVersions.Add(v.VersionNumber);
            }
        }
        if (AvailableVersions.Count == 0 && SelectedSource != null)
        {
            AvailableVersions.Add(SelectedSource.LatestVersion);
        }
    }
}
