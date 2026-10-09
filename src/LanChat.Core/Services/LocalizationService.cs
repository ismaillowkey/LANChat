using System;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace LanChat.Core.Services;

public partial class LocalizationService : ObservableObject
{
    private static LocalizationService? _instance;
    public static LocalizationService Instance => _instance ??= new LocalizationService();

    [ObservableProperty]
    private string _currentLanguage = "id"; // "id" or "en"

    public bool IsEnglish => CurrentLanguage == "en";
    public bool IsIndonesian => CurrentLanguage == "id";

    public LocalizationService()
    {
        _currentLanguage = AppSettingsService.LoadLanguage();
    }

    public void SetLanguage(string lang)
    {
        lang = lang.ToLowerInvariant();
        if (lang != "id" && lang != "en") lang = "id";

        if (CurrentLanguage != lang)
        {
            CurrentLanguage = lang;
            AppSettingsService.SaveLanguage(lang);
            NotifyAllStringsChanged();
        }
    }

    [RelayCommand]
    public void ToggleLanguage()
    {
        SetLanguage(CurrentLanguage == "id" ? "en" : "id");
    }

    public void NotifyAllStringsChanged()
    {
        OnPropertyChanged(string.Empty);
        OnPropertyChanged(nameof(IsEnglish));
        OnPropertyChanged(nameof(IsIndonesian));
        OnPropertyChanged(nameof(LanguageToggleButtonText));
    }

    public string LanguageToggleButtonText => IsEnglish ? "🇬🇧 English" : "🇮🇩 Indonesia";
    public string LanguageToggleShortText => IsEnglish ? "🇬🇧 EN" : "🇮🇩 ID";

    // Application Branding & Headers
    public string AppVersion => AppVersionService.Version;
    public string AppTitle => IsEnglish 
        ? $"LAN Chat v{AppVersion} - Cross-Platform Local Messenger" 
        : $"LAN Chat v{AppVersion} - Messenger Lokal Lintas Platform";

    public string SubTitle => "Direct & Broadcast P2P (Wi-Fi / LAN)";
    public string MyDevice => IsEnglish ? "My Device:" : "Device Saya:";
    public string MyDeviceLabel => IsEnglish ? "My Device:" : "Device Saya:";
    public string Change => IsEnglish ? "Change" : "Ubah";
    public string SaveDeviceTooltip => IsEnglish ? "Save device name to settings" : "Simpan nama device ke settings";
    public string MediaFolder => IsEnglish ? "📁 Media Folder" : "📁 Folder Media";
    public string MediaFolderTooltip => IsEnglish ? "Open local media folder (photos/videos) in File Explorer" : "Buka folder media lokal (foto/video) di File Explorer";
    public string LanguageToggleTooltip => IsEnglish ? "Switch language to Indonesian" : "Ganti bahasa ke Bahasa Indonesia";

    // Sidebar & Peer List
    public string DevicesOnLan => IsEnglish ? "DEVICES ON LAN" : "DEVICE DI LAN";
    public string DevicesOnLanSub => IsEnglish ? "Select chat target (Broadcast or Direct 1-on-1)" : "Pilih tujuan chat (Broadcast atau Direct 1-on-1)";
    public string AllDevicesBroadcast => IsEnglish ? "All Devices (Broadcast)" : "Semua Device (Broadcast)";
    public string AllDevicesDisplayName => IsEnglish ? "📢 All Devices (Broadcast)" : "📢 Semua Device (Broadcast)";
    public string AllDevices => IsEnglish ? "All Devices" : "Semua Device";
    public string BroadcastSubtitle => IsEnglish ? "Send to all devices on LAN" : "Kirim ke semua device di LAN";

    // Chat Header & Badges
    public string ChatRoom => IsEnglish ? "Chat Room: " : "Ruang Chat: ";
    public string ModeBroadcast => IsEnglish ? "BROADCAST MODE" : "MODE BROADCAST";
    public string ChatDirect => IsEnglish ? "DIRECT CHAT" : "CHAT DIRECT";
    public string ModeBroadcastBadge => IsEnglish ? "📢 Public (All Devices)" : "📢 Publik (Semua Device)";
    public string ModePrivateBadge => IsEnglish ? "🔒 Private Chat (1-on-1)" : "🔒 Chat Privat (1-on-1)";
    public string BroadcastBadge => "Broadcast";
    public string DirectBadge => "Direct";
    public string You => IsEnglish ? "You" : "Anda";

    // Message Media Display
    public string ImageDeletedNotice => IsEnglish 
        ? "Image file was deleted or not found in storage folder" 
        : "File gambar telah dihapus atau tidak ditemukan di folder penyimpanan";

    public string PlayVideo => IsEnglish ? "▶ Play" : "▶ Putar";
    public string PlayVideoTooltip => IsEnglish ? "Play video" : "Putar video";
    public string OpenVideoFolderTooltip => IsEnglish ? "Open video folder" : "Buka folder video";
    public string FileDeleted => IsEnglish ? "File deleted" : "File telah dihapus";

    // Input Bar
    public string TypeMessagePlaceholder => IsEnglish ? "Message" : "Ketik pesan...";
    public string Cancel => IsEnglish ? "Cancel" : "Batal";
    public string AttachMediaTitle => IsEnglish ? "Attach Media" : "Lampirkan Media";
    public string Send => IsEnglish ? "Send" : "Kirim";
    public string SendTooltip => IsEnglish ? "Send Message (Enter)" : "Kirim Pesan (Enter)";
    public string AttachImageTooltip => IsEnglish ? "Send Photo / Image" : "Kirim Foto / Gambar";
    public string AttachVideoTooltip => IsEnglish ? "Send Video (Max 250 MB)" : "Kirim Video (Maks 250 MB)";
    public string Photo => IsEnglish ? "Photo" : "Foto";
    public string Gallery => IsEnglish ? "Gallery" : "Galeri";
    public string Video => "Video";

