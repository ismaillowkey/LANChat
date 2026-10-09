using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using LanChat.Core.Services;

namespace LanChat.Services;

public class AvaloniaFilePickerService : IFilePickerService
{
    private readonly Func<IStorageProvider?> _storageProviderProvider;

    public AvaloniaFilePickerService(Func<IStorageProvider?> storageProviderProvider)
    {
        _storageProviderProvider = storageProviderProvider;
    }

    public async Task<string?> PickImageFileAsync()
    {
        var sp = _storageProviderProvider();
        if (sp == null) return null;

        var options = new FilePickerOpenOptions
        {
            Title = "Pilih Gambar untuk Dikirim",
            AllowMultiple = false,
            FileTypeFilter = new List<FilePickerFileType>
            {
                new("File Gambar (*.jpg, *.png, *.webp)")
                {
                    Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.webp", "*.bmp", "*.gif" },
                    MimeTypes = new[] { "image/*" }
                },
                new("Semua File (*.*)") { Patterns = new[] { "*.*" } }
            }
        };

        var files = await sp.OpenFilePickerAsync(options);
        var file = files.FirstOrDefault();
        if (file == null) return null;

        return await ResolveStorageFilePathAsync(file, "img");
    }

    public async Task<string?> PickVideoFileAsync()
    {
        var sp = _storageProviderProvider();
        if (sp == null) return null;

        var options = new FilePickerOpenOptions
        {
            Title = "Pilih Video untuk Dikirim",
            AllowMultiple = false,
            FileTypeFilter = new List<FilePickerFileType>
            {
                new("File Video (*.mp4, *.mkv, *.avi, *.mov, *.webm)")
                {
                    Patterns = new[] { "*.mp4", "*.mkv", "*.avi", "*.mov", "*.webm", "*.flv", "*.m4v" },
                    MimeTypes = new[] { "video/*" }
                },
                new("Semua File (*.*)") { Patterns = new[] { "*.*" } }
            }
        };

        var files = await sp.OpenFilePickerAsync(options);
        var file = files.FirstOrDefault();
        if (file == null) return null;

        return await ResolveStorageFilePathAsync(file, "vid");
    }

    private static async Task<string?> ResolveStorageFilePathAsync(IStorageFile file, string prefix)
    {
        // On non-Android platforms, if a valid physical path exists and is readable, use it directly
        if (!OperatingSystem.IsAndroid())
        {
            string? localPath = file.TryGetLocalPath();
            if (!string.IsNullOrEmpty(localPath) && File.Exists(localPath))
            {
                return localPath;
            }
        }

        try
        {
            string rawName = file.Name ?? string.Empty;
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                rawName = rawName.Replace(c, '_');
            }

            string ext = Path.GetExtension(rawName);
            if (string.IsNullOrEmpty(ext))
            {
                ext = prefix == "img" ? ".jpg" : ".mp4";
            }

            string baseName = Path.GetFileNameWithoutExtension(rawName);
            if (string.IsNullOrWhiteSpace(baseName))
            {
                baseName = $"{prefix}_{DateTime.Now:yyyyMMdd_HHmmss}";
            }

            string targetDir = StoragePaths.MediaDirectory;
            Directory.CreateDirectory(targetDir);
            string destPath = Path.Combine(targetDir, $"{Guid.NewGuid():N}_{baseName}{ext}");

            await using var srcStream = await file.OpenReadAsync();
            await using var dstStream = File.Create(destPath);
            await srcStream.CopyToAsync(dstStream);

            return destPath;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to resolve picked storage file: {ex.Message}");
            return null;
        }
    }
}
