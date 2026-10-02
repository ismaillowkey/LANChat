# LAN Chat (Cross-Platform P2P Messenger)

Aplikasi chat lokal (LAN / Wi-Fi) tanpa ketergantungan pada server eksternal/cloud. Berjalan secara peer-to-peer (P2P) dengan dukungan:
- **Desktop**: WPF (.NET 9 Windows)
- **Mobile**: .NET MAUI (Android, iOS)
- **Shared Core**: `LanChat.Core` (.NET 9 Class Library)

---

## ✨ Fitur Utama

1. **Auto Device Discovery (Zero-Config)**:
   - Menggunakan protokol **UDP Broadcast / Multicast** (Port default `45450`).
   - Setiap device otomatis mengenali device lain yang terhubung ke jaringan Wi-Fi/LAN yang sama.
   - Heartbeat berkala & deteksi otomatis jika device offline/terputus.

2. **Mode Pengiriman Fleksibel**:
   - **Default: Broadcast ke Semua Device (`📢 Semua Device`)**: Pesan otomatis didistribusikan ke seluruh peer yang terhubung.
   - **Direct Message (1-on-1)**: Cukup klik salah satu device di daftar peer untuk mengirim pesan privat langsung via TCP ke IP target.

3. **Nama & Identitas Device**:
   - Setiap device memiliki nama yang dapat diubah kapan saja langsung dari antarmuka aplikasi.
   - Perubahan nama akan otomatis di-broadcast ke seluruh jaringan.

4. **Kirim Teks & Gambar**:
   - Mendukung pengiriman teks real-time dengan status outgoing/incoming.
   - Mendukung pengiriman file gambar (JPG, PNG, GIF, WEBP, BMP) via TCP streaming dengan preview instan.

5. **Antarmuka Modern (Rich Dark UI)**:
   - Tampilan bernuansa dark slate modern, badge direct/broadcast berwarna, indikator IP & port aktif.

---

## 🏗️ Struktur Solusi

```
LAN Chat/
├── LanChat.slnx
├── src/
│   ├── LanChat.Core/                     # Shared Logic & Networking
│   │   ├── Models/
│   │   │   ├── ChatMessage.cs            # Model data pesan (teks & gambar)
│   │   │   ├── DevicePeer.cs             # Model data device di LAN
│   │   │   ├── MessageType.cs            # Enum Text / Image
│   │   │   └── NetworkPacket.cs          # Protokol packet UDP beacon & TCP framing
│   │   └── Services/
│   │       ├── LanChatManager.cs         # Facade coordinator
│   │       ├── NetworkUtils.cs           # Utility IP & Broadcast resolution
│   │       ├── TcpChatTransport.cs       # TCP Socket Framing & Delivery
│   │       └── UdpDiscoveryService.cs    # UDP Multicast / Broadcast Discovery
│   │
│   ├── LanChat.Wpf/                      # Windows Desktop App (WPF .NET 9)
│   │   ├── Converters/                   # XAML Image & Layout converters
│   │   ├── ViewModels/                   # MVVM MainWindowViewModel
│   │   └── MainWindow.xaml               # Antarmuka Desktop Modern
│   │
│   └── LanChat.Maui/                     # Mobile App (.NET MAUI 9 Android/iOS)
│       ├── Converters/                   # MAUI ImageSource converters
│       ├── Platforms/Android/            # MulticastLock & Wi-Fi Permissions
│       ├── ViewModels/                   # MVVM Mobile MainChatViewModel
│       └── MainPage.xaml                 # Antarmuka Mobile Responsif
```

---

## 🚀 Cara Menjalankan

### 1. Menjalankan Desktop App (WPF)
Jalankan perintah berikut di terminal:
```powershell
dotnet run --project src/LanChat.Wpf/LanChat.Wpf.csproj
```
> **Tips Simulasi 2 Device di 1 PC**:
> Anda dapat membuka 2 terminal dan menjalankan perintah di atas dua kali. Karena `LanChat.Core` mendeteksi port yang sibuk secara otomatis, instance kedua akan otomatis menggunakan port TCP berikutnya (misal: 45452), dan keduanya akan saling mengenali dan dapat saling kirim pesan & gambar secara langsung!

### 2. Menjalankan Mobile App (.NET MAUI di Android)
Pastikan HP Android terhubung ke PC via USB Debugging (atau jalankan Android Emulator) dan berada di **jaringan Wi-Fi yang sama**:
```powershell
dotnet build src/LanChat.Maui/LanChat.Maui.csproj -t:Run -f net9.0-android
```
atau buka di **Visual Studio 2022 / 2026** dan pilih target Android Device.
