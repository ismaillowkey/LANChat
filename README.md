# LAN Chat v0.2.3 (Cross-Platform P2P Messenger)

Aplikasi chat lokal (LAN / Wi-Fi) tanpa ketergantungan pada server eksternal ataupun cloud. Berjalan secara murni **Peer-to-Peer (P2P)** antar perangkat dalam satu jaringan lokal dengan dukungan:
- **Desktop (Windows)**: WPF (.NET Framework 4.7.2)
- **Mobile (Android/iOS)**: .NET MAUI (.NET 10.0)
- **Shared Core**: `LanChat.Core` (.NET Standard 2.0 & .NET 10.0 Multi-target)

---

## ✨ Fitur Utama

1. **Auto Device Discovery (Zero-Configuration)**:
   - Protokol **UDP Broadcast / Multicast** (Port default `45450`).
   - Setiap device di jaringan Wi-Fi/LAN yang sama langsung terdeteksi otomatis tanpa perlu memasukkan alamat IP manual.
   - Heartbeat berkala & deteksi status Online / Offline secara real-time.

2. **Ruang Chat Terpisah (Broadcast & Direct 1-on-1)**:
   - **`📢 Semua Device (Broadcast)`**: Pesan otomatis terkirim ke seluruh perangkat di LAN.
   - **Direct Message (1-on-1)**: Klik device tertentu pada panel kiri untuk chat privat langsung via koneksi TCP.
   - Notifikasi **Lonceng (`🔔 [jumlah]`)** di panel kiri saat ada pesan direct masuk.

3. **Penyimpanan Riwayat Chat Lokal (Chat History Persistence)**:
   - Pesan tersimpan secara lokal di `%APPDATA%\LanChat\chat_history.json` sehingga riwayat chat tidak hilang saat aplikasi ditutup.
   - Daftar kontak lawan bicara direct tetap tersimpan di panel kiri (dengan indikator status hijau saat Online dan abu-abu saat Offline).

4. **Kirim Foto & Video Ala WhatsApp**:
   - Mendukung pengiriman foto (JPG, PNG, GIF, WEBP, BMP) dan video (MP4, MKV, AVI, dll) hingga 250 MB.
   - File media disimpan di folder lokal: `%APPDATA%\LanChat\Media\`.
   - **Prinsip WhatsApp**: Jika file foto dihapus dari folder media oleh pengguna, bubble chat tidak akan rusak/error, melainkan otomatis menampilkan status *"Pengirim mengirim gambar (File telah dihapus)"*.
   - Tersedia tombol cepat **`📁 Folder Media`** di header aplikasi untuk membuka direktori penyimpanan media.

5. **Pengaturan Identitas & Info Jaringan**:
   - Nama device tersimpan permanen di `%APPDATA%\LanChat\settings.json` dan dapat diubah sewaktu-waktu.
   - Menampilkan alamat IP lokal dan MAC Address perangkat.
   - Tampilan Light Theme modern dan bersih dengan icon resmi LAN & Chat.

---

## 🏗️ Struktur Solusi

```
LAN Chat/
├── LanChat.sln                           # Solution File Visual Studio
├── build_desktop_exe.bat                 # Script build Portable Desktop EXE
├── build_mobile_apk.bat                  # Script build Mobile Android APK
├── create_installer_windows.bat          # Script build Setup Installer Windows (NSIS)
├── installer.nsi                         # Konfigurasi NSIS Modern UI 2 Installer
├── src/
│   ├── LanChat.Core/                     # Shared Library (.NET Standard 2.0 & .NET 10.0)
│   │   ├── Models/
│   │   │   ├── ChatMessage.cs            # Model data pesan (teks, foto, video, status media)
│   │   │   ├── DevicePeer.cs             # Model data device di LAN (IP, MAC, Port, Online)
│   │   │   ├── MessageType.cs            # Enum Text, Image, Video
│   │   │   └── NetworkPacket.cs          # Protokol packet UDP beacon & TCP framing
│   │   └── Services/
│   │       ├── AppSettingsService.cs     # Penyimpanan setting nama device
│   │       ├── ChatHistoryService.cs     # Penyimpanan riwayat pesan lokal
│   │       ├── LanChatManager.cs         # Facade coordinator
│   │       ├── MediaStorageService.cs    # Manajemen penyimpanan file foto & video
│   │       ├── NetworkUtils.cs           # Deteksi IP, Subnet, dan MAC Address
│   │       ├── StoragePaths.cs           # Sentralisasi path folder AppData & Media
│   │       ├── TcpChatTransport.cs       # TCP Socket Streaming (Foto, Video & Chat)
│   │       └── UdpDiscoveryService.cs    # UDP Multicast / Broadcast Discovery
│   │
│   ├── LanChat.Wpf/                      # Windows Desktop App (WPF .NET Framework 4.7.2)
│   │   ├── Converters/                   # XAML Converters
│   │   ├── Resources/                    # App Logo (PNG & ICO)
│   │   ├── ViewModels/                   # MVVM MainWindowViewModel
│   │   └── MainWindow.xaml               # Antarmuka Desktop Modern Light Theme
│   │
│   └── LanChat.Maui/                     # Mobile App (.NET MAUI 10 Android/iOS)
│       ├── Converters/                   # MAUI Converters
│       ├── Platforms/Android/            # MulticastLock & Android Permissions
│       ├── ViewModels/                   # MVVM Mobile MainChatViewModel
│       └── MainPage.xaml                 # Antarmuka Mobile Responsif
```

---

## 📦 Build & Installer

Tersedia file batch otomatis di root direktori:

1. **Build Windows Setup Installer (NSIS)**:
   ```cmd
   create_installer_windows.bat
   ```
   *Menghasilkan file installer Windows lengkap dengan desktop shortcut, start menu, dan uninstaller di `publish\Installer\LAN_Chat_Setup_v0.2.3.exe`.*

2. **Build Desktop EXE Portable**:
   ```cmd
   build_desktop_exe.bat
   ```
   *Menghasilkan file eksekusi siap pakai di `publish\Desktop\LanChat.Wpf.exe`.*

3. **Build Android APK**:
   ```cmd
   build_mobile_apk.bat
   ```
   *Menghasilkan file package Android di `publish\Mobile\LanChat.apk`.*

---

## 🚀 Cara Menjalankan

### 1. Menjalankan Desktop App (WPF)
Jalankan melalui terminal:
```powershell
dotnet run --project src/LanChat.Wpf/LanChat.Wpf.csproj
```
> **Tips Simulasi 2 Device di 1 Komputer**:
> Buka 2 jendela terminal dan jalankan perintah di atas di masing-masing jendela. Port TCP akan otomatis beralih jika port 45451 sedang dipakai, sehingga kedua instance dapat langsung saling mengenali dan bertukar chat, foto, maupun video.

### 2. Menjalankan Mobile App (.NET MAUI di Android)
Pastikan perangkat Android terhubung via USB Debugging (atau emulator aktif) dalam satu jaringan Wi-Fi:
```powershell
dotnet build src/LanChat.Maui/LanChat.Maui.csproj -t:Run -f net10.0-android
```
atau buka file solusi `LanChat.sln` di Visual Studio dan jalankan ke target perangkat Android Anda.
