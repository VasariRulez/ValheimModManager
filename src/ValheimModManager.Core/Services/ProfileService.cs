namespace ValheimModManager.Core.Services;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ValheimModManager.Core.Models;

public sealed record AppState(
    string ActiveProfile = "Default",
    string? CustomGamePath = null,
    string? CustomLaunchArgs = null,
    string? Language = null,
    bool LaunchViaSteam = true
);

public sealed class ProfileService
{
    private readonly string _baseProfilesDirectory;
    private readonly string _stateFilePath;
    private readonly ILogger<ProfileService> _logger;

    public ProfileService(
        string? baseProfilesDirectory = null,
        ILogger<ProfileService>? logger = null)
    {
        _logger = logger ?? NullLogger<ProfileService>.Instance;
        _baseProfilesDirectory = baseProfilesDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ValheimModManager", "profiles");

        _stateFilePath = Path.Combine(
            Path.GetDirectoryName(_baseProfilesDirectory) ?? _baseProfilesDirectory,
            "state.json");

        Directory.CreateDirectory(_baseProfilesDirectory);
        EnsureDefaultProfileExists();
    }

    public string BaseProfilesDirectory => _baseProfilesDirectory;

    public string GetProfileDirectory(string profileName) =>
        Path.Combine(_baseProfilesDirectory, SanitizeFolderName(profileName));

    public string GetProfileBepInExDirectory(string profileName) =>
        Path.Combine(GetProfileDirectory(profileName), "BepInEx");

