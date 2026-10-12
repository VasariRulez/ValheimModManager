namespace ValheimModManager.App.ViewModels;

using System;
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
    public bool IsManualMod => string.Equals(Model.Key.ProviderId, "manual", StringComparison.OrdinalIgnoreCase);
    public string ProviderBadgeText => IsManualMod 
        ? ValheimModManager.Core.Localization.LocalizationService.Instance.CurrentStrings.ManualModBadge 
        : ProviderId;
    public string ProviderBadgeColor => IsManualMod ? "#7c3aed" : "#3f3f46";
    public string? UpdateProviderId { get; }
    public string KeyString => Model.Key.ToString();
    public string AuthorDisplay => string.Format(ValheimModManager.Core.Localization.LocalizationService.Instance.CurrentStrings.ModAuthorPrefix, Owner);
    public string UpdateBadgeText
    {
        get
        {
            var baseText = string.Format(ValheimModManager.Core.Localization.LocalizationService.Instance.CurrentStrings.ModUpdateAvailableBadge, LatestVersion);
            if (!string.IsNullOrEmpty(UpdateProviderId) &&
                !UpdateProviderId.Equals(ProviderId, StringComparison.OrdinalIgnoreCase))
            {
                var storeName = UpdateProviderId.Equals("thunderstore", StringComparison.OrdinalIgnoreCase)
                    ? "⚡ Thunderstore"
                    : UpdateProviderId.Equals("hexium", StringComparison.OrdinalIgnoreCase)
                        ? "🔷 Hexium"
                        : UpdateProviderId;
                return $"{baseText} ({storeName})";
            }
            return baseText;
        }
    }

    private readonly Action<InstalledModItemViewModel, bool>? _onToggle;
    private bool _suppressToggleCallback;

    public InstalledModItemViewModel(
        InstalledMod model, 
        string latestVersion = "", 
        bool hasUpdate = false,
        Action<InstalledModItemViewModel, bool>? onToggle = null,
        string? updateProviderId = null)
    {
        Model = model;
        _isEnabled = model.IsEnabled;
        _latestVersion = string.IsNullOrEmpty(latestVersion) ? model.InstalledVersion : latestVersion;
        _hasUpdate = hasUpdate;
        _onToggle = onToggle;
        UpdateProviderId = updateProviderId;
    }

    partial void OnIsEnabledChanged(bool value)
    {
        if (_suppressToggleCallback) return;
        _onToggle?.Invoke(this, value);
    }

    public void SetIsEnabledSilently(bool value)
    {
        _suppressToggleCallback = true;
        IsEnabled = value;
        _suppressToggleCallback = false;
    }
}
