namespace ValheimModManager.App.ViewModels;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using ValheimModManager.App.Services;
using ValheimModManager.Core.Models;

public partial class CatalogModItemViewModel : ObservableObject
{
    public GroupedModSummary Grouped { get; }

    [ObservableProperty]
    private Bitmap? _thumbnail;

    [ObservableProperty]
    private ModSourceRelease _selectedSource;

    [ObservableProperty]
    private string _selectedVersion;

    [ObservableProperty]
    private IReadOnlyList<string> _availableVersions = [];

    [ObservableProperty]
    private bool _isInstalling;

    public string Name => Grouped.Name;
    public string Owner => Grouped.Owner;
    public string Description => Grouped.Description;
    public string IconUrl => Grouped.IconUrl;
    public long TotalDownloads => Grouped.TotalDownloadsOverall;
    public int RatingScore => Grouped.HighestRatingOverall;
    public string AuthorDisplay => string.Format(ValheimModManager.Core.Localization.LocalizationService.Instance.CurrentStrings.ModByPrefix, Owner);
    public string DownloadsDisplay => string.Format(ValheimModManager.Core.Localization.LocalizationService.Instance.CurrentStrings.ModDownloadsLabel, TotalDownloads);
    public string RatingDisplay => string.Format(ValheimModManager.Core.Localization.LocalizationService.Instance.CurrentStrings.ModVotesLabel, RatingScore);
    public System.DateTime DateUpdated => Grouped.DateUpdatedOverall;
    public string DateUpdatedDisplay => DateUpdated > System.DateTime.MinValue
        ? string.Format(ValheimModManager.Core.Localization.LocalizationService.Instance.CurrentStrings.ModUpdatedLabel, DateUpdated.ToLocalTime())
        : string.Empty;
    public IReadOnlyList<string> Categories => Grouped.Categories;
    public IReadOnlyList<ModSourceRelease> AvailableSources => Grouped.AvailableSources;
    public bool HasMultipleSources => AvailableSources.Count > 1;

    public IReadOnlyList<ModSourceRelease> UpToDateSources =>
        AvailableSources.Where(s => s.IsNewestOverall).ToList();

    public IReadOnlyList<ModSourceRelease> OutdatedSources =>
        AvailableSources.Where(s => !s.IsNewestOverall).ToList();

    public bool HasOutdatedSources => OutdatedSources.Count > 0;

    public string OutdatedSourcesTooltip =>
        string.Join(", ", OutdatedSources.Select(s => $"{s.DisplayName} (v{s.LatestVersion})"));

    public string OutdatedSourcesCountText =>
        $"+{OutdatedSources.Count} {(OutdatedSources.Count == 1 ? "fonte precedente" : "fonti precedenti")}";

    public ModSummary Summary => SelectedSource.Summary;
    public string ProviderId => SelectedSource.ProviderId;

    public CatalogModItemViewModel(GroupedModSummary grouped)
    {
        Grouped = grouped;
        _selectedSource = grouped.AvailableSources[0];
        _selectedVersion = _selectedSource.LatestVersion;
        RefreshVersionsList();
    }

    private System.Threading.CancellationTokenSource? _thumbnailCts;
    private bool _isLoadingThumbnail;

    public void EnsureThumbnailLoaded()
    {
        if (Thumbnail != null || _isLoadingThumbnail || string.IsNullOrWhiteSpace(IconUrl)) return;

        _isLoadingThumbnail = true;
        _thumbnailCts?.Cancel();
        _thumbnailCts = new System.Threading.CancellationTokenSource();
        var ct = _thumbnailCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                var bmp = await AvaloniaImageLoader.Instance.LoadImageAsync(IconUrl, ct).ConfigureAwait(false);
                if (bmp != null && !ct.IsCancellationRequested)
                {
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        if (!ct.IsCancellationRequested)
                        {
                            Thumbnail = bmp;
                        }
                    }, Avalonia.Threading.DispatcherPriority.Background);
                }
            }
            catch (System.OperationCanceledException) { }
            catch (System.Exception) { }
            finally
            {
                _isLoadingThumbnail = false;
            }
        }, ct);
    }

    public Task LoadThumbnailAsync(System.Threading.CancellationToken ct = default)
    {
        EnsureThumbnailLoaded();
        return Task.CompletedTask;
    }

    public void DetachThumbnail()
    {
        _thumbnailCts?.Cancel();
        _isLoadingThumbnail = false;
        Thumbnail = null;
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
            ],
            DateUpdatedOverall: singleSummary.DateUpdated
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
        if (SelectedSource?.Summary?.Versions != null && SelectedSource.Summary.Versions.Count > 0)
        {
            AvailableVersions = SelectedSource.Summary.Versions.Select(v => v.VersionNumber).ToList();
        }
        else if (SelectedSource != null)
        {
            AvailableVersions = [SelectedSource.LatestVersion];
        }
        else
        {
            AvailableVersions = [];
        }
    }
}