    public IReadOnlyList<string> ListProfileNames()
    {
        return Directory.GetDirectories(_baseProfilesDirectory)
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrEmpty(name))
            .ToList()!;
    }

    public Profile GetProfile(string profileName)
    {
        var dir = GetProfileDirectory(profileName);
        var jsonPath = Path.Combine(dir, "profile.json");

        if (File.Exists(jsonPath))
        {
            try
            {
                var json = File.ReadAllText(jsonPath);
                var p = JsonSerializer.Deserialize<Profile>(json);
                if (p != null) return p;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load profile.json for {Profile}", profileName);
            }
        }

        var defaultProfile = Profile.CreateDefault(profileName);
        SaveProfile(defaultProfile);
        return defaultProfile;
    }

    public void SaveProfile(Profile profile)
    {
        var dir = GetProfileDirectory(profile.Name);
        Directory.CreateDirectory(dir);
        var jsonPath = Path.Combine(dir, "profile.json");
        var updated = profile with { LastModified = DateTime.UtcNow };

        var json = JsonSerializer.Serialize(updated, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(jsonPath, json);
    }

    public Profile CreateProfile(string name, GameTarget target = GameTarget.Client)
    {
        var sanitized = SanitizeFolderName(name);
        var dir = GetProfileDirectory(sanitized);

        if (Directory.Exists(dir))
        {
            throw new InvalidOperationException($"A profile named '{sanitized}' already exists.");
        }

        Directory.CreateDirectory(dir);
        Directory.CreateDirectory(Path.Combine(dir, "BepInEx", "plugins"));
        Directory.CreateDirectory(Path.Combine(dir, "BepInEx", "config"));

        var profile = Profile.CreateDefault(sanitized, target);
        SaveProfile(profile);
        return profile;
    }

    public Profile CloneProfile(string sourceName, string newName)
    {
        var sourceProfile = GetProfile(sourceName);
        var newProfile = CreateProfile(newName, sourceProfile.Target);

        var sourceDir = GetProfileDirectory(sourceName);
        var targetDir = GetProfileDirectory(newName);

        // Copy BepInEx folder contents
        CopyDirectory(Path.Combine(sourceDir, "BepInEx"), Path.Combine(targetDir, "BepInEx"));

        // Copy mod list
        var clonedMods = sourceProfile.Mods.Select(m => m with {
            InstalledFiles = m.InstalledFiles.Select(f => f.Replace(sourceDir, targetDir)).ToList()
        }).ToList();

        var updated = newProfile with { Mods = clonedMods };
        SaveProfile(updated);
        return updated;
    }

    public void DeleteProfile(string profileName)
    {
        var sanitized = SanitizeFolderName(profileName);
        if (sanitized.Equals("Default", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Cannot delete the Default profile.");
        }

        var dir = GetProfileDirectory(sanitized);
        if (Directory.Exists(dir))
        {
            Directory.Delete(dir, recursive: true);
        }

        if (GetActiveProfileName().Equals(sanitized, StringComparison.OrdinalIgnoreCase))
        {
            SetActiveProfile("Default");
        }
    }

    public void ExportProfile(string profileName, string destinationZipPath)
    {
        var profileDir = GetProfileDirectory(profileName);
        if (!Directory.Exists(profileDir))
        {
            throw new DirectoryNotFoundException($"Profile {profileName} not found.");
        }

        var tempDir = Path.Combine(Path.GetTempPath(), "VMM_Export_" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(tempDir);

            // Copy profile.json
            var profileJson = Path.Combine(profileDir, "profile.json");
            if (File.Exists(profileJson))
            {
                File.Copy(profileJson, Path.Combine(tempDir, "profile.json"));
            }

            // Copy BepInEx/config
            var configDir = Path.Combine(profileDir, "BepInEx", "config");
            if (Directory.Exists(configDir))
            {
                var targetConfig = Path.Combine(tempDir, "BepInEx", "config");
                CopyDirectory(configDir, targetConfig);
            }

            if (File.Exists(destinationZipPath)) File.Delete(destinationZipPath);
            System.IO.Compression.ZipFile.CreateFromDirectory(tempDir, destinationZipPath);
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
        }
    }

    public Profile ImportProfile(string sourceZipPath, string? targetProfileName = null)
    {
        using var archive = System.IO.Compression.ZipFile.OpenRead(sourceZipPath);
        var profileEntry = archive.GetEntry("profile.json");
        if (profileEntry == null)
        {
            throw new InvalidOperationException("Invalid .vmmprofile: missing profile.json entry.");
        }

        Profile imported;
        using (var stream = profileEntry.Open())
        {
            imported = JsonSerializer.Deserialize<Profile>(stream)
                       ?? throw new InvalidOperationException("Failed to parse profile.json from archive.");
        }

        var finalName = string.IsNullOrWhiteSpace(targetProfileName) ? imported.Name : targetProfileName;
        finalName = SanitizeFolderName(finalName);

        // If profile already exists, generate a unique name
        var existingNames = ListProfileNames();
        var uniqueName = finalName;
        int counter = 2;
        while (existingNames.Contains(uniqueName, StringComparer.OrdinalIgnoreCase))
        {
            uniqueName = $"{finalName} ({counter++})";
        }

        var newProfile = CreateProfile(uniqueName, imported.Target);
        var targetDir = GetProfileDirectory(uniqueName);

        // Extract configs if present
        foreach (var entry in archive.Entries)
        {
            if (entry.FullName.StartsWith("BepInEx/config/", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(entry.Name))
            {
                var rel = entry.FullName["BepInEx/config/".Length..];
                var dest = Path.Combine(targetDir, "BepInEx", "config", rel.Replace('/', Path.DirectorySeparatorChar));
                var dir = Path.GetDirectoryName(dest);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                entry.ExtractToFile(dest, overwrite: true);
            }
        }

        var updated = newProfile with { Mods = imported.Mods };
        SaveProfile(updated);
        return updated;
    }

    public string GetActiveProfileName()
    {
        if (File.Exists(_stateFilePath))
        {
            try
            {
                var state = JsonSerializer.Deserialize<AppState>(File.ReadAllText(_stateFilePath));
                if (state != null && !string.IsNullOrWhiteSpace(state.ActiveProfile))
                {
                    return state.ActiveProfile;
                }
            }
            catch
            {
                // ignore
            }
        }
        return "Default";
    }

    public void SetActiveProfile(string profileName)
    {
        var state = LoadState();
        var updated = state with { ActiveProfile = profileName };
        File.WriteAllText(_stateFilePath, JsonSerializer.Serialize(updated, new JsonSerializerOptions { WriteIndented = true }));
    }

    public AppState LoadState()
    {
        if (File.Exists(_stateFilePath))
        {
            try
            {
                var state = JsonSerializer.Deserialize<AppState>(File.ReadAllText(_stateFilePath));
                if (state != null) return state;
            }
            catch
            {
                // ignore
            }
        }
        return new AppState();
    }

    public void SaveState(AppState state)
    {
        var dir = Path.GetDirectoryName(_stateFilePath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(_stateFilePath, JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true }));
    }

    private void EnsureDefaultProfileExists()
    {
        var defaultDir = GetProfileDirectory("Default");
        if (!Directory.Exists(defaultDir))
        {
            CreateProfile("Default");
        }
    }

    private static string SanitizeFolderName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(name.Where(c => !invalid.Contains(c)).ToArray()).Trim();
    }

    private static void CopyDirectory(string sourceDir, string targetDir)
    {
        if (!Directory.Exists(sourceDir)) return;
        Directory.CreateDirectory(targetDir);

        foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(sourceDir, file);
            var dest = Path.Combine(targetDir, relative);
            var dir = Path.GetDirectoryName(dest);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.Copy(file, dest, overwrite: true);
        }
    }
}
