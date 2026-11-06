## RamNap v0.1.1 — safer and more honest 🌙

Lightweight Windows tool (~4 MB RAM) that shows everything using your RAM, including hidden services, and helps you decide what to do about it.

*Công cụ Windows siêu nhẹ (~4 MB RAM): xem mọi thứ đang dùng RAM, kể cả dịch vụ ẩn, và giúp bạn quyết định nên làm gì.*

### ⬇️ Which file should I download? · Tải file nào?

| File | Size | Choose this if… |
|---|---|---|
| `RamNap-v0.1.1-win-x64.zip` | ~44 MB | **Most people.** Unzip and run, nothing else to install. · *Đa số người dùng: giải nén là chạy.* |
| `RamNap-v0.1.1-win-x64-lite.zip` | ~0.15 MB | You already have the [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0). · *Máy đã cài .NET 10 Desktop Runtime.* |

### 🔄 What changed · Thay đổi

- **Working-set trimming is now off by default.** No benchmark has shown it helps, and apps pushed out must load their data back. Nap now only lowers memory priority. To turn trimming back on, set `TrimEnabled` to `true`. · *Mặc định tắt việc đẩy working set vì chưa được kiểm chứng; đặt `TrimEnabled` thành `true` để bật lại.*
- **The "reclaimed" number is gone.** It counted pages moved out of working sets, not RAM you actually got back. · *Bỏ số "đã thu hồi" vì nó không phản ánh RAM lấy lại được.*
- **A mistake in `settings.json` no longer turns off your settings.** Reloading a broken file keeps the current settings. Starting with a broken file runs in dry run mode, and RamNap will not overwrite the file. · *File cài đặt lỗi không còn làm mất `DryRun` hay `Exclusions`, và không bị ghi đè.*
- **Unknown keys in `settings.json` now count as errors**, so a misspelled `DryRun` can't be silently ignored. Trailing commas and comments are accepted. · *Khóa lạ được coi là lỗi; dấu phẩy thừa và ghi chú thì được chấp nhận.*
- **Force quit is narrower.** No button for runtimes, databases and virtual machines (`node`, `python`, `postgres`...), since one name can cover many separate programs. The confirmation shows how many processes will end. · *Không còn nút buộc thoát cho runtime, cơ sở dữ liệu, máy ảo; hộp xác nhận cho biết số tiến trình.*
- **Excluded apps get no close, quit or startup tips.** Browser memory-saving tips still appear. · *App đã loại trừ không còn bị gợi ý đóng, thoát hay bỏ tự khởi động; gợi ý tiết kiệm RAM của trình duyệt vẫn hiện.*
- **Double-clicking a tip no longer runs its action**, and the button no longer switches to another tip when the selected one disappears. · *Nhấp đúp không còn chạy hành động; nút không tự đổi sang gợi ý khác.*
- **Corrected wording.** "Your RAM is being used sensibly" no longer appears when RAM is nearly full, and the docs now say what RamNap really checks. · *Sửa các câu chữ sai trong app và tài liệu.*

**Updating:** your existing `settings.json` keeps working. Trimming is off even if you used it before, unless you add `"TrimEnabled": true`. · *Cập nhật: file cài đặt cũ vẫn dùng được; muốn dùng trim thì thêm `"TrimEnabled": true`.*

### ⚠️ Good to know · Lưu ý

- **"Windows protected your PC"**: the app is not code-signed yet. Click **More info → Run anyway**. The full source code is in this repository. · *App chưa ký số: bấm More info → Run anyway.*
- Some antivirus tools may be cautious because RamNap adjusts the memory priority of other processes. · *Một số phần mềm diệt virus có thể cảnh báo vì RamNap điều chỉnh memory priority của tiến trình khác.*
- **Nap may rarely reach busy apps.** Apps that keep using a little CPU, such as browsers and chat apps, may seldom nap. The saving tips work either way. · *App liên tục dùng một ít CPU, như trình duyệt và app chat, có thể hiếm khi được cho ngủ; phần gợi ý tiết kiệm vẫn hoạt động bình thường.*
- Software can't create RAM: if your RAM is often full during normal work, closing apps or adding RAM is still the real fix.

**Full guide in 10 languages:** see the [README](https://github.com/Tamekhanh/RamNap#readme).
