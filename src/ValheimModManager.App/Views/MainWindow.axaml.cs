namespace ValheimModManager.App.Views;

using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using ValheimModManager.App.ViewModels;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.OnCopyToClipboardRequested = async text =>
            {
                var cb = TopLevel.GetTopLevel(this)?.Clipboard;
                if (cb != null)
                {
                    await cb.SetTextAsync(text);
                }
            };
        }
    }

    private async void OnBrowseGameFolderClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;

        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Seleziona la cartella di installazione di Valheim (contenente valheim.exe)",
            AllowMultiple = false
        });

        if (folders.Count > 0)
        {
            var selectedPath = folders[0].Path.LocalPath;
            vm.SetCustomGamePath(selectedPath);
        }
    }

    private async void OnExportProfileClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Esporta Profilo",
            DefaultExtension = "vmmprofile",
            SuggestedFileName = $"{vm.SelectedProfile}.vmmprofile",
            FileTypeChoices = new List<FilePickerFileType>
            {
                new("Valheim Mod Manager Profile (*.vmmprofile)") { Patterns = ["*.vmmprofile"] }
            }
        });

        if (file != null)
        {
            vm.ExportActiveProfile(file.Path.LocalPath);
        }
    }

    private async void OnExportServerPackageClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Esporta Pacchetto Server Dedicato",
            DefaultExtension = "zip",
            SuggestedFileName = $"{vm.SelectedProfile}-DedicatedServer.zip",
            FileTypeChoices = new List<FilePickerFileType>
            {
                new("Dedicated Server Modpack (*.zip)") { Patterns = ["*.zip"] }
            }
        });

        if (file != null)
        {
            vm.ExportServerPackage(file.Path.LocalPath);
        }
    }

    private async void OnImportVmmProfileClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;

        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Seleziona file profilo (.vmmprofile)",
            AllowMultiple = false,
            FileTypeFilter = new List<FilePickerFileType>
            {
                new("Valheim Mod Manager Profile (*.vmmprofile)") { Patterns = ["*.vmmprofile"] }
            }
        });

        if (files.Count > 0)
        {
            vm.ImportVmmProfileFile(files[0].Path.LocalPath);
        }
    }

    private async void OnImportR2zClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;

        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Seleziona file esportazione r2modman (.r2z)",
            AllowMultiple = false,
            FileTypeFilter = new List<FilePickerFileType>
            {
                new("r2modman Profile Export (*.r2z)") { Patterns = ["*.r2z", "*.zip"] }
            }
        });

        if (files.Count > 0)
        {
            await vm.ImportR2zFileAsync(files[0].Path.LocalPath);
        }
    }
}