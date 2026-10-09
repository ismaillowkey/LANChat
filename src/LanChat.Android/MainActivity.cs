using System;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Avalonia;
using Avalonia.Android;

namespace LanChat.Android;

[Activity(
    Label = "LAN Chat",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode,
    WindowSoftInputMode = SoftInput.AdjustResize)]
public class MainActivity : AvaloniaMainActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        try
        {
            string manufacturer = global::Android.OS.Build.Manufacturer ?? "";
            string model = global::Android.OS.Build.Model ?? "Android";
            string defaultDeviceName = model.StartsWith(manufacturer, StringComparison.OrdinalIgnoreCase)
                ? model
                : $"{manufacturer} {model}".Trim();

            LanChat.Core.Services.AppSettingsService.DefaultDeviceNameResolver = () => defaultDeviceName;
            LanChat.Core.Services.NotificationManagerService.Instance = new AndroidNotificationService(this);

            if (global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.Tiramisu)
            {
                if (CheckSelfPermission(global::Android.Manifest.Permission.PostNotifications) != global::Android.Content.PM.Permission.Granted)
                {
                    RequestPermissions(new[] { global::Android.Manifest.Permission.PostNotifications }, 101);
                }
            }

            // Register FileProvider opener for opening media with external apps (video players, image viewers)
            LanChat.Core.Services.PlatformLauncherService.CustomFileOpener = filePath =>
            {
                try
                {
                    var file = new Java.IO.File(filePath);
                    var uri = global::AndroidX.Core.Content.FileProvider.GetUriForFile(this, $"{PackageName}.fileprovider", file);
                    var intent = new global::Android.Content.Intent(global::Android.Content.Intent.ActionView);
                    string ext = System.IO.Path.GetExtension(filePath).ToLowerInvariant();
                    string mimeType = ext switch
                    {
                        ".jpg" or ".jpeg" or ".png" or ".webp" or ".bmp" or ".gif" => "image/*",
                        ".mp4" or ".mkv" or ".avi" or ".mov" or ".webm" or ".3gp" or ".flv" => "video/*",
                        _ => "*/*"
                    };

                    intent.SetDataAndType(uri, mimeType);
                    intent.AddFlags(global::Android.Content.ActivityFlags.GrantReadUriPermission);
                    intent.AddFlags(global::Android.Content.ActivityFlags.NewTask);

                    var chooser = global::Android.Content.Intent.CreateChooser(intent, "Buka Media");
                    chooser?.AddFlags(global::Android.Content.ActivityFlags.NewTask);
                    if (chooser != null)
                    {
                        StartActivity(chooser);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to open media via FileProvider: {ex.Message}");
                }
            };

            // Register file sharer (for sharing images/media from the app via WhatsApp, Telegram, etc.)
            LanChat.Core.Services.PlatformLauncherService.CustomFileSharer = (filePath, mimeType) =>
            {
                try
                {
                    var file = new Java.IO.File(filePath);
                    var uri = AndroidX.Core.Content.FileProvider.GetUriForFile(
                        this,
                        $"{PackageName}.fileprovider",
                        file);

                    var shareIntent = new global::Android.Content.Intent(global::Android.Content.Intent.ActionSend);
                    string ext = System.IO.Path.GetExtension(filePath).ToLowerInvariant();
                    string type = !string.IsNullOrEmpty(mimeType) ? mimeType : (ext is ".mp4" or ".mkv" or ".avi" ? "video/*" : "image/*");
                    shareIntent.SetType(type);
                    shareIntent.PutExtra(global::Android.Content.Intent.ExtraStream, uri);
                    shareIntent.AddFlags(global::Android.Content.ActivityFlags.GrantReadUriPermission);
                    shareIntent.AddFlags(global::Android.Content.ActivityFlags.NewTask);

                    var chooser = global::Android.Content.Intent.CreateChooser(shareIntent, "Bagikan Foto");
                    chooser?.AddFlags(global::Android.Content.ActivityFlags.NewTask);
                    if (chooser != null)
                    {
                        StartActivity(chooser);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to share file: {ex.Message}");
                }
            };

            // Register custom file saver (saves image to public Downloads/LanChat and shows Toast)
            LanChat.Core.Services.PlatformLauncherService.CustomFileSaver = (filePath, defaultName) =>
            {
                try
                {
                    var downloadDir = global::Android.OS.Environment.GetExternalStoragePublicDirectory(
                        global::Android.OS.Environment.DirectoryDownloads);
                    var targetFolder = new Java.IO.File(downloadDir, "LanChat");
                    if (!targetFolder.Exists()) targetFolder.Mkdirs();

                    string fileName = !string.IsNullOrEmpty(defaultName) ? defaultName : System.IO.Path.GetFileName(filePath);
                    string destPath = System.IO.Path.Combine(targetFolder.AbsolutePath, fileName);
                    System.IO.File.Copy(filePath, destPath, true);

                    global::Android.Media.MediaScannerConnection.ScanFile(
                        this,
                        new[] { destPath },
                        new[] { "image/*" },
                        null);

                    RunOnUiThread(() =>
                    {
                        global::Android.Widget.Toast.MakeText(this, $"Foto disimpan ke Downloads/LanChat/{fileName}", global::Android.Widget.ToastLength.Short)?.Show();
                    });

                    return destPath;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to save file: {ex.Message}");
                    return null;
                }
            };

            // Register custom media saver to store images/videos in Android/media/{PackageName}/Media Received/Photos and Videos
            LanChat.Core.Services.MediaStorageService.CustomMediaSaver = (originalFileName, data) =>
            {
                try
                {
                    string safeName = LanChat.Core.Services.MediaStorageService.SanitizeFileName(originalFileName);
                    string ext = System.IO.Path.GetExtension(safeName).ToLowerInvariant();
                    bool isVideo = ext is ".mp4" or ".mkv" or ".avi" or ".mov" or ".webm" or ".3gp" or ".flv";

                    string targetFolder = GetLanChatMediaDirectory(isVideo);
                    string destPath = System.IO.Path.Combine(targetFolder, $"{DateTime.Now:yyyyMMdd_HHmmss}_{safeName}");
                    System.IO.File.WriteAllBytes(destPath, data);

                    // Scan file into Android MediaStore so it immediately appears in Gallery
                    string mimeType = isVideo ? "video/*" : "image/*";
                    global::Android.Media.MediaScannerConnection.ScanFile(
                        this,
                        new[] { destPath },
                        new[] { mimeType },
                        null);

                    return destPath;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"CustomMediaSaver error: {ex.Message}");
                    return string.Empty;
                }
            };

            // Register Folder Opener for Media button on Android
            LanChat.Core.Services.PlatformLauncherService.CustomFolderOpener = () =>
            {
                try
                {
                    string mediaRoot = GetLanChatMediaRootDirectory();

                    // 1. Prioritize Samsung "My Files" if installed (e.g. on Samsung Galaxy S22)
                    var pkgMgr = PackageManager;
                    var samsungIntent = pkgMgr?.GetLaunchIntentForPackage("com.sec.android.app.myfiles");
                    if (samsungIntent != null)
                    {
                        try
                        {
                            var sIntent = new global::Android.Content.Intent("samsung.myfiles.intent.action.LAUNCH_MY_FILES");
                            sIntent.PutExtra("samsung.myfiles.intent.extra.START_PATH", mediaRoot);
                            sIntent.AddFlags(global::Android.Content.ActivityFlags.NewTask);
                            StartActivity(sIntent);
                            return;
                        }
                        catch { }
                    }

                    // 2. Try File Manager + if installed
                    var fileManagerPlusIntent = pkgMgr?.GetLaunchIntentForPackage("com.alphainventor.filemanager");
                    if (fileManagerPlusIntent != null)
                    {
                        try
                        {
                            fileManagerPlusIntent.AddFlags(global::Android.Content.ActivityFlags.NewTask);
                            StartActivity(fileManagerPlusIntent);
                            return;
                        }
                        catch { }
                    }

                    // 3. Try DocumentsUI / Storage Access Framework directory viewer
                    try
                    {
                        string relativeDocId = Uri.EscapeDataString($"primary:Android/media/{PackageName}/Media Received");
                        var docUri = global::Android.Net.Uri.Parse($"content://com.android.externalstorage.documents/document/{relativeDocId}");
                        var docIntent = new global::Android.Content.Intent(global::Android.Content.Intent.ActionView);
                        docIntent.SetDataAndType(docUri, "vnd.android.document/directory");
                        docIntent.AddFlags(global::Android.Content.ActivityFlags.NewTask | global::Android.Content.ActivityFlags.GrantReadUriPermission);
                        StartActivity(docIntent);
                        return;
                    }
                    catch { }

                    // 4. Try FileProvider directory viewer with resource/folder
                    try
                    {
                        var dirFile = new Java.IO.File(mediaRoot);
                        var contentUri = AndroidX.Core.Content.FileProvider.GetUriForFile(
                            this,
                            $"{PackageName}.fileprovider",
                            dirFile);

                        var folderIntent = new global::Android.Content.Intent(global::Android.Content.Intent.ActionView);
                        folderIntent.SetDataAndType(contentUri, "resource/folder");
                        folderIntent.AddFlags(global::Android.Content.ActivityFlags.NewTask | global::Android.Content.ActivityFlags.GrantReadUriPermission);
                        StartActivity(folderIntent);
                        return;
                    }
                    catch { }

                    // 5. Fallback: Open Gallery
                    var galleryIntent = new global::Android.Content.Intent(global::Android.Content.Intent.ActionView);
                    galleryIntent.SetDataAndType(global::Android.Provider.MediaStore.Images.Media.ExternalContentUri, "image/*");
                    galleryIntent.AddFlags(global::Android.Content.ActivityFlags.NewTask);
                    StartActivity(galleryIntent);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to open media folder: {ex.Message}");
                }
            };

            // Register predictive back / gesture navigation callback
            OnBackPressedDispatcher.AddCallback(this, new MainBackCallback(this));

            // Start foreground service to maintain socket listener and multicast discovery in background
            var svcIntent = new global::Android.Content.Intent(this, typeof(LanChatForegroundService));
            if (global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.O)
            {
                StartForegroundService(svcIntent);
            }
            else
            {
                StartService(svcIntent);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MainActivity OnCreate error: {ex.Message}");
        }
    }

    private class MainBackCallback : global::AndroidX.Activity.OnBackPressedCallback
    {
        private readonly MainActivity _activity;
        public MainBackCallback(MainActivity activity) : base(true)
        {
            _activity = activity;
        }

        public override void HandleOnBackPressed()
        {
            _activity.DoHandleBack();
        }
    }

    public void DoHandleBack()
    {
        if (LanChat.App.MainViewModelInstance?.HandleBackNavigation() == true)
        {
            return;
        }

        // Minimize instead of killing activity so background sockets & notifications stay alive
        MoveTaskToBack(true);
    }

    public override void OnBackPressed()
    {
        DoHandleBack();
    }

    public override bool OnKeyDown(Keycode keyCode, KeyEvent? e)
    {
        if (keyCode == Keycode.Back)
        {
            DoHandleBack();
            return true;
        }
        return base.OnKeyDown(keyCode, e);
    }

    protected override void OnResume()
    {
        base.OnResume();
        LanChat.Core.Services.AppLifecycleService.IsForeground = true;
    }

    protected override void OnPause()
    {
        base.OnPause();
        LanChat.Core.Services.AppLifecycleService.IsForeground = false;
    }

    protected override void OnStop()
    {
        base.OnStop();
        LanChat.Core.Services.AppLifecycleService.IsForeground = false;
    }

    public string GetLanChatMediaDirectory(bool isVideo)
    {
        string baseMediaDir = string.Empty;
        var externalMediaDirs = GetExternalMediaDirs();
        if (externalMediaDirs != null && externalMediaDirs.Length > 0 && externalMediaDirs[0] != null)
        {
            baseMediaDir = externalMediaDirs[0].AbsolutePath;
        }

        if (string.IsNullOrEmpty(baseMediaDir))
        {
            baseMediaDir = System.IO.Path.Combine(
                global::Android.OS.Environment.ExternalStorageDirectory?.AbsolutePath ?? "/storage/emulated/0",
                "Android",
                "media",
                PackageName);
        }

        string subFolder = isVideo ? "Videos" : "Photos";
        string targetPath = System.IO.Path.Combine(baseMediaDir, "Media Received", subFolder);
        if (!System.IO.Directory.Exists(targetPath))
        {
            System.IO.Directory.CreateDirectory(targetPath);
        }
        return targetPath;
    }

    public string GetLanChatMediaRootDirectory()
    {
        string baseMediaDir = string.Empty;
        var externalMediaDirs = GetExternalMediaDirs();
        if (externalMediaDirs != null && externalMediaDirs.Length > 0 && externalMediaDirs[0] != null)
        {
            baseMediaDir = externalMediaDirs[0].AbsolutePath;
        }

        if (string.IsNullOrEmpty(baseMediaDir))
        {
            baseMediaDir = System.IO.Path.Combine(
                global::Android.OS.Environment.ExternalStorageDirectory?.AbsolutePath ?? "/storage/emulated/0",
                "Android",
                "media",
                PackageName);
        }

        string rootPath = System.IO.Path.Combine(baseMediaDir, "Media Received");
        if (!System.IO.Directory.Exists(rootPath))
        {
            System.IO.Directory.CreateDirectory(rootPath);
        }
        return rootPath;
    }
}
