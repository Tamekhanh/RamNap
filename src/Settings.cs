using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RamNap;

/// <summary>Cài đặt, lưu ở %APPDATA%\RamNap\settings.json.</summary>
internal sealed class Settings
{
    /// <summary>
    /// Luôn được loại trừ, dù settings.json ghi gì. Ngoài tiến trình hệ thống còn có các tiến trình
    /// nền không bao giờ lên foreground nhưng cần phản hồi tức thì: dịch vụ thông báo/clipboard
    /// (svchost), WebView2 của Start/Search, bộ gõ tiếng Việt, hook bàn phím, trình quản lý mật khẩu.
    /// </summary>
    private static readonly string[] BuiltInExclusions =
    [
        "System", "Idle", "Registry", "smss", "csrss", "wininit", "winlogon", "services", "lsass", "svchost",
        "dwm", "explorer", "sihost", "ctfmon", "fontdrvhost", "audiodg", "ApplicationFrameHost",
        "ShellExperienceHost", "StartMenuExperienceHost", "SearchHost", "TextInputHost", "msedgewebview2",
        "LockApp", "RuntimeBroker", "MsMpEng", "SecurityHealthSystray", "Taskmgr", "conhost",
        "UniKey", "UniKeyNT", "EVKey", "EVKey64", "OpenKey", "AutoHotkey", "AutoHotkey64", "AutoHotkeyU64",
        "PowerToys", "PowerToys.KeyboardManagerEngine", "KeePass", "KeePassXC", "1Password", "Bitwarden",
        "Spotify", "vlc", "foobar2000", "MusicBee", "AIMP", "PotPlayerMini64", "mpc-hc64", "obs64",
        "RamNap",
    ];

    /// <summary>"auto" = theo ngôn ngữ Windows, hoặc vi, en, zh, ja, ko, fr, es, pt, id, ru.</summary>
    public string? Language { get; set; } = "auto";

    /// <summary>App không được bạn dùng và không tốn CPU trong bấy nhiêu phút thì được cho ngủ.</summary>
    public int IdleMinutes { get; set; } = 10;

    /// <summary>App dùng từ ngần này % của 1 nhân CPU trở lên được coi là đang làm việc (phát nhạc, tải file...).</summary>
    public double CpuActivePercent { get; set; } = 2.0;

    /// <summary>
    /// Đẩy working set của app đang ngủ ra khi RAM căng. Mặc định tắt: chưa có benchmark nào chứng
    /// minh việc này có lợi, còn app bị đẩy ra thì phải nạp lại dữ liệu khi được dùng tiếp.
    /// </summary>
    public bool TrimEnabled { get; set; }

    /// <summary>Khi <see cref="TrimEnabled"/> bật: chỉ giải phóng RAM của app đang ngủ khi RAM sử dụng đạt mức % này.</summary>
    public int PressurePercent { get; set; } = 80;

    /// <summary>Khoảng nghỉ tối thiểu giữa hai lần giải phóng RAM của cùng một app.</summary>
    public int TrimCooldownSeconds { get; set; } = 300;

    public int IntervalSeconds { get; set; } = 5;

    /// <summary>Chế độ thử: chỉ ghi nhật ký, không thay đổi gì trên máy.</summary>
    public bool DryRun { get; set; }

    /// <summary>
    /// Chỉ cho ngủ app có cửa sổ (kể cả đang thu nhỏ). App chỉ chạy ngầm ở khay thường là
    /// bộ gõ, hook phím, công cụ hệ thống: không đụng tới, trừ khi có trong <see cref="TrayAppsAllowed"/>.
    /// </summary>
    public bool RequireWindow { get; set; } = true;

    /// <summary>App chạy ở khay nhưng ngốn RAM, được phép cho ngủ dù không có cửa sổ.</summary>
    public List<string>? TrayAppsAllowed { get; set; } =
        ["Discord", "Slack", "Telegram", "Zalo", "steam", "steamwebhelper", "EpicGamesLauncher", "Battle.net"];

    /// <summary>Tên tiến trình (không cần ".exe") bạn muốn loại trừ thêm, cộng với danh sách có sẵn.</summary>
    public List<string>? Exclusions { get; set; } = [];

    /// <summary>
    /// Khác null nghĩa là settings.json đang lỗi và đây là cài đặt an toàn thay thế (chế độ thử).
    /// Khi đó không được ghi đè file, vì file đó có thể chứa cài đặt của bạn.
    /// </summary>
    [JsonIgnore]
    public string? LoadError { get; private init; }

    /// <summary>
    /// Bỏ qua dấu phẩy thừa và ghi chú vì chúng không làm đổi ý nghĩa. Khóa lạ thì báo lỗi: gõ sai
    /// "DryRun" mà lờ đi thì app sẽ âm thầm chạy thật.
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    public static string Folder { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RamNap");

    public static string FilePath { get; } = Path.Combine(Folder, "settings.json");

    public HashSet<string> ExclusionSet() => NameSet(BuiltInExclusions.Concat(Exclusions ?? []));

    public HashSet<string> TrayAllowedSet() => NameSet(TrayAppsAllowed ?? []);

    private static HashSet<string> NameSet(IEnumerable<string?> names) =>
        new(names.Where(n => !string.IsNullOrWhiteSpace(n))
                 .Select(n => n!.Trim())
                 .Select(n => n.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? n[..^4] : n),
            StringComparer.OrdinalIgnoreCase);

    /// <summary>Đọc settings.json. File lỗi thì trả về cài đặt an toàn có <see cref="LoadError"/>, không bao giờ âm thầm dùng mặc định.</summary>
    public static Settings Load()
    {
        Settings settings;
        try
        {
            settings = File.Exists(FilePath)
                ? JsonSerializer.Deserialize<Settings>(File.ReadAllText(FilePath), JsonOptions)
                    ?? throw new JsonException("The file contains null.")
                : new Settings();
        }
        catch (Exception ex)
        {
            Log.Write(Lang.T("log.settingsReadError", ex.Message));
            return new Settings { DryRun = true, LoadError = ex.Message };
        }

        settings.IdleMinutes = Math.Clamp(settings.IdleMinutes, 1, 24 * 60);
        settings.CpuActivePercent = Math.Clamp(settings.CpuActivePercent, 0.1, 100);
        settings.PressurePercent = Math.Clamp(settings.PressurePercent, 30, 99);
        settings.TrimCooldownSeconds = Math.Clamp(settings.TrimCooldownSeconds, 10, 3600);
        settings.IntervalSeconds = Math.Clamp(settings.IntervalSeconds, 1, 60);
        if (!File.Exists(FilePath)) settings.Save();
        return settings;
    }

    public void Save()
    {
        if (LoadError != null) return;
        try
        {
            Directory.CreateDirectory(Folder);
            WriteAtomic(FilePath, JsonSerializer.Serialize(this, JsonOptions));
        }
        catch (Exception ex) { Log.Write(Lang.T("log.settingsSaveError", ex.Message)); }
    }

    /// <summary>Ghi ra file tạm rồi đổi tên, để mất điện hay crash giữa chừng không để lại file ghi dở.</summary>
    public static void WriteAtomic(string path, string text)
    {
        string temp = path + ".tmp";
        using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            stream.Write(new UTF8Encoding(false).GetBytes(text));
            stream.Flush(flushToDisk: true);
        }
        File.Move(temp, path, overwrite: true);
    }
}