    // Statuses
    public string Online => "Online";
    public string Offline => "Offline";
    public string DeleteOfflineDeviceTooltip => IsEnglish ? "Delete offline device" : "Hapus device offline";

    // Dynamic Formatted Messages
    public string FormatUserSentImage(string senderName) =>
        IsEnglish ? $"{senderName} sent an image" : $"{senderName} mengirim gambar";

    public string FormatUserSentVideo(string senderName) =>
        IsEnglish ? $"{senderName} sent a video" : $"{senderName} mengirim video";

    public string FormatPeerOffline(string peerName) =>
        IsEnglish
            ? $"Device '{peerName}' is offline. Direct message cannot be sent while peer is offline."
            : $"Device '{peerName}' sedang offline. Pesan direct tidak dapat dikirim saat lawan bicara offline.";

    public string FormatSendFailed(string peerName) =>
        IsEnglish
            ? $"Failed to send message to {peerName}. Ensure device is connected to the same LAN."
            : $"Gagal mengirim pesan ke {peerName}. Pastikan device masih terhubung dalam satu jaringan LAN.";

    public string PickImageTitle => IsEnglish ? "Select Image to Send" : "Pilih Gambar untuk Dikirim";
    public string PickVideoTitle => IsEnglish ? "Select Video to Send" : "Pilih Video untuk Dikirim";
    public string SendPhoto => IsEnglish ? "Send Photo" : "Kirim Foto";
    public string SendVideo => IsEnglish ? "Send Video" : "Kirim Video";
    public string AttachMedia => IsEnglish ? "Attach Media" : "Lampirkan Media";
    public string VideoTooLarge => IsEnglish ? "Video size exceeds 250 MB limit." : "Ukuran video melebihi batas 250 MB.";
    public string VideoTooLargeTitle => IsEnglish ? "File Too Large" : "File Terlalu Besar";
    public string MediaNotFound => IsEnglish ? "Media file not found in local storage (it may have been deleted)." : "File media tidak ditemukan di penyimpanan lokal (mungkin telah dipindahkan atau dihapus).";
    public string MediaNotFoundTitle => IsEnglish ? "File Not Found" : "File Tidak Ditemukan";
    public string WarningTitle => IsEnglish ? "Warning" : "Perhatian";
    public string ErrorTitle => "Error";
    public string ConfirmDeleteTitle => IsEnglish ? "Confirm Delete" : "Konfirmasi Hapus";
    public string FormatConfirmDeletePeer(string peerName) =>
        IsEnglish
            ? $"Are you sure you want to remove offline device '{peerName}' from the list?"
            : $"Apakah Anda yakin ingin menghapus device offline '{peerName}' dari daftar?";

    public string DeviceNameUpdatedTitle => IsEnglish ? "Device Name Updated" : "Nama Device Diperbarui";
    public string FormatDeviceNameUpdated(string newName) =>
        IsEnglish
            ? $"Device name successfully changed to '{newName}'."
            : $"Nama device berhasil diubah menjadi '{newName}'.";
    public string DeviceNameCannotBeEmpty => IsEnglish ? "Device name cannot be empty." : "Nama device tidak boleh kosong.";

    public string ConfirmChangeDeviceNameTitle => IsEnglish ? "Confirm Change Device Name" : "Konfirmasi Ubah Nama Perangkat";
    public string ConfirmChangeDeviceNameMessage => IsEnglish
        ? "Are you sure you want to change your device name? Changing device name carries a risk of losing chat history and previous sessions."
        : "Apakah Anda yakin ingin mengubah nama perangkat? Mengubah nama perangkat berisiko kehilangan riwayat chat dan sesi obrolan sebelumnya.";
    public string YesChange => IsEnglish ? "Yes, Change" : "Ya, Ganti";

    // Menu Bar & About Strings
    public string MenuFile => IsEnglish ? "File" : "Berkas";
    public string MenuExit => IsEnglish ? "Exit" : "Keluar";
    public string MenuSettings => IsEnglish ? "Settings" : "Pengaturan";
    public string AutoStartWithWindows => IsEnglish ? "Auto Start on System Startup" : "Mulai Otomatis Saat Startup Komputer";
    public string AllowFirewall => IsEnglish ? "Allow Windows Firewall Access" : "Buka Akses Windows Firewall";
    public string AllowFirewallSuccess => IsEnglish ? "Windows Firewall rules for LAN Chat have been successfully registered!" : "Aturan Windows Firewall untuk LAN Chat berhasil didaftarkan!";
    public string AllowFirewallError => IsEnglish ? "Administrator permission was not granted or cancelled." : "Izin Administrator tidak diberikan atau dibatalkan.";
    public string MenuAbout => IsEnglish ? "About" : "Tentang";
    public string AboutTitle => IsEnglish ? "About LAN Chat" : "Tentang LAN Chat";
    public string AboutDescription => IsEnglish
        ? "Cross-platform local network (LAN/Wi-Fi) peer-to-peer instant messenger. Transfer text messages, photos, and videos directly without internet or external servers."
        : "Aplikasi pesan instan jaringan lokal (LAN/Wi-Fi) lintas platform berbasis peer-to-peer tanpa internet. Kirim pesan teks, foto, dan video langsung tanpa server eksternal.";
    public string AboutTechInfo => "Built with Avalonia UI (.NET 10) • Pure P2P Architecture";
    public string TrayOpen => IsEnglish ? "Open LAN Chat" : "Buka LAN Chat";
    public string TrayExit => IsEnglish ? "Exit" : "Keluar";
}
