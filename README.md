# LAN Chat (Cross-Platform Local P2P Messenger)

Aplikasi chat lokal (LAN / Wi-Fi) tanpa ketergantungan pada server eksternal ataupun cloud. Berjalan secara murni **Peer-to-Peer (P2P)** antar perangkat dalam satu jaringan lokal dengan dukungan multi-platform:
- **Desktop (Windows 32/64-bit & Linux)**: .NET 10.0
- **Mobile (Android APK)**: .NET 10.0
- **Shared Core**: `LanChat.Core`

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
   - Status centang pesan ala WhatsApp: ✓ (Terkirim), ✓✓ abu-abu (Diterima), ✓✓ biru (Dibaca).

3. **Penyimpanan Riwayat Chat Lokal (Chat History Persistence)**:
   - Pesan tersimpan secara lokal di `%APPDATA%\LanChat\chat_history.json` sehingga riwayat chat tidak hilang saat aplikasi ditutup.
   - Daftar kontak lawan bicara direct tetap tersimpan di panel kiri dengan highlight status aktif.

4. **Kirim Foto & Video Ala WhatsApp**:
   - Mendukung pengiriman foto (JPG, PNG, GIF, WEBP, BMP) dan video (MP4, MKV, AVI, dll) hingga 250 MB.
   - File media disimpan di folder lokal: `%APPDATA%\LanChat\Media\`.
   - Tersedia tombol cepat **`📁 Folder Media`** di header aplikasi untuk membuka direktori penyimpanan media.

5. **Pengaturan Identitas & Info Jaringan**:
   - Device Unique GUID tersimpan permanen di registry/pengaturan.
   - Nama device tersimpan di `%APPDATA%\LanChat\config.ini` dan dapat diubah sewaktu-waktu.
   - Auto-scroll ke pesan terbawah saat membuka chat atau menerima pesan baru.

---

## 🏗️ Struktur Solusi

```
LAN Chat/
├── LanChat.slnx                         # Solution File
├── build_installer_desktop_win32.bat    # Script build NSIS Setup Installer Windows Win32
├── build_portable_desktop_win32.bat     # Script build Portable ZIP Windows Win32
├── build_installer_desktop_linux32.bat  # Script build Installer Linux 32-bit (.tar.gz)
├── build_installer_mobile_android.bat   # Script build Android APK
├── installer.nsi                        # Konfigurasi NSIS Modern UI 2 Installer
├── src/
│   ├── LanChat.Core/                    # Network, P2P Socket, Storage, Identity
│   ├── LanChat/                         # Cross-platform UI (Views & ViewModels)
│   ├── LanChat.Desktop/                 # Desktop Entry Point (Win32 & Linux)
│   ├── LanChat.Android/                 # Mobile Android Entry Point
│   ├── LanChat.Browser/                 # WebAssembly Entry Point
│   └── LanChat.iOS/                     # iOS Entry Point
```

---

## 📦 Build & Installer

Tersedia file batch otomatis di root direktori:

1. **Build Windows Setup Installer (NSIS)**:
   ```cmd
   build_installer_desktop_win32.bat
   ```
   *Menghasilkan file installer Windows lengkap dengan desktop shortcut, start menu, firewall rules, dan uninstaller di `publish\desktop_win32\LAN_Chat_Setup_v[VERSION]_win32.exe`.*

2. **Build Desktop Portable ZIP**:
   ```cmd
   build_portable_desktop_win32.bat
   ```
   *Menghasilkan file arsip ZIP portable di `publish\desktop_portable_win32\LAN_Chat_Portable_v[VERSION]_win32.zip`.*

3. **Build Android APK**:
   ```cmd
   build_installer_mobile_android.bat
   ```
   *Menghasilkan file package Android di `publish\mobile_android\LAN_Chat_v[VERSION].apk`.*

4. **Build Linux 32-bit**:
   ```cmd
   build_installer_desktop_linux32.bat
   ```
   *Menghasilkan paket `publish\desktop_linux32\LAN_Chat_Linux32_Setup_v[VERSION].tar.gz`.*
