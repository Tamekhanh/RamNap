# RamNap 🌙

**⬇️ [Download / Tải về](https://github.com/Tamekhanh/RamNap/releases/latest)** · Windows 10/11 64-bit · ~4 MB RAM

**Tiếng Việt** · **English** · **简体中文** · **日本語** · **한국어** · **Français** · **Español** · **Português** · **Bahasa Indonesia** · **Русский**

> Bấm vào ngôn ngữ của bạn để mở · Click your language to expand · 点击你的语言展开 · 言語をクリックして開く · 언어를 클릭해 펼치기 · Cliquez sur votre langue · Haz clic en tu idioma · Clique no seu idioma · Klik bahasa Anda · Нажмите на свой язык

<details>
<summary><b>🇻🇳 Tiếng Việt</b></summary>

### RamNap là gì?

Ứng dụng nhỏ cho Windows, giúp bạn **thấy rõ RAM đang đi đâu** và **lấy lại RAM mà không làm hỏng trải nghiệm**:

- **Tất cả những gì đang dùng RAM**, kể cả thứ chạy ẩn: app đang mở, app chạy ngầm ở khay, từng dịch vụ ẩn (mỗi `svchost` được tách riêng và gọi đúng tên dịch vụ), thành phần Windows, kho nén bộ nhớ, và phần RAM không thuộc app nào (nhân Windows, driver, GPU, bộ nhớ dùng chung).
- **Cố vấn tiết kiệm**: gợi ý cụ thể kèm số RAM lấy lại được, ví dụ bật Memory Saver của trình duyệt, thoát app chạy ngầm nặng, tắt app tự chạy cùng Windows. Bạn luôn là người bấm quyết định. Runtime, cơ sở dữ liệu và máy ảo (`node`, `python`, `postgres`...) không có nút buộc thoát, còn app đã loại trừ thì không bị gợi ý đóng, thoát hay bỏ tự khởi động.
- **Cho app bỏ quên "ngủ"**: khi máy thiếu RAM, Windows lấy RAM từ app bạn bỏ quên trước, thay vì từ app bạn đang dùng.

### RamNap tốn bao nhiêu RAM?

Đo bằng cột "Memory" của Task Manager (private working set), trên máy 16 GB:

| Trạng thái | RAM |
|---|---|
| Chạy nền ở khay | **~4 MB** |
| Đang mở cửa sổ | ~10 MB |
| Sau khi đóng cửa sổ | ~7 MB |

Cửa sổ chỉ được tạo khi bạn mở và bị hủy khi đóng. RamNap quét hệ thống bằng một lần gọi `NtQuerySystemInformation`, không mở handle tới từng app, và chỉ nạp ngôn ngữ đang dùng vào RAM.

### Chế độ "ngủ" hoạt động thế nào

1. Cứ 5 giây, RamNap xem bạn đang dùng cửa sổ nào và từng app tốn bao nhiêu CPU.
2. App **có cửa sổ** mà **bạn không mở tới trong 10 phút** và **gần như không dùng CPU** thì được cho ngủ: RamNap hạ *memory priority* của nó xuống Low.
3. Mặc định RamNap chỉ hạ priority. Việc đẩy working set của app ngủ ra đang **tắt** vì chưa có benchmark chứng minh lợi ích. Nếu bạn đặt `TrimEnabled` thành `true`, RamNap làm việc này khi RAM đã dùng **≥ 80%**, mỗi lượt quét **một** app.
4. Bạn chuyển sang app nào là priority của app đó trở về bình thường **ngay lập tức**. Dữ liệu đã rời RAM vẫn cần thời gian nạp lại.
5. RamNap **cố tránh** app đang làm việc bằng cách xem CPU (từ 2% của một nhân trở lên), nhưng không kiểm tra âm thanh hay mạng, nên app tải file chậm hoặc tab phát nhạc vẫn có thể bị cho ngủ. **Luôn được loại trừ**: các trình phát nhạc phổ biến (Spotify, VLC...); tiến trình hệ thống, `svchost`, WebView2; bộ gõ tiếng Việt (UniKey, EVKey, OpenKey), AutoHotkey, PowerToys, trình quản lý mật khẩu.
6. Nếu RamNap bị tắt đột ngột, lần chạy sau nó tự trả các app về bình thường.

**Đánh đổi:** khi RAM đã căng, mở lại một app ngủ lâu có thể khựng một chút trong lúc nạp lại dữ liệu. Khi máy dư RAM, RamNap gần như không làm gì.

### Build và chạy

Cần .NET 10 SDK.

```bash
dotnet build -c Release
```

Chạy `bin\Release\net10.0-windows\RamNap.exe`. Đóng cửa sổ thì RamNap vẫn nằm ở khay hệ thống (biểu tượng trăng khuyết); bấm đúp để mở lại, chuột phải để mở menu. Xem tình trạng RAM mà không thay đổi gì:

```bash
bin\Release\net10.0-windows\RamNap.exe --scan
```

### Cài đặt

File `%APPDATA%\RamNap\settings.json` (menu → *Mở file cài đặt*, sửa xong chọn *Tải lại cài đặt*):

| Khóa | Mặc định | Ý nghĩa |
|---|---|---|
| `Language` | auto | Ngôn ngữ: `auto` (theo Windows), `vi`, `en`, `zh`, `ja`, `ko`, `fr`, `es`, `pt`, `id`, `ru` |
| `IdleMinutes` | 10 | Số phút không dùng trước khi app được cho ngủ |
| `CpuActivePercent` | 2.0 | Từ mức này (% của 1 nhân CPU) trở lên thì coi là app đang làm việc |
| `TrimEnabled` | false | Đẩy working set của app ngủ ra khi RAM căng. Mặc định tắt vì chưa được kiểm chứng |
| `PressurePercent` | 80 | Khi bật `TrimEnabled`: mức RAM sử dụng (%) bắt đầu giải phóng RAM |
| `TrimCooldownSeconds` | 300 | Khoảng nghỉ giữa hai lần giải phóng RAM của cùng một app |
| `IntervalSeconds` | 5 | Chu kỳ quét |
| `DryRun` | false | `true` = chỉ ghi nhật ký, không thay đổi gì |
| `RequireWindow` | true | Chỉ cho ngủ app có cửa sổ |
| `TrayAppsAllowed` | Discord, Slack, Telegram, Zalo, Steam... | App chạy ở khay vẫn được cho ngủ |
| `Exclusions` | (trống) | Tên tiến trình bạn muốn loại trừ thêm |

Nếu `settings.json` có lỗi (kể cả gõ sai tên khóa), RamNap không áp dụng và không ghi đè file. *Tải lại cài đặt* sẽ giữ nguyên cài đặt đang dùng; nếu khởi động với file lỗi, RamNap chạy ở chế độ thử cho tới khi bạn sửa file. Dấu phẩy thừa và ghi chú được chấp nhận.

Mọi hành động đều được ghi vào `%APPDATA%\RamNap\ramnap.log`.

### Ngôn ngữ

Đổi ngôn ngữ: chuột phải biểu tượng ở khay → **Ngôn ngữ**. Muốn thêm ngôn ngữ mới: chép `lang\en.json` thành file mới, dịch các giá trị (giữ nguyên `{0}`, `{1}`), rồi thêm mã ngôn ngữ vào `Lang.Available` trong [src/Lang.cs](src/Lang.cs).

### Giới hạn

- Không đụng được app chạy quyền Administrator.
- Gom nhóm theo tên tiến trình nên còn thô: cửa sổ ở màn hình thứ hai mà bạn chỉ nhìn chứ không bấm vào vẫn có thể bị coi là "không dùng". Khi đó hãy thêm app vào `Exclusions`.
- Chưa có benchmark đo hiệu quả trên từng máy. Bật `DryRun` để xem RamNap *sẽ* làm gì trước.
- App liên tục dùng một ít CPU, như trình duyệt và app chat, có thể hiếm khi được cho ngủ: chỉ một lượt quét đạt `CpuActivePercent` là thời gian chờ bị tính lại từ đầu.
- Phần mềm không tạo thêm RAM được. Nếu RAM thường xuyên đầy khi làm việc bình thường, giải pháp là đóng bớt app hoặc nâng cấp RAM.

</details>

<details>
<summary><b>🇬🇧 English</b></summary>

### What is RamNap?

A small Windows app that shows you **where your RAM is going** and helps you **get RAM back without hurting your experience**:

- **Everything using RAM**, including hidden things: open apps, background tray apps, each hidden service (every `svchost` is split out and named after its real service), Windows components, the memory compression store, and RAM that belongs to no app (Windows kernel, drivers, GPU, shared memory).
- **Saving advisor**: concrete suggestions with how much RAM you can reclaim, such as turning on your browser's Memory Saver, quitting heavy background apps, or stopping apps from starting with Windows. You always make the final click. There is no force-quit button for runtimes, databases and virtual machines (`node`, `python`, `postgres`...), and excluded apps get no close, quit or startup suggestions.
- **Lets forgotten apps "nap"**: when RAM runs low, Windows takes RAM from apps you forgot about first, instead of from the app you are using.

### How much RAM does RamNap use?

Measured with Task Manager's "Memory" column (private working set) on a 16 GB machine:

| State | RAM |
|---|---|
| Running in the tray | **~4 MB** |
| Window open | ~10 MB |
| After closing the window | ~7 MB |

The window is only created when you open it and is destroyed when you close it. RamNap scans the system with a single `NtQuerySystemInformation` call without opening a handle to every app, and only loads the language in use into RAM.

### How nap mode works

1. Every 5 seconds, RamNap checks which window you are using and how much CPU each app uses.
2. An app **with a window** that **you haven't opened for 10 minutes** and that **uses almost no CPU** is put to nap: RamNap lowers its *memory priority* to Low.
3. By default RamNap only lowers priority. Pushing napping apps out of their working set is **off**, because no benchmark has shown it helps yet. If you set `TrimEnabled` to `true`, RamNap does this when RAM use is **≥ 80%**, **one** app per scan.
4. As soon as you switch to an app, its priority returns to normal **immediately**. Data that already left RAM still takes time to load back.
5. RamNap **tries to avoid** apps that are working by checking CPU use (2% of one core or more). It does not check audio or network, so a slow download or a tab playing music can still nap. **Always excluded**: common music players (Spotify, VLC...); system processes, `svchost`, WebView2; Vietnamese input methods (UniKey, EVKey, OpenKey), AutoHotkey, PowerToys, password managers.
6. If RamNap is closed unexpectedly, it restores every app to normal the next time it starts.

**Trade-off:** when RAM is already tight, reopening an app that napped for a long time may pause briefly while its data is reloaded. When you have plenty of RAM, RamNap does almost nothing.

### Build and run

Requires the .NET 10 SDK.

```bash
dotnet build -c Release
```

Run `bin\Release\net10.0-windows\RamNap.exe`. After closing the window, RamNap stays in the system tray (crescent moon icon); double-click to reopen, right-click for the menu. To see the RAM status without changing anything:

```bash
bin\Release\net10.0-windows\RamNap.exe --scan
```

### Settings

File `%APPDATA%\RamNap\settings.json` (menu → *Open settings file*, then *Reload settings* after editing):

| Key | Default | Meaning |
|---|---|---|
| `Language` | auto | Language: `auto` (Windows language), `vi`, `en`, `zh`, `ja`, `ko`, `fr`, `es`, `pt`, `id`, `ru` |
| `IdleMinutes` | 10 | Minutes without use before an app naps |
| `CpuActivePercent` | 2.0 | At or above this (% of one CPU core) the app counts as working |
| `TrimEnabled` | false | Push napping apps out of their working set when RAM is tight. Off by default because it is unproven |
| `PressurePercent` | 80 | With `TrimEnabled` on: RAM use (%) at which RAM starts being freed |
| `TrimCooldownSeconds` | 300 | Minimum time between two frees of the same app |
| `IntervalSeconds` | 5 | Scan interval |
| `DryRun` | false | `true` = only write the log, change nothing |
| `RequireWindow` | true | Only nap apps that have a window |
| `TrayAppsAllowed` | Discord, Slack, Telegram, Zalo, Steam... | Tray apps that may still nap |
| `Exclusions` | (empty) | Extra process names to exclude |

If `settings.json` has an error (including a misspelled key), RamNap does not apply it and does not overwrite it. *Reload settings* keeps the current settings; if RamNap starts with a broken file, it runs in dry run mode until you fix the file. Trailing commas and comments are allowed.

Every action is written to `%APPDATA%\RamNap\ramnap.log`.

### Languages

Change language: right-click the tray icon → **Language**. To add a new language: copy `lang\en.json` to a new file, translate the values (keep `{0}`, `{1}` as they are), then add the language code to `Lang.Available` in [src/Lang.cs](src/Lang.cs).

### Limitations

- Cannot touch apps running as Administrator.
- Grouping by process name is coarse: a window on a second monitor that you only look at without clicking may still count as "unused". In that case, add the app to `Exclusions`.
- No benchmark yet proves the effect on every machine. Turn on `DryRun` to see what RamNap *would* do first.
- Apps that keep using a little CPU, such as browsers and chat apps, may rarely nap: one scan at or above `CpuActivePercent` restarts the idle timer.
- Software cannot create more RAM. If RAM is often full during normal work, the fix is closing apps or upgrading RAM.

</details>

<details>
<summary><b>🇨🇳 简体中文</b></summary>

### RamNap 是什么？

一个小巧的 Windows 应用，让你**看清内存都去哪儿了**，并在**不影响使用体验的前提下拿回内存**：

- **所有占用内存的项目**，包括隐藏的：已打开的应用、托盘中的后台应用、每个隐藏服务（每个 `svchost` 都单独列出并显示真实的服务名）、Windows 组件、内存压缩存储，以及不属于任何应用的内存（Windows 内核、驱动、GPU、共享内存）。
- **节省顾问**：给出具体建议并显示可回收的内存，例如开启浏览器的“节省内存”、退出占用大的后台应用、取消开机自启。最终操作总由你来点击。运行时、数据库和虚拟机（`node`、`python`、`postgres` 等）没有强制退出按钮，已排除的应用也不会收到关闭、退出或取消自启的建议。
- **让被遗忘的应用“休眠”**：内存不足时，Windows 会优先从你忘掉的应用那里拿回内存，而不是从你正在使用的应用。

### RamNap 自己占用多少内存？

在 16 GB 的电脑上用任务管理器的“内存”列（专用工作集）测得：

| 状态 | 内存 |
|---|---|
| 在托盘中运行 | **约 4 MB** |
| 打开窗口时 | 约 10 MB |
| 关闭窗口后 | 约 7 MB |

窗口只在你打开时创建，关闭即销毁。RamNap 通过一次 `NtQuerySystemInformation` 调用扫描系统，不会逐个打开应用句柄，并且只把正在使用的语言加载到内存中。

### 休眠模式如何工作

1. 每 5 秒，RamNap 查看你正在使用哪个窗口，以及每个应用占用多少 CPU。
2. **有窗口**、**你 10 分钟没有打开过**、并且**几乎不占 CPU** 的应用会进入休眠：RamNap 把它的*内存优先级*降为低。
3. 默认情况下，RamNap 只降低优先级。把休眠应用移出工作集的功能处于**关闭**状态，因为目前还没有基准测试证明它有好处。如果将 `TrimEnabled` 设为 `true`，RamNap 会在内存使用 **≥ 80%** 时执行此操作，每次扫描只处理**一个**应用。
4. 你一切换到某个应用，它的优先级就会**立即**恢复正常。已离开内存的数据仍需要时间重新加载。
5. RamNap 通过检查 CPU 占用（单核 2% 及以上）**尽量避开**正在工作的应用，但不检查音频或网络，因此缓慢的下载或正在播放音乐的标签页仍可能进入休眠。**始终排除**：常见音乐播放器（Spotify、VLC 等）；系统进程、`svchost`、WebView2；越南语输入法（UniKey、EVKey、OpenKey）、AutoHotkey、PowerToys、密码管理器。
6. 如果 RamNap 意外关闭，下次启动时会自动把所有应用恢复正常。

**取舍：**内存已经紧张时，重新打开一个休眠了很久的应用，可能会在重新加载数据时短暂卡顿。内存充足时，RamNap 几乎什么都不做。

### 构建和运行

需要 .NET 10 SDK。

```bash
dotnet build -c Release
```

运行 `bin\Release\net10.0-windows\RamNap.exe`。关闭窗口后 RamNap 仍留在系统托盘中（月牙图标）；双击重新打开，右键打开菜单。不做任何更改、只查看内存状态：

```bash
bin\Release\net10.0-windows\RamNap.exe --scan
```

### 设置

文件 `%APPDATA%\RamNap\settings.json`（菜单 → *打开设置文件*，修改后选择*重新加载设置*）：

| 键 | 默认值 | 含义 |
|---|---|---|
| `Language` | auto | 语言：`auto`（跟随 Windows）、`vi`、`en`、`zh`、`ja`、`ko`、`fr`、`es`、`pt`、`id`、`ru` |
| `IdleMinutes` | 10 | 应用休眠前的未使用分钟数 |
| `CpuActivePercent` | 2.0 | 达到此值（单个 CPU 核心的百分比）即视为应用正在工作 |
| `TrimEnabled` | false | 内存紧张时把休眠应用移出工作集。因尚未得到验证，默认关闭 |
| `PressurePercent` | 80 | 开启 `TrimEnabled` 时：开始释放内存的内存使用率（%） |
| `TrimCooldownSeconds` | 300 | 同一应用两次释放之间的最短间隔 |
| `IntervalSeconds` | 5 | 扫描间隔 |
| `DryRun` | false | `true` = 只写日志，不做任何更改 |
| `RequireWindow` | true | 只让有窗口的应用休眠 |
| `TrayAppsAllowed` | Discord、Slack、Telegram、Zalo、Steam… | 允许休眠的托盘应用 |
| `Exclusions` | （空） | 额外排除的进程名 |

如果 `settings.json` 有错误（包括键名拼写错误），RamNap 不会应用也不会覆盖该文件。*重新加载设置* 会保留当前设置；如果启动时文件有错误，RamNap 会以试运行模式运行，直到你修复文件。允许多余的逗号和注释。

所有操作都会写入 `%APPDATA%\RamNap\ramnap.log`。

### 语言

切换语言：右键单击托盘图标 → **语言**。添加新语言：把 `lang\en.json` 复制为新文件并翻译各个值（保持 `{0}`、`{1}` 不变），然后把语言代码加入 [src/Lang.cs](src/Lang.cs) 中的 `Lang.Available`。

### 局限

- 无法处理以管理员身份运行的应用。
- 按进程名分组比较粗略：在第二块屏幕上只看不点的窗口，仍可能被算作“未使用”。遇到这种情况，请把该应用加入 `Exclusions`。
- 目前还没有在每台电脑上证明效果的基准测试。可以先开启 `DryRun`，看看 RamNap *会*做什么。
- 持续占用少量 CPU 的应用（例如浏览器和聊天应用）可能很少进入休眠：只要有一次扫描达到 `CpuActivePercent`，空闲计时就会重新开始。
- 软件无法凭空增加内存。如果日常使用时内存经常占满，解决办法是关闭部分应用或增加内存。

</details>

<details>
<summary><b>🇯🇵 日本語</b></summary>

### RamNap とは？

Windows 用の小さなアプリです。**RAM がどこに使われているかを見える化**し、**使い心地を損なわずに RAM を取り戻す**手助けをします：

- **RAM を使っているすべて**（隠れたものも含む）：開いているアプリ、トレイで動くバックグラウンド アプリ、隠れたサービス（`svchost` ごとに分けて実際のサービス名を表示）、Windows のコンポーネント、メモリ圧縮ストア、どのアプリにも属さない RAM（Windows カーネル、ドライバー、GPU、共有メモリ）。
- **節約アドバイザー**：回収できる RAM の量とともに具体的な提案を表示します。例：ブラウザーのメモリ セーバーをオンにする、重いバックグラウンド アプリを終了する、Windows 起動時の自動実行を止める。最後のクリックは常にあなたが行います。ランタイム、データベース、仮想マシン（`node`、`python`、`postgres` など）には強制終了ボタンを表示しません。除外したアプリには、閉じる・終了・自動実行の停止を提案しません。
- **忘れられたアプリを「休止」**：RAM が足りなくなると、Windows は使用中のアプリではなく、忘れられたアプリから先に RAM を取り戻します。

### RamNap 自体の RAM 使用量は？

16 GB の PC で、タスク マネージャーの「メモリ」列（プライベート ワーキング セット）で測定：

| 状態 | RAM |
|---|---|
| トレイで実行中 | **約 4 MB** |
| ウィンドウを開いているとき | 約 10 MB |
| ウィンドウを閉じた後 | 約 7 MB |

ウィンドウは開いたときにだけ作られ、閉じると破棄されます。RamNap は `NtQuerySystemInformation` を 1 回呼ぶだけでシステムをスキャンし、アプリごとにハンドルを開くことはありません。また、使用中の言語だけを RAM に読み込みます。

### 休止モードのしくみ

1. RamNap は 5 秒ごとに、使用中のウィンドウと各アプリの CPU 使用量を確認します。
2. **ウィンドウがあり**、**10 分間開かれておらず**、**CPU をほとんど使っていない**アプリは休止します：RamNap はその*メモリ優先度*を低に下げます。
3. 既定では、RamNap は優先度を下げるだけです。休止中のアプリをワーキング セットから押し出す機能は、効果を示すベンチマークがまだないため**オフ**です。`TrimEnabled` を `true` にすると、RAM 使用率が **80% 以上**のときにこれを行います。1 回のスキャンで扱うのは **1 つ**のアプリだけです。
4. アプリに切り替えると、優先度は**すぐに**通常に戻ります。RAM から出たデータの再読み込みには時間がかかります。
5. RamNap は CPU 使用量（1 コアの 2% 以上）を確認して、作業中のアプリを**避けるようにします**が、音声やネットワークは確認しません。そのため、低速なダウンロードや音楽を再生中のタブが休止することもあります。**常に除外**：一般的な音楽プレーヤー（Spotify、VLC など）、システム プロセス、`svchost`、WebView2、ベトナム語入力方式（UniKey、EVKey、OpenKey）、AutoHotkey、PowerToys、パスワード マネージャー。
6. RamNap が異常終了しても、次回起動時にすべてのアプリを通常に戻します。

**トレードオフ：**RAM がすでに逼迫しているとき、長く休止していたアプリを開き直すと、データの再読み込みで一瞬止まることがあります。RAM に余裕があるとき、RamNap はほとんど何もしません。

### ビルドと実行

.NET 10 SDK が必要です。

```bash
dotnet build -c Release
```

`bin\Release\net10.0-windows\RamNap.exe` を実行します。ウィンドウを閉じても RamNap はシステム トレイ（三日月アイコン）に残ります。ダブルクリックで再表示、右クリックでメニューを開きます。何も変更せずに RAM の状態だけを見るには：

```bash
bin\Release\net10.0-windows\RamNap.exe --scan
```

### 設定

ファイル `%APPDATA%\RamNap\settings.json`（メニュー → *設定ファイルを開く*、編集後に*設定を再読み込み*）：

| キー | 既定値 | 意味 |
|---|---|---|
| `Language` | auto | 言語：`auto`（Windows の言語）、`vi`、`en`、`zh`、`ja`、`ko`、`fr`、`es`、`pt`、`id`、`ru` |
| `IdleMinutes` | 10 | アプリが休止するまでの未使用時間（分） |
| `CpuActivePercent` | 2.0 | この値（CPU 1 コアに対する %）以上なら、アプリは作業中とみなす |
| `TrimEnabled` | false | RAM が逼迫したときに休止中のアプリをワーキング セットから押し出します。効果が未検証のため既定はオフ |
| `PressurePercent` | 80 | `TrimEnabled` がオンのとき：RAM の解放を始める RAM 使用率（%） |
| `TrimCooldownSeconds` | 300 | 同じアプリを解放する間隔の最小値 |
| `IntervalSeconds` | 5 | スキャン間隔 |
| `DryRun` | false | `true` = ログを書くだけで何も変更しない |
| `RequireWindow` | true | ウィンドウのあるアプリだけを休止させる |
| `TrayAppsAllowed` | Discord、Slack、Telegram、Zalo、Steam など | 休止してよいトレイ アプリ |
| `Exclusions` | （空） | 追加で除外するプロセス名 |

`settings.json` にエラーがある場合（キー名の入力ミスを含む）、RamNap はそのファイルを適用も上書きもしません。*設定を再読み込み* では現在の設定を維持します。エラーのあるファイルで起動した場合は、ファイルを修正するまでドライ ラン モードで動作します。余分なカンマとコメントは使用できます。

すべての操作は `%APPDATA%\RamNap\ramnap.log` に記録されます。

### 言語

言語の切り替え：トレイ アイコンを右クリック → **言語**。新しい言語を追加するには、`lang\en.json` を新しいファイルにコピーして値を翻訳し（`{0}`、`{1}` はそのまま）、[src/Lang.cs](src/Lang.cs) の `Lang.Available` に言語コードを追加します。

### 制限事項

- 管理者として実行中のアプリには触れられません。
- プロセス名でのグループ化は大まかです：2 台目のモニターで見ているだけでクリックしていないウィンドウは「未使用」とみなされることがあります。その場合はアプリを `Exclusions` に追加してください。
- すべての PC で効果を証明するベンチマークはまだありません。まず `DryRun` をオンにして、RamNap が何を*する予定か*を確認できます。
- ブラウザーやチャット アプリのように少しずつ CPU を使い続けるアプリは、ほとんど休止しない場合があります。1 回のスキャンでも `CpuActivePercent` に達すると、待ち時間が最初からやり直しになるためです。
- ソフトウェアで RAM を増やすことはできません。普段の作業でも RAM がよく満杯になるなら、アプリを閉じるか RAM を増設するしかありません。

</details>

<details>
<summary><b>🇰🇷 한국어</b></summary>

### RamNap이란?

Windows용 작은 앱으로, **RAM이 어디에 쓰이는지 한눈에 보여 주고**, **사용 경험을 해치지 않으면서 RAM을 되찾도록** 도와줍니다:

- **RAM을 사용하는 모든 것**(숨겨진 것 포함): 열린 앱, 트레이의 백그라운드 앱, 숨겨진 서비스(각 `svchost`를 따로 나누어 실제 서비스 이름으로 표시), Windows 구성 요소, 메모리 압축 저장소, 어떤 앱에도 속하지 않는 RAM(Windows 커널, 드라이버, GPU, 공유 메모리).
- **절약 도우미**: 되찾을 수 있는 RAM 양과 함께 구체적인 제안을 보여 줍니다. 예: 브라우저의 메모리 절약 모드 켜기, 무거운 백그라운드 앱 종료, Windows 시작 시 자동 실행 끄기. 마지막 클릭은 항상 사용자가 합니다. 런타임, 데이터베이스, 가상 머신(`node`, `python`, `postgres` 등)에는 강제 종료 버튼이 없으며, 제외한 앱에는 닫기, 종료, 자동 실행 끄기를 제안하지 않습니다.
- **잊힌 앱을 "절전"**: RAM이 부족해지면 Windows가 사용 중인 앱이 아니라 잊힌 앱에서 먼저 RAM을 가져옵니다.

### RamNap 자체는 RAM을 얼마나 쓰나요?

16 GB PC에서 작업 관리자의 "메모리" 열(개인 작업 집합)로 측정:

| 상태 | RAM |
|---|---|
| 트레이에서 실행 중 | **약 4 MB** |
| 창을 연 상태 | 약 10 MB |
| 창을 닫은 후 | 약 7 MB |

창은 열 때만 만들어지고 닫으면 제거됩니다. RamNap은 `NtQuerySystemInformation`을 한 번 호출해 시스템을 검사하며, 앱마다 핸들을 열지 않습니다. 또한 사용 중인 언어만 RAM에 불러옵니다.

### 절전 모드 작동 방식

1. RamNap은 5초마다 사용 중인 창과 각 앱의 CPU 사용량을 확인합니다.
2. **창이 있고**, **10분 동안 열지 않았으며**, **CPU를 거의 쓰지 않는** 앱은 절전 상태가 됩니다: RamNap이 *메모리 우선순위*를 낮음으로 내립니다.
3. 기본적으로 RamNap은 우선순위만 낮춥니다. 절전 중인 앱을 작업 집합에서 내보내는 기능은 효과를 입증한 벤치마크가 아직 없어 **꺼져** 있습니다. `TrimEnabled`를 `true`로 설정하면 RAM 사용률이 **80% 이상**일 때 이 작업을 하며, 검사 한 번에 **한 개**의 앱만 처리합니다.
4. 앱으로 전환하는 즉시 우선순위가 **바로** 정상으로 돌아옵니다. 이미 RAM에서 나간 데이터는 다시 불러오는 데 시간이 걸립니다.
5. RamNap은 CPU 사용량(코어 하나의 2% 이상)을 확인해 작업 중인 앱을 **피하려고 합니다**. 하지만 오디오나 네트워크는 확인하지 않으므로 느린 다운로드나 음악을 재생하는 탭은 절전될 수 있습니다. **항상 제외**: 일반적인 음악 플레이어(Spotify, VLC 등), 시스템 프로세스, `svchost`, WebView2, 베트남어 입력기(UniKey, EVKey, OpenKey), AutoHotkey, PowerToys, 비밀번호 관리자.
6. RamNap이 비정상 종료되어도 다음 실행 때 모든 앱을 정상으로 되돌립니다.

**절충점:** RAM이 이미 부족할 때 오래 절전했던 앱을 다시 열면 데이터를 다시 불러오느라 잠깐 멈출 수 있습니다. RAM이 넉넉하면 RamNap은 거의 아무것도 하지 않습니다.

### 빌드 및 실행

.NET 10 SDK가 필요합니다.

```bash
dotnet build -c Release
```

`bin\Release\net10.0-windows\RamNap.exe`를 실행합니다. 창을 닫아도 RamNap은 시스템 트레이(초승달 아이콘)에 남아 있으며, 두 번 클릭하면 다시 열고 마우스 오른쪽 버튼으로 메뉴를 엽니다. 아무것도 바꾸지 않고 RAM 상태만 보려면:

```bash
bin\Release\net10.0-windows\RamNap.exe --scan
```

### 설정

파일 `%APPDATA%\RamNap\settings.json` (메뉴 → *설정 파일 열기*, 수정 후 *설정 다시 불러오기*):

| 키 | 기본값 | 의미 |
|---|---|---|
| `Language` | auto | 언어: `auto`(Windows 언어), `vi`, `en`, `zh`, `ja`, `ko`, `fr`, `es`, `pt`, `id`, `ru` |
| `IdleMinutes` | 10 | 앱이 절전되기까지의 미사용 시간(분) |
| `CpuActivePercent` | 2.0 | 이 값(CPU 코어 1개 기준 %) 이상이면 앱이 작업 중인 것으로 봄 |
| `TrimEnabled` | false | RAM이 부족할 때 절전 중인 앱을 작업 집합에서 내보냅니다. 효과가 검증되지 않아 기본값은 꺼짐 |
| `PressurePercent` | 80 | `TrimEnabled`가 켜져 있을 때: RAM 해제를 시작하는 RAM 사용률(%) |
| `TrimCooldownSeconds` | 300 | 같은 앱을 두 번 해제하는 최소 간격 |
| `IntervalSeconds` | 5 | 검사 주기 |
| `DryRun` | false | `true` = 로그만 쓰고 아무것도 바꾸지 않음 |
| `RequireWindow` | true | 창이 있는 앱만 절전 |
| `TrayAppsAllowed` | Discord, Slack, Telegram, Zalo, Steam 등 | 절전해도 되는 트레이 앱 |
| `Exclusions` | (비어 있음) | 추가로 제외할 프로세스 이름 |

`settings.json`에 오류가 있으면(키 이름 오타 포함) RamNap은 파일을 적용하지도 덮어쓰지도 않습니다. *설정 다시 불러오기*은(는) 현재 설정을 유지합니다. 오류가 있는 파일로 시작하면 파일을 고칠 때까지 시험 실행 모드로 동작합니다. 불필요한 쉼표와 주석은 허용됩니다.

모든 작업은 `%APPDATA%\RamNap\ramnap.log`에 기록됩니다.

### 언어

언어 변경: 트레이 아이콘을 마우스 오른쪽 버튼으로 클릭 → **언어**. 새 언어를 추가하려면 `lang\en.json`을 새 파일로 복사해 값을 번역하고(`{0}`, `{1}`은 그대로 유지), [src/Lang.cs](src/Lang.cs)의 `Lang.Available`에 언어 코드를 추가하세요.

### 한계

- 관리자 권한으로 실행 중인 앱은 건드릴 수 없습니다.
- 프로세스 이름으로 묶는 방식이라 다소 거칩니다: 보조 모니터에서 보기만 하고 클릭하지 않는 창은 "미사용"으로 간주될 수 있습니다. 그럴 때는 해당 앱을 `Exclusions`에 추가하세요.
- 모든 PC에서 효과를 입증하는 벤치마크는 아직 없습니다. 먼저 `DryRun`을 켜서 RamNap이 무엇을 *할지* 확인할 수 있습니다.
- 브라우저나 채팅 앱처럼 CPU를 조금씩 계속 쓰는 앱은 거의 절전되지 않을 수 있습니다. 검사 한 번이라도 `CpuActivePercent`에 도달하면 대기 시간이 처음부터 다시 시작되기 때문입니다.
- 소프트웨어로 RAM을 늘릴 수는 없습니다. 평소 작업에서도 RAM이 자주 가득 찬다면 앱을 닫거나 RAM을 늘리는 것이 해결책입니다.

</details>

<details>
<summary><b>🇫🇷 Français</b></summary>

### Qu'est-ce que RamNap ?

Une petite appli Windows qui vous montre **où passe votre RAM** et vous aide à **récupérer de la RAM sans dégrader votre confort** :

- **Tout ce qui utilise la RAM**, y compris ce qui est caché : applis ouvertes, applis en arrière-plan dans la zone de notification, chaque service caché (chaque `svchost` est séparé et porte le nom de son vrai service), composants de Windows, magasin de compression mémoire, et la RAM qui n'appartient à aucune appli (noyau Windows, pilotes, GPU, mémoire partagée).
- **Conseiller d'économie** : des suggestions concrètes avec la quantité de RAM récupérable, par exemple activer l'économiseur de mémoire du navigateur, quitter les applis lourdes en arrière-plan ou empêcher des applis de se lancer au démarrage. C'est toujours vous qui cliquez. Les runtimes, bases de données et machines virtuelles (`node`, `python`, `postgres`...) n'ont pas de bouton de fermeture forcée, et les applis exclues ne reçoivent aucune suggestion de fermeture ou de retrait du démarrage.
- **Met en « sommeil » les applis oubliées** : quand la RAM manque, Windows reprend d'abord la RAM des applis que vous avez oubliées, plutôt que celle de l'appli que vous utilisez.

### Combien de RAM RamNap utilise-t-il ?

Mesuré avec la colonne « Mémoire » du Gestionnaire des tâches (plage de travail privée) sur une machine de 16 Go :

| État | RAM |
|---|---|
| En arrière-plan dans la zone de notification | **~4 Mo** |
| Fenêtre ouverte | ~10 Mo |
| Après fermeture de la fenêtre | ~7 Mo |

La fenêtre n'est créée que lorsque vous l'ouvrez et elle est détruite à la fermeture. RamNap analyse le système en un seul appel à `NtQuerySystemInformation`, sans ouvrir de handle vers chaque appli, et ne charge en RAM que la langue utilisée.

### Fonctionnement du mode sommeil

1. Toutes les 5 secondes, RamNap regarde quelle fenêtre vous utilisez et combien de CPU consomme chaque appli.
2. Une appli **avec une fenêtre**, **que vous n'avez pas ouverte depuis 10 minutes** et **qui n'utilise presque pas de CPU** est mise en sommeil : RamNap abaisse sa *priorité mémoire* à Faible.
3. Par défaut, RamNap abaisse seulement la priorité. La sortie des applis en sommeil de leur working set est **désactivée**, car aucun benchmark n'a encore montré son utilité. Si vous réglez `TrimEnabled` sur `true`, RamNap le fait lorsque la RAM est utilisée à **80 % ou plus**, **une** appli par analyse.
4. Dès que vous passez à une appli, sa priorité revient à la normale **immédiatement**. Les données déjà sorties de la RAM mettent tout de même du temps à être rechargées.
5. RamNap **essaie d'éviter** les applis au travail en vérifiant leur CPU (2 % d'un cœur ou plus). Il ne vérifie ni le son ni le réseau : un téléchargement lent ou un onglet qui lit de la musique peut donc être mis en sommeil. **Toujours exclus** : les lecteurs de musique courants (Spotify, VLC...) ; les processus système, `svchost`, WebView2 ; les méthodes de saisie vietnamiennes (UniKey, EVKey, OpenKey), AutoHotkey, PowerToys, gestionnaires de mots de passe.
6. Si RamNap se ferme brutalement, il remet toutes les applis à la normale au prochain lancement.

