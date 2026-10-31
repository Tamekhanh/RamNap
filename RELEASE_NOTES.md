## RamNap v0.1.0 — first release 🌙

Lightweight Windows tool (~4 MB RAM) that shows everything using your RAM — including hidden services — and helps you reclaim it without slowing your PC.

*Công cụ Windows siêu nhẹ (~4 MB RAM): xem mọi thứ đang dùng RAM, kể cả dịch vụ ẩn, và lấy lại RAM mà không làm chậm máy.*

### ⬇️ Which file should I download? · Tải file nào?

| File | Size | Choose this if… |
|---|---|---|
| `RamNap-v0.1.0-win-x64.zip` | ~44 MB | **Most people.** Unzip and run, nothing else to install. · *Đa số người dùng: giải nén là chạy.* |
| `RamNap-v0.1.0-win-x64-lite.zip` | ~0.1 MB | You already have the [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0). · *Máy đã cài .NET 10 Desktop Runtime.* |

Both versions use the same amount of RAM while running (~4 MB in the tray). Windows 10/11, 64-bit.

### ✨ Features · Tính năng

- **Everything using RAM**: open apps, background tray apps, each hidden `svchost` service by its real name, Windows components, memory compression, and RAM owned by no app (kernel, drivers, GPU, shared memory).
- **Saving advisor**: concrete tips with how much RAM you can reclaim — browser memory saver (detects if it's already on), heavy background apps, apps starting with Windows. You always make the final click.
- **Nap mode**: forgotten apps get lower memory priority, so Windows takes RAM from them first instead of from the app you're using. Apps are never closed or frozen.
- **10 languages**: Tiếng Việt, English, 简体中文, 日本語, 한국어, Français, Español, Português, Bahasa Indonesia, Русский — switch with the 🌐 button.

### 🪶 RamNap's own footprint

| State | RAM (Task Manager "Memory") |
|---|---|
| Running in the tray | ~4 MB |
| Window open | ~10 MB |

### ⚠️ Good to know · Lưu ý

- **"Windows protected your PC"**: the app is not code-signed yet. Click **More info → Run anyway**. The full source code is in this repository. · *App chưa ký số: bấm More info → Run anyway.*
- Some antivirus tools may be cautious because RamNap adjusts the memory priority of other processes. Check the SHA-256 in `SHA256SUMS.txt` if you want to verify the download.
- Software can't create RAM: if your RAM is often full during normal work, closing apps or adding RAM is still the real fix.

**Full guide in 10 languages:** see the [README](https://github.com/Tamekhanh/RamNap#readme).
