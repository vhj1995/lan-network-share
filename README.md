# LAN Network Share C#

A modern, high-performance C# / .NET 8 WPF desktop application for fast, secure file and folder sharing over a Local Area Network (LAN). 

This project is a complete C# rewrite and modernization of the open-source C++ Qt application [LAN-Share](https://github.com/abdularis/LAN-Share), engineered specifically for **Windows**.

---

## ⚡ Features

- **Automatic Device Discovery**: Effortlessly finds active peers on the same local network using UDP broadcasting.
- **Fast TCP File & Folder Transfers**: Stream files and full directory structures directly between Windows machines at maximum LAN speeds.
- **Modern Windows UI**: Built with WPF and XAML, offering a clean layout with live progress indicators, peer status, and real-time transfer tracking.
- **Asynchronous Architecture**: Fully asynchronous network I/O (`async`/`await`) ensuring zero UI freezes during large file transfers.
- **Zero Configuration**: No complex setup, external servers, or internet connection required.

---

## 🛠️ Tech Stack & Architecture

- **Framework**: .NET 8 (WPF)
- **Language**: C# 12
- **Networking**: `System.Net.Sockets` (`UdpClient`, `TcpListener`, `TcpClient`)
- **MVVM Pattern**: `CommunityToolkit.Mvvm`
- **Target OS**: Windows 10 / Windows 11 (x64)

---

## 🚀 Getting Started

### Prerequisites

- **Windows 10 / 11**
- **.NET 8.0 SDK** (or Visual Studio 2022 with the *.NET Desktop Development* workload)

### Running from Visual Studio

1. Clone this repository:
   ```bash
   git clone https://github.com/your-username/LAN-Share-CSharp.git
   ```
2. Open `LANShare.CSharp.sln` in **Visual Studio 2022**.
3. Set `LANShare.CSharp` as the Startup Project.
4. Press **`F5`** to build and run.

### Building via .NET CLI

```bash
cd LANShare.CSharp
dotnet build -c Release
dotnet run
```

---

## 📖 How It Works

1. **Discovery (UDP)**: The app broadcasts discovery packets on the local subnet. Other instances running on the network respond automatically, populating the active peers list.
2. **Transfer Request (TCP)**: Selecting files or folders sends a transfer request to the selected peer.
3. **Data Streaming**: File metadata and binary chunks are streamed directly over a high-throughput TCP socket connection.

---

## 🧱 Project Structure

```
LANShare.CSharp/
├── Models/           # Data transfer contracts, packets, and file metadata
├── Network/          # UDP discovery, TCP file sender, and receiver logic
├── ViewModels/       # MVVM view models and observable state logic
├── Views/            # WPF XAML windows and UI components
├── App.xaml          # Application entry point & resources
└── MainWindow.xaml   # Main desktop user interface
```

---

## 📜 License

Distributed under the [MIT License](LICENSE).

---

## 🙏 Acknowledgments

- Original C++ Qt implementation: [abdularis/LAN-Share](https://github.com/abdularis/LAN-Share)