**Compromis :** quand la RAM est déjà saturée, rouvrir une appli en sommeil depuis longtemps peut marquer un court temps d'arrêt pendant le rechargement de ses données. Quand vous avez de la RAM en réserve, RamNap ne fait presque rien.

### Compiler et lancer

Nécessite le SDK .NET 10.

```bash
dotnet build -c Release
```

Lancez `bin\Release\net10.0-windows\RamNap.exe`. Après la fermeture de la fenêtre, RamNap reste dans la zone de notification (icône en croissant de lune) ; double-cliquez pour la rouvrir, clic droit pour le menu. Pour voir l'état de la RAM sans rien modifier :

```bash
bin\Release\net10.0-windows\RamNap.exe --scan
```

### Paramètres

Fichier `%APPDATA%\RamNap\settings.json` (menu → *Ouvrir le fichier de paramètres*, puis *Recharger les paramètres* après modification) :

| Clé | Par défaut | Signification |
|---|---|---|
| `Language` | auto | Langue : `auto` (langue de Windows), `vi`, `en`, `zh`, `ja`, `ko`, `fr`, `es`, `pt`, `id`, `ru` |
| `IdleMinutes` | 10 | Minutes sans utilisation avant la mise en sommeil |
| `CpuActivePercent` | 2.0 | À partir de cette valeur (% d'un cœur CPU), l'appli est considérée comme active |
| `TrimEnabled` | false | Sort les applis en sommeil de leur working set quand la RAM est saturée. Désactivé par défaut, car non prouvé |
| `PressurePercent` | 80 | Avec `TrimEnabled` activé : taux d'utilisation de la RAM (%) à partir duquel la RAM est libérée |
| `TrimCooldownSeconds` | 300 | Délai minimum entre deux libérations de la même appli |
| `IntervalSeconds` | 5 | Intervalle d'analyse |
| `DryRun` | false | `true` = écrire seulement le journal, ne rien modifier |
| `RequireWindow` | true | Ne mettre en sommeil que les applis qui ont une fenêtre |
| `TrayAppsAllowed` | Discord, Slack, Telegram, Zalo, Steam... | Applis de la zone de notification pouvant être mises en sommeil |
| `Exclusions` | (vide) | Noms de processus supplémentaires à exclure |

Si `settings.json` contient une erreur (y compris une clé mal orthographiée), RamNap ne l'applique pas et ne l'écrase pas. *Recharger les paramètres* conserve les paramètres actuels ; si RamNap démarre avec un fichier erroné, il fonctionne en mode test jusqu'à ce que vous corrigiez le fichier. Les virgules en trop et les commentaires sont acceptés.

Chaque action est écrite dans `%APPDATA%\RamNap\ramnap.log`.

### Langues

Changer de langue : clic droit sur l'icône → **Langue**. Pour ajouter une langue : copiez `lang\en.json` dans un nouveau fichier, traduisez les valeurs (gardez `{0}`, `{1}` tels quels), puis ajoutez le code de langue à `Lang.Available` dans [src/Lang.cs](src/Lang.cs).

### Limites

- Ne peut pas agir sur les applis lancées en tant qu'administrateur.
- Le regroupement par nom de processus reste approximatif : une fenêtre sur un second écran que vous regardez sans cliquer peut être considérée comme « inutilisée ». Dans ce cas, ajoutez l'appli à `Exclusions`.
- Aucun benchmark ne prouve encore l'effet sur chaque machine. Activez `DryRun` pour voir d'abord ce que RamNap *ferait*.
- Les applis qui utilisent en continu un peu de CPU, comme les navigateurs et les applis de discussion, peuvent rarement être mises en sommeil : une seule analyse atteignant `CpuActivePercent` relance le délai d'inactivité.
- Un logiciel ne peut pas créer de RAM. Si la RAM est souvent pleine en usage normal, la solution est de fermer des applis ou d'ajouter de la RAM.

</details>

<details>
<summary><b>🇪🇸 Español</b></summary>

### ¿Qué es RamNap?

Una pequeña app para Windows que te muestra **en qué se va tu RAM** y te ayuda a **recuperar RAM sin empeorar tu experiencia**:

- **Todo lo que usa RAM**, incluido lo oculto: apps abiertas, apps en segundo plano de la bandeja, cada servicio oculto (cada `svchost` se separa y lleva el nombre de su servicio real), componentes de Windows, el almacén de compresión de memoria y la RAM que no pertenece a ninguna app (kernel de Windows, controladores, GPU, memoria compartida).
- **Asesor de ahorro**: sugerencias concretas con la RAM que puedes recuperar, como activar el ahorro de memoria del navegador, cerrar apps pesadas en segundo plano o evitar que se inicien con Windows. Siempre eres tú quien hace el último clic. Los runtimes, bases de datos y máquinas virtuales (`node`, `python`, `postgres`...) no tienen botón para forzar el cierre, y las apps excluidas no reciben sugerencias de cerrar, salir o quitar del inicio.
- **Pone a "dormir" las apps olvidadas**: cuando falta RAM, Windows quita RAM primero a las apps que olvidaste, en lugar de a la que estás usando.

### ¿Cuánta RAM usa RamNap?

Medido con la columna "Memoria" del Administrador de tareas (espacio de trabajo privado) en un equipo de 16 GB:

| Estado | RAM |
|---|---|
| En la bandeja | **~4 MB** |
| Ventana abierta | ~10 MB |
| Tras cerrar la ventana | ~7 MB |

La ventana solo se crea cuando la abres y se destruye al cerrarla. RamNap analiza el sistema con una sola llamada a `NtQuerySystemInformation`, sin abrir un handle a cada app, y solo carga en RAM el idioma en uso.

### Cómo funciona el modo dormir

1. Cada 5 segundos, RamNap comprueba qué ventana estás usando y cuánta CPU usa cada app.
2. Una app **con ventana** que **no has abierto en 10 minutos** y que **casi no usa CPU** se pone a dormir: RamNap baja su *prioridad de memoria* a Baja.
3. De forma predeterminada, RamNap solo baja la prioridad. Sacar a las apps dormidas de su working set está **desactivado**, porque ningún benchmark ha demostrado aún que ayude. Si pones `TrimEnabled` en `true`, RamNap lo hace cuando el uso de RAM es **≥ 80 %**, **una** app por análisis.
4. En cuanto cambias a una app, su prioridad vuelve a la normalidad **al instante**. Los datos que ya salieron de la RAM tardan un tiempo en volver a cargarse.
5. RamNap **intenta evitar** las apps que están trabajando comprobando su CPU (2 % de un núcleo o más). No comprueba el audio ni la red, así que una descarga lenta o una pestaña que reproduce música aún pueden dormirse. **Siempre excluidos**: reproductores de música comunes (Spotify, VLC...); procesos del sistema, `svchost`, WebView2; métodos de entrada vietnamitas (UniKey, EVKey, OpenKey), AutoHotkey, PowerToys, gestores de contraseñas.
6. Si RamNap se cierra de forma inesperada, devuelve todas las apps a la normalidad la próxima vez que se inicie.

**Compromiso:** cuando la RAM ya está al límite, volver a abrir una app que lleva mucho tiempo dormida puede detenerse un momento mientras recarga sus datos. Cuando te sobra RAM, RamNap casi no hace nada.

### Compilar y ejecutar

Requiere el SDK de .NET 10.

```bash
dotnet build -c Release
```

Ejecuta `bin\Release\net10.0-windows\RamNap.exe`. Al cerrar la ventana, RamNap sigue en la bandeja del sistema (icono de luna creciente); haz doble clic para volver a abrirla y clic derecho para el menú. Para ver el estado de la RAM sin cambiar nada:

```bash
bin\Release\net10.0-windows\RamNap.exe --scan
```

### Configuración

Archivo `%APPDATA%\RamNap\settings.json` (menú → *Abrir archivo de configuración*, y después *Recargar configuración*):

| Clave | Predeterminado | Significado |
|---|---|---|
| `Language` | auto | Idioma: `auto` (idioma de Windows), `vi`, `en`, `zh`, `ja`, `ko`, `fr`, `es`, `pt`, `id`, `ru` |
| `IdleMinutes` | 10 | Minutos sin uso antes de que una app duerma |
| `CpuActivePercent` | 2.0 | A partir de este valor (% de un núcleo de CPU) la app se considera activa |
| `TrimEnabled` | false | Saca a las apps dormidas de su working set cuando la RAM está al límite. Desactivado de forma predeterminada porque no está demostrado |
| `PressurePercent` | 80 | Con `TrimEnabled` activado: uso de RAM (%) a partir del cual se libera RAM |
| `TrimCooldownSeconds` | 300 | Tiempo mínimo entre dos liberaciones de la misma app |
| `IntervalSeconds` | 5 | Intervalo de análisis |
| `DryRun` | false | `true` = solo escribe el registro, no cambia nada |
| `RequireWindow` | true | Solo duerme apps que tienen ventana |
| `TrayAppsAllowed` | Discord, Slack, Telegram, Zalo, Steam... | Apps de la bandeja que sí pueden dormir |
| `Exclusions` | (vacío) | Nombres de procesos adicionales a excluir |

Si `settings.json` tiene un error (incluida una clave mal escrita), RamNap no lo aplica ni lo sobrescribe. *Recargar configuración* conserva la configuración actual; si RamNap se inicia con un archivo con errores, funciona en modo de prueba hasta que corrijas el archivo. Se aceptan comas sobrantes y comentarios.

Cada acción se escribe en `%APPDATA%\RamNap\ramnap.log`.

### Idiomas

Cambiar de idioma: clic derecho en el icono de la bandeja → **Idioma**. Para añadir un idioma: copia `lang\en.json` a un archivo nuevo, traduce los valores (deja `{0}`, `{1}` tal cual) y añade el código del idioma a `Lang.Available` en [src/Lang.cs](src/Lang.cs).

### Limitaciones

- No puede actuar sobre apps que se ejecutan como administrador.
- Agrupar por nombre de proceso es aproximado: una ventana en un segundo monitor que solo miras sin hacer clic puede contar como "sin uso". En ese caso, añade la app a `Exclusions`.
- Aún no hay benchmarks que demuestren el efecto en cada equipo. Activa `DryRun` para ver primero lo que RamNap *haría*.
- Las apps que usan continuamente un poco de CPU, como navegadores y apps de chat, pueden dormirse rara vez: basta un análisis que alcance `CpuActivePercent` para reiniciar el tiempo de inactividad.
- Un programa no puede crear RAM. Si la RAM suele llenarse en el uso normal, la solución es cerrar apps o ampliar la RAM.

</details>

<details>
<summary><b>🇧🇷 Português</b></summary>

### O que é o RamNap?

Um pequeno app para Windows que mostra **para onde vai a sua RAM** e ajuda a **recuperar RAM sem piorar a sua experiência**:

- **Tudo o que usa RAM**, inclusive o que está oculto: apps abertos, apps em segundo plano na bandeja, cada serviço oculto (cada `svchost` aparece separado, com o nome do serviço real), componentes do Windows, o armazenamento de compressão de memória e a RAM que não pertence a nenhum app (kernel do Windows, drivers, GPU, memória compartilhada).
- **Consultor de economia**: sugestões concretas com quanta RAM dá para recuperar, como ativar a economia de memória do navegador, sair de apps pesados em segundo plano ou impedir apps de iniciar com o Windows. Quem dá o clique final é sempre você. Runtimes, bancos de dados e máquinas virtuais (`node`, `python`, `postgres`...) não têm botão para forçar o encerramento, e apps excluídos não recebem sugestões de fechar, sair ou remover da inicialização.
- **Faz os apps esquecidos "dormirem"**: quando falta RAM, o Windows tira RAM primeiro dos apps que você esqueceu, em vez do app que você está usando.

### Quanta RAM o RamNap usa?

Medido pela coluna "Memória" do Gerenciador de Tarefas (conjunto de trabalho privado) em um computador de 16 GB:

| Estado | RAM |
|---|---|
| Rodando na bandeja | **~4 MB** |
| Janela aberta | ~10 MB |
| Depois de fechar a janela | ~7 MB |

A janela só é criada quando você a abre e é destruída ao fechar. O RamNap verifica o sistema com uma única chamada a `NtQuerySystemInformation`, sem abrir um handle para cada app, e só carrega na RAM o idioma em uso.

### Como funciona o modo dormir

1. A cada 5 segundos, o RamNap verifica qual janela você está usando e quanta CPU cada app usa.
2. Um app **com janela** que **você não abre há 10 minutos** e que **quase não usa CPU** é posto para dormir: o RamNap reduz a *prioridade de memória* dele para Baixa.
3. Por padrão, o RamNap apenas reduz a prioridade. Tirar os apps adormecidos do working set está **desativado**, porque nenhum benchmark mostrou ainda que isso ajuda. Se você definir `TrimEnabled` como `true`, o RamNap faz isso quando o uso de RAM chega a **80 % ou mais**, **um** app por verificação.
4. Assim que você muda para um app, a prioridade dele volta ao normal **na hora**. Os dados que já saíram da RAM ainda levam um tempo para serem recarregados.
5. O RamNap **tenta evitar** apps que estão trabalhando verificando a CPU (2 % de um núcleo ou mais). Ele não verifica áudio nem rede, então um download lento ou uma aba tocando música ainda pode dormir. **Sempre excluídos**: players de música comuns (Spotify, VLC...); processos do sistema, `svchost`, WebView2; métodos de entrada vietnamitas (UniKey, EVKey, OpenKey), AutoHotkey, PowerToys, gerenciadores de senhas.
6. Se o RamNap fechar de repente, na próxima vez que iniciar ele devolve todos os apps ao normal.

**Contrapartida:** quando a RAM já está no limite, reabrir um app que dormiu por muito tempo pode travar por um instante enquanto os dados são recarregados. Quando sobra RAM, o RamNap quase não faz nada.

### Compilar e executar

Requer o SDK do .NET 10.

```bash
dotnet build -c Release
```

Execute `bin\Release\net10.0-windows\RamNap.exe`. Ao fechar a janela, o RamNap continua na bandeja do sistema (ícone de lua crescente); clique duas vezes para reabrir e com o botão direito para o menu. Para ver o estado da RAM sem alterar nada:

```bash
bin\Release\net10.0-windows\RamNap.exe --scan
```

### Configurações

Arquivo `%APPDATA%\RamNap\settings.json` (menu → *Abrir arquivo de configurações*, depois *Recarregar configurações*):

| Chave | Padrão | Significado |
|---|---|---|
| `Language` | auto | Idioma: `auto` (idioma do Windows), `vi`, `en`, `zh`, `ja`, `ko`, `fr`, `es`, `pt`, `id`, `ru` |
| `IdleMinutes` | 10 | Minutos sem uso até o app dormir |
| `CpuActivePercent` | 2.0 | A partir deste valor (% de um núcleo de CPU) o app conta como ativo |
| `TrimEnabled` | false | Tira os apps adormecidos do working set quando a RAM está no limite. Desativado por padrão porque não foi comprovado |
| `PressurePercent` | 80 | Com `TrimEnabled` ativado: uso de RAM (%) a partir do qual a RAM começa a ser liberada |
| `TrimCooldownSeconds` | 300 | Intervalo mínimo entre duas liberações do mesmo app |
| `IntervalSeconds` | 5 | Intervalo de verificação |
| `DryRun` | false | `true` = só grava o log, não altera nada |
| `RequireWindow` | true | Só faz dormir apps que têm janela |
| `TrayAppsAllowed` | Discord, Slack, Telegram, Zalo, Steam... | Apps da bandeja que podem dormir |
| `Exclusions` | (vazio) | Nomes de processos extras a excluir |

Se o `settings.json` tiver um erro (inclusive uma chave digitada errado), o RamNap não o aplica nem o sobrescreve. *Recarregar configurações* mantém as configurações atuais; se o RamNap iniciar com um arquivo com erro, ele funciona em modo de teste até você corrigir o arquivo. Vírgulas sobrando e comentários são aceitos.

Cada ação é gravada em `%APPDATA%\RamNap\ramnap.log`.

### Idiomas

Trocar o idioma: clique com o botão direito no ícone da bandeja → **Idioma**. Para adicionar um idioma: copie `lang\en.json` para um novo arquivo, traduza os valores (mantenha `{0}`, `{1}` como estão) e adicione o código do idioma a `Lang.Available` em [src/Lang.cs](src/Lang.cs).

### Limitações

- Não consegue agir em apps executados como administrador.
- Agrupar por nome de processo é aproximado: uma janela em um segundo monitor que você só olha, sem clicar, pode contar como "sem uso". Nesse caso, adicione o app a `Exclusions`.
- Ainda não há benchmarks que comprovem o efeito em todo computador. Ative `DryRun` para ver antes o que o RamNap *faria*.
- Apps que usam continuamente um pouco de CPU, como navegadores e apps de chat, podem raramente dormir: basta uma verificação que atinja `CpuActivePercent` para reiniciar o tempo de inatividade.
- Um programa não cria RAM. Se a RAM vive cheia no uso normal, a solução é fechar apps ou aumentar a RAM.

</details>

<details>
<summary><b>🇮🇩 Bahasa Indonesia</b></summary>

### Apa itu RamNap?

Aplikasi kecil untuk Windows yang menunjukkan **ke mana RAM Anda terpakai** dan membantu **mendapatkan kembali RAM tanpa mengganggu pengalaman Anda**:

- **Semua yang memakai RAM**, termasuk yang tersembunyi: aplikasi terbuka, aplikasi latar belakang di baki, setiap layanan tersembunyi (setiap `svchost` dipisahkan dan diberi nama layanan aslinya), komponen Windows, penyimpanan kompresi memori, dan RAM yang bukan milik aplikasi mana pun (kernel Windows, driver, GPU, memori bersama).
- **Penasihat hemat**: saran konkret beserta jumlah RAM yang bisa didapat kembali, misalnya mengaktifkan Penghemat Memori di browser, keluar dari aplikasi latar belakang yang berat, atau mencegah aplikasi berjalan saat Windows dimulai. Klik terakhir selalu ada di tangan Anda. Runtime, basis data, dan mesin virtual (`node`, `python`, `postgres`...) tidak punya tombol paksa keluar, dan aplikasi yang dikecualikan tidak mendapat saran untuk ditutup, dikeluarkan, atau dihapus dari startup.
- **Menidurkan aplikasi yang terlupakan**: saat RAM menipis, Windows mengambil RAM dari aplikasi yang Anda lupakan lebih dulu, bukan dari aplikasi yang sedang Anda pakai.

### Berapa banyak RAM yang dipakai RamNap?

Diukur dengan kolom "Memori" di Task Manager (private working set) pada komputer 16 GB:

| Keadaan | RAM |
|---|---|
| Berjalan di baki | **~4 MB** |
| Jendela terbuka | ~10 MB |
| Setelah jendela ditutup | ~7 MB |

Jendela hanya dibuat saat Anda membukanya dan dihapus saat ditutup. RamNap memindai sistem dengan satu panggilan `NtQuerySystemInformation` tanpa membuka handle ke setiap aplikasi, dan hanya memuat bahasa yang sedang dipakai ke RAM.

### Cara kerja mode tidur

1. Setiap 5 detik, RamNap melihat jendela mana yang Anda pakai dan berapa banyak CPU yang dipakai tiap aplikasi.
2. Aplikasi **yang punya jendela**, **tidak Anda buka selama 10 menit**, dan **hampir tidak memakai CPU** akan ditidurkan: RamNap menurunkan *prioritas memori*-nya menjadi Rendah.
3. Secara bawaan, RamNap hanya menurunkan prioritas. Mengeluarkan aplikasi yang tidur dari working set-nya **dimatikan**, karena belum ada benchmark yang menunjukkan manfaatnya. Jika Anda mengatur `TrimEnabled` ke `true`, RamNap melakukannya saat pemakaian RAM **≥ 80%**, **satu** aplikasi per pemindaian.
4. Begitu Anda beralih ke sebuah aplikasi, prioritasnya **langsung** kembali normal. Data yang sudah keluar dari RAM tetap butuh waktu untuk dimuat ulang.
5. RamNap **berusaha menghindari** aplikasi yang sedang bekerja dengan memeriksa CPU (2% dari satu inti atau lebih). RamNap tidak memeriksa audio atau jaringan, jadi unduhan lambat atau tab yang memutar musik masih bisa ditidurkan. **Selalu dikecualikan**: pemutar musik umum (Spotify, VLC...); proses sistem, `svchost`, WebView2; metode input bahasa Vietnam (UniKey, EVKey, OpenKey), AutoHotkey, PowerToys, pengelola kata sandi.
6. Jika RamNap tertutup tiba-tiba, saat dijalankan lagi ia mengembalikan semua aplikasi ke normal.

**Konsekuensi:** saat RAM sudah sangat terbatas, membuka lagi aplikasi yang lama tertidur bisa tersendat sebentar saat datanya dimuat ulang. Saat RAM masih longgar, RamNap hampir tidak melakukan apa-apa.

### Build dan jalankan

Memerlukan .NET 10 SDK.

```bash
dotnet build -c Release
```

Jalankan `bin\Release\net10.0-windows\RamNap.exe`. Setelah jendela ditutup, RamNap tetap ada di baki sistem (ikon bulan sabit); klik dua kali untuk membuka lagi, klik kanan untuk menu. Untuk melihat status RAM tanpa mengubah apa pun:

```bash
bin\Release\net10.0-windows\RamNap.exe --scan
```

### Pengaturan

File `%APPDATA%\RamNap\settings.json` (menu → *Buka file pengaturan*, lalu *Muat ulang pengaturan* setelah diubah):

| Kunci | Bawaan | Arti |
|---|---|---|
| `Language` | auto | Bahasa: `auto` (bahasa Windows), `vi`, `en`, `zh`, `ja`, `ko`, `fr`, `es`, `pt`, `id`, `ru` |
| `IdleMinutes` | 10 | Menit tanpa dipakai sebelum aplikasi ditidurkan |
| `CpuActivePercent` | 2.0 | Mulai dari nilai ini (% dari satu inti CPU) aplikasi dianggap sedang bekerja |
| `TrimEnabled` | false | Mengeluarkan aplikasi yang tidur dari working set saat RAM terbatas. Mati secara bawaan karena belum terbukti |
| `PressurePercent` | 80 | Saat `TrimEnabled` aktif: pemakaian RAM (%) saat RAM mulai dibebaskan |
| `TrimCooldownSeconds` | 300 | Jeda minimum antara dua pembebasan pada aplikasi yang sama |
| `IntervalSeconds` | 5 | Jeda pemindaian |
| `DryRun` | false | `true` = hanya menulis log, tidak mengubah apa pun |
| `RequireWindow` | true | Hanya menidurkan aplikasi yang punya jendela |
| `TrayAppsAllowed` | Discord, Slack, Telegram, Zalo, Steam... | Aplikasi baki yang tetap boleh ditidurkan |
| `Exclusions` | (kosong) | Nama proses tambahan yang dikecualikan |

Jika `settings.json` berisi kesalahan (termasuk nama kunci yang salah ketik), RamNap tidak menerapkan dan tidak menimpanya. *Muat ulang pengaturan* mempertahankan pengaturan saat ini; jika RamNap dimulai dengan file yang salah, ia berjalan dalam mode uji sampai Anda memperbaiki file. Koma berlebih dan komentar diperbolehkan.

Setiap tindakan dicatat di `%APPDATA%\RamNap\ramnap.log`.

### Bahasa

Ganti bahasa: klik kanan ikon di baki → **Bahasa**. Untuk menambah bahasa: salin `lang\en.json` ke file baru, terjemahkan nilainya (biarkan `{0}`, `{1}` apa adanya), lalu tambahkan kode bahasa ke `Lang.Available` di [src/Lang.cs](src/Lang.cs).

### Keterbatasan

- Tidak bisa menyentuh aplikasi yang berjalan sebagai Administrator.
- Pengelompokan berdasarkan nama proses masih kasar: jendela di monitor kedua yang hanya Anda lihat tanpa diklik bisa dianggap "tidak dipakai". Dalam kasus itu, tambahkan aplikasinya ke `Exclusions`.
- Belum ada benchmark yang membuktikan efeknya di setiap komputer. Aktifkan `DryRun` untuk melihat dulu apa yang *akan* dilakukan RamNap.
- Aplikasi yang terus memakai sedikit CPU, seperti browser dan aplikasi obrolan, mungkin jarang ditidurkan: satu pemindaian yang mencapai `CpuActivePercent` sudah mengulang hitungan waktu tidak aktif.
- Perangkat lunak tidak bisa menambah RAM. Jika RAM sering penuh saat dipakai normal, solusinya menutup sebagian aplikasi atau menambah RAM.

</details>

<details>
<summary><b>🇷🇺 Русский</b></summary>

### Что такое RamNap?

Небольшое приложение для Windows, которое показывает, **куда уходит ваша оперативная память**, и помогает **вернуть память, не ухудшая удобство работы**:

- **Всё, что занимает ОЗУ**, включая скрытое: открытые приложения, фоновые приложения в области уведомлений, каждая скрытая служба (каждый `svchost` показан отдельно с настоящим именем службы), компоненты Windows, хранилище сжатия памяти и память, не принадлежащая ни одному приложению (ядро Windows, драйверы, GPU, общая память).
- **Советник по экономии**: конкретные советы с объёмом памяти, который можно вернуть, например включить экономию памяти в браузере, закрыть тяжёлые фоновые приложения или отключить их автозапуск. Последний щелчок всегда за вами. Для сред выполнения, баз данных и виртуальных машин (`node`, `python`, `postgres`...) нет кнопки принудительного завершения, а исключённым приложениям не предлагается закрытие, выход или отключение автозапуска.
- **Усыпляет забытые приложения**: когда памяти не хватает, Windows сначала забирает её у забытых приложений, а не у того, с которым вы работаете.

### Сколько памяти занимает сам RamNap?

Измерено по столбцу «Память» диспетчера задач (частный рабочий набор) на компьютере с 16 ГБ:

| Состояние | ОЗУ |
|---|---|
| Работает в области уведомлений | **~4 МБ** |
| Окно открыто | ~10 МБ |
| После закрытия окна | ~7 МБ |

Окно создаётся только тогда, когда вы его открываете, и уничтожается при закрытии. RamNap сканирует систему одним вызовом `NtQuerySystemInformation`, не открывая дескриптор к каждому приложению, и загружает в память только используемый язык.

### Как работает режим сна

1. Каждые 5 секунд RamNap проверяет, каким окном вы пользуетесь и сколько процессорного времени тратит каждое приложение.
2. Приложение **с окном**, которое **вы не открывали 10 минут** и которое **почти не нагружает процессор**, усыпляется: RamNap понижает его *приоритет памяти* до низкого.
3. По умолчанию RamNap только понижает приоритет. Вывод спящих приложений из рабочего набора **отключён**, потому что пока ни один тест не показал его пользу. Если задать `TrimEnabled` значение `true`, RamNap делает это, когда ОЗУ занята на **80% и больше** — **одно** приложение за сканирование.
4. Как только вы переключаетесь на приложение, его приоритет **сразу** возвращается к обычному. Данным, которые уже покинули ОЗУ, всё равно нужно время на повторную загрузку.
5. RamNap **старается не трогать** работающие приложения, проверяя загрузку процессора (от 2% одного ядра). Звук и сеть он не проверяет, поэтому медленная загрузка или вкладка с музыкой всё же могут уснуть. **Исключены всегда**: популярные музыкальные плееры (Spotify, VLC...); системные процессы, `svchost`, WebView2; вьетнамские способы ввода (UniKey, EVKey, OpenKey), AutoHotkey, PowerToys, менеджеры паролей.
6. Если RamNap аварийно завершится, при следующем запуске он вернёт все приложения в нормальное состояние.

**Компромисс:** когда памяти уже не хватает, долго спавшее приложение при повторном открытии может ненадолго подвиснуть, пока его данные загружаются заново. Когда памяти достаточно, RamNap почти ничего не делает.

### Сборка и запуск

Нужен .NET 10 SDK.

```bash
dotnet build -c Release
```

Запустите `bin\Release\net10.0-windows\RamNap.exe`. После закрытия окна RamNap остаётся в области уведомлений (значок полумесяца); двойной щелчок открывает окно снова, правый щелчок — меню. Чтобы посмотреть состояние памяти, ничего не меняя:

```bash
bin\Release\net10.0-windows\RamNap.exe --scan
```

### Настройки

Файл `%APPDATA%\RamNap\settings.json` (меню → *Открыть файл настроек*, после правки — *Перезагрузить настройки*):

| Ключ | По умолчанию | Значение |
|---|---|---|
| `Language` | auto | Язык: `auto` (язык Windows), `vi`, `en`, `zh`, `ja`, `ko`, `fr`, `es`, `pt`, `id`, `ru` |
| `IdleMinutes` | 10 | Минут без использования до усыпления приложения |
| `CpuActivePercent` | 2.0 | Начиная с этого значения (% одного ядра процессора) приложение считается работающим |
| `TrimEnabled` | false | Выводит спящие приложения из рабочего набора, когда памяти не хватает. По умолчанию отключено, так как польза не доказана |
| `PressurePercent` | 80 | При включённом `TrimEnabled`: загрузка ОЗУ (%), при которой начинается освобождение памяти |
| `TrimCooldownSeconds` | 300 | Минимальный интервал между двумя освобождениями одного приложения |
| `IntervalSeconds` | 5 | Интервал сканирования |
| `DryRun` | false | `true` = только писать журнал, ничего не менять |
| `RequireWindow` | true | Усыплять только приложения с окном |
| `TrayAppsAllowed` | Discord, Slack, Telegram, Zalo, Steam... | Приложения из области уведомлений, которые можно усыплять |
| `Exclusions` | (пусто) | Дополнительные имена процессов для исключения |

Если в `settings.json` есть ошибка (в том числе опечатка в имени ключа), RamNap не применяет файл и не перезаписывает его. *Перезагрузить настройки* сохраняет текущие настройки; если RamNap запускается с ошибочным файлом, он работает в пробном режиме, пока вы не исправите файл. Лишние запятые и комментарии допускаются.

Каждое действие записывается в `%APPDATA%\RamNap\ramnap.log`.

### Языки

Сменить язык: правый щелчок по значку в области уведомлений → **Язык**. Чтобы добавить язык: скопируйте `lang\en.json` в новый файл, переведите значения (оставив `{0}`, `{1}` как есть) и добавьте код языка в `Lang.Available` в [src/Lang.cs](src/Lang.cs).

### Ограничения

- Не может работать с приложениями, запущенными от имени администратора.
- Группировка по имени процесса приблизительна: окно на втором мониторе, на которое вы только смотрите, не щёлкая, может считаться «неиспользуемым». В таком случае добавьте приложение в `Exclusions`.
- Пока нет тестов производительности, подтверждающих эффект на каждом компьютере. Включите `DryRun`, чтобы сначала посмотреть, что RamNap *сделал бы*.
- Приложения, которые постоянно используют немного процессора, например браузеры и чаты, могут засыпать редко: одно сканирование с уровнем от `CpuActivePercent` запускает отсчёт простоя заново.
- Программа не может создать память из ничего. Если ОЗУ часто заполнена при обычной работе, решение — закрыть часть приложений или добавить памяти.

</details>

---

📄 [MIT License](LICENSE) · © 2026 Tamekhanh
