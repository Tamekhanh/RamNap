namespace RamNap;

internal enum AppCategory { Open, Background, Service, System, Windows }

/// <summary>Một dòng trong danh sách "tất cả những gì đang dùng RAM": một app, một dịch vụ, hoặc một phần bộ nhớ của Windows.</summary>
internal sealed class AppEntry(string key, string name, string title)
{
    public string Key { get; } = key;
    public string Name { get; } = name;
    public string Title { get; } = title;
    public AppCategory Category { get; set; }
    public List<ProcInfo> Procs { get; } = [];
    public List<string> Services { get; } = [];
    public long Private { get; set; }
    public bool HasWindow { get; set; }
    public TimeSpan? Idle { get; set; }
    public bool Napping { get; set; }
    public string Note { get; set; } = "";
}

internal enum AdviceLevel { Recommended, Consider, Info }

internal enum AdviceAction { None, CloseApp, ForceQuit, OpenStartupSettings, OpenUrl }

internal sealed record Advice(
    string Key, AdviceLevel Level, string Title, string Detail, long Savings, bool Estimated,
    AdviceAction Action = AdviceAction.None, string? ActionText = null,
    string? ProcessName = null, string? ExePath = null, string? Url = null);

/// <summary>Gom tiến trình thành từng app và đưa ra gợi ý tiết kiệm RAM. Người dùng luôn là người bấm quyết định.</summary>
internal static class Advisor
{
    private const long MB = 1024 * 1024;

    public static string CategoryName(AppCategory category) => Lang.T(category switch
    {
        AppCategory.Open => "cat.open",
        AppCategory.Background => "cat.background",
        AppCategory.Service => "cat.service",
        AppCategory.System => "cat.system",
        _ => "cat.windows",
    });

    /// <summary>Tiến trình lõi: luôn là "Thành phần của Windows", dù chạy ở session nào.</summary>
    private static readonly HashSet<string> SystemNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "System", "Secure System", "Registry", "Memory Compression", "smss", "csrss", "wininit", "winlogon",
        "services", "lsass", "LsaIso", "fontdrvhost", "dwm",
    };

    /// <summary>Thành phần giao diện Windows chạy trong phiên của bạn nhưng không có cửa sổ.</summary>
    private static readonly HashSet<string> ShellNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "explorer", "sihost", "ctfmon", "RuntimeBroker", "SearchHost", "StartMenuExperienceHost",
        "ShellExperienceHost", "TextInputHost", "LockApp", "ApplicationFrameHost", "conhost", "dllhost",
        "taskhostw", "SecurityHealthSystray", "smartscreen", "UserOOBEBroker", "Widgets", "WidgetService",
        "SearchIndexer", "SearchProtocolHost", "SearchFilterHost", "backgroundTaskHost", "CrossDeviceResume",
        "PhoneExperienceHost", "ShellHost", "WmiPrvSE", "audiodg", "AggregatorHost", "svchost", "msedgewebview2",
    };

    /// <summary>Tiến trình phụ được app gốc bật lại nếu bị tắt riêng: gợi ý và nút thoát phải nhắm vào app gốc.</summary>
    private static readonly Dictionary<string, (string Title, string[] QuitNames)> HelperOf = new(StringComparer.OrdinalIgnoreCase)
    {
        ["steamwebhelper"] = ("Steam", ["steam", "steamwebhelper"]),
        ["EpicWebHelper"] = ("Epic Games Launcher", ["EpicGamesLauncher", "EpicWebHelper"]),
        ["Battle.net Helper"] = ("Battle.net", ["Battle.net", "Battle.net Helper"]),
    };

    /// <summary>
    /// Runtime, cơ sở dữ liệu và máy ảo: một tên có thể gồm nhiều chương trình độc lập (server, script,
    /// dữ liệu đang ghi dở), nên không bao giờ có nút buộc thoát theo tên.
    /// </summary>
    private static readonly HashSet<string> NoForceQuitNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "node", "deno", "bun", "python", "python3", "pythonw", "py", "java", "javaw", "dotnet", "MSBuild",
        "ruby", "php", "php-cgi", "perl", "postgres", "mysqld", "mariadbd", "mongod", "redis-server", "sqlservr",
        "docker", "dockerd", "com.docker.backend", "wsl", "wslhost", "wslrelay", "vmmem", "vmmemWSL",
        "vmware-vmx", "VirtualBoxVM", "VBoxHeadless", "qemu-system-x86_64",
    };

    /// <summary>Tên tiến trình → khóa ghi chú trong file ngôn ngữ.</summary>
    private static readonly Dictionary<string, string> NoteKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        ["System"] = "note.system",
        ["Secure System"] = "note.secureSystem",
        ["Registry"] = "note.registry",
        ["Memory Compression"] = "note.memoryCompression",
        ["dwm"] = "note.dwm",
        ["explorer"] = "note.explorer",
        ["MsMpEng"] = "note.defender",
        ["SearchHost"] = "note.searchHost",
        ["SearchIndexer"] = "note.searchIndexer",
        ["msedgewebview2"] = "note.webview2",
        ["RuntimeBroker"] = "note.runtimeBroker",
        ["audiodg"] = "note.audiodg",
        ["ctfmon"] = "note.ctfmon",
        ["csrss"] = "note.core",
        ["wininit"] = "note.core",
        ["smss"] = "note.core",
        ["winlogon"] = "note.winlogon",
        ["services"] = "note.services",
        ["lsass"] = "note.lsass",
        ["WmiPrvSE"] = "note.wmi",
        ["spoolsv"] = "note.spooler",
        ["dllhost"] = "note.dllhost",
        ["VBCSCompiler"] = "note.vbcscompiler",
        ["node"] = "note.node",
        ["steamwebhelper"] = "note.steamwebhelper",
        ["Discord"] = "note.electronChat",
        ["Slack"] = "note.electronChat",
        ["UniKeyNT"] = "note.vietIme",
        ["EVKey64"] = "note.vietIme",
        ["PredatorSense"] = "note.predatorSense",
        ["RamNap"] = "note.ramnap",
    };

    /// <param name="FeatureKey">Khóa tên tính năng tiết kiệm bộ nhớ trong file ngôn ngữ.</param>
    /// <param name="UserData">Thư mục "User Data" trong %LOCALAPPDATA% (trình duyệt nhân Chromium), để đọc file Local State.</param>
    /// <param name="PathHint">Tên tiến trình quá chung chung (Cốc Cốc là "browser.exe"): chỉ nhận khi đường dẫn exe chứa chuỗi này.</param>
    private sealed record BrowserInfo(string Display, string FeatureKey, string Url, string? UserData, string? PathHint = null, bool BuiltIn = false);

    private static readonly Dictionary<string, BrowserInfo> Browsers = new(StringComparer.OrdinalIgnoreCase)
    {
        ["brave"] = new("Brave", "feature.memorySaver", "brave://settings/system", @"BraveSoftware\Brave-Browser\User Data"),
        ["chrome"] = new("Chrome", "feature.memorySaver", "chrome://settings/performance", @"Google\Chrome\User Data"),
        ["msedge"] = new("Edge", "feature.sleepingTabs", "edge://settings/system", @"Microsoft\Edge\User Data"),
        ["browser"] = new("Cốc Cốc", "feature.memorySaver", "coccoc://settings/performance", @"CocCoc\Browser\User Data", PathHint: "CocCoc"),
        ["vivaldi"] = new("Vivaldi", "feature.memorySaver", "vivaldi://settings/performance", @"Vivaldi\User Data"),
        ["opera"] = new("Opera", "feature.tabSnoozing", "opera://settings", null),
        ["firefox"] = new("Firefox", "", "", null, BuiltIn: true),
    };

    private static readonly Dictionary<string, (DateTime WriteTime, bool? Enabled)> memorySaverCache = [];

    public static List<AppEntry> BuildEntries(
        IReadOnlyList<ProcInfo> procs, IReadOnlyDictionary<int, List<ServiceInfo>> services,
        HashSet<int> withWindow, int mySession, Func<string, (TimeSpan? Idle, bool Napping)> usage)
    {
        var entries = new Dictionary<string, AppEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in procs)
        {
            if (p.Pid == 0) continue; // "Idle" không phải tiến trình thật

            string key = p.Name, title = p.Name;
            // Mỗi svchost là một dịch vụ khác nhau: tách riêng và gọi đúng tên dịch vụ.
            if (p.Name.Equals("svchost", StringComparison.OrdinalIgnoreCase)
                && services.TryGetValue(p.Pid, out var svcs) && svcs.Count > 0)
            {
                key = $"svchost#{p.Pid}";
                title = svcs.Count > 1 ? Lang.T("svchost.more", svcs[0].DisplayName, svcs.Count - 1) : svcs[0].DisplayName;
            }

            if (!entries.TryGetValue(key, out var e)) entries[key] = e = new AppEntry(key, p.Name, title);
            e.Procs.Add(p);
            if (services.TryGetValue(p.Pid, out var list))
                e.Services.AddRange(list.Select(s => s.DisplayName == s.Name ? s.Name : $"{s.DisplayName} ({s.Name})"));
        }

        foreach (var e in entries.Values)
        {
            // Kho nén là working set của tiến trình Memory Compression, không phải private working set.
            e.Private = e.Name.Equals("Memory Compression", StringComparison.OrdinalIgnoreCase)
                ? e.Procs.Sum(p => p.WorkingSet)
                : e.Procs.Sum(p => p.PrivateWorkingSet);
            e.HasWindow = e.Procs.Any(p => withWindow.Contains(p.Pid));
            e.Category = Categorize(e, mySession);
            e.Note = NoteKeys.TryGetValue(e.Name, out var noteKey) ? Lang.T(noteKey) : DefaultNote(e);
            // App chạy ngầm không có cửa sổ để "dùng", nên chỉ tính thời gian không dùng cho app đang mở.
            if (e.Category == AppCategory.Open) (e.Idle, e.Napping) = usage(e.Name);
        }

        return entries.Values.OrderBy(e => e.Category).ThenByDescending(e => e.Private).ToList();
    }

    /// <summary>Thêm các phần RAM không thuộc tiến trình nào: nhân Windows, driver, trang dùng chung.</summary>
    public static void AddWindowsMemory(List<AppEntry> entries, MemoryOverview mem)
    {
        long processes = entries.Sum(e => e.Private);
        long rest = Math.Max(0, mem.Used - processes - mem.KernelNonpaged - mem.KernelPaged);
        Add("kernel-nonpaged", "kernel.nonpaged", mem.KernelNonpaged);
        Add("kernel-paged", "kernel.paged", mem.KernelPaged);
        Add("rest", "kernel.rest", rest);

        void Add(string key, string textKey, long bytes)
        {
            string title = Lang.T(textKey + ".title");
            entries.Add(new AppEntry(key, title, title) { Category = AppCategory.Windows, Private = bytes, Note = Lang.T(textKey + ".note") });
        }
    }

    /// <param name="exclusions">App bạn đã loại trừ thì không gợi ý đóng, thoát hay bỏ tự khởi động.</param>
    public static List<Advice> BuildAdvice(List<AppEntry> entries, MemoryOverview mem, HashSet<string> startupNames, IReadOnlySet<string> exclusions)
    {
        var advice = new List<Advice>();
        foreach (var e in entries)
        {
            if (e.Category is not (AppCategory.Open or AppCategory.Background)) continue;
            if (e.Name.Equals("RamNap", StringComparison.OrdinalIgnoreCase) || ShellNames.Contains(e.Name)) continue;

            string size = Format(e.Private);
            string? exePath = Browsers.ContainsKey(e.Name) ? Native.ProcessPathOf((uint)e.Procs[0].Pid) : null;
            if (Browsers.TryGetValue(e.Name, out var browser)
                && (browser.PathHint == null || exePath?.Contains(browser.PathHint, StringComparison.OrdinalIgnoreCase) == true))
            {
                if (e.Private >= 1024 * MB) advice.Add(BrowserAdvice(e, browser, exePath));
                continue;
            }

            if (exclusions.Contains(e.Name)) continue;

            if (e.Category == AppCategory.Open && e.Idle >= TimeSpan.FromMinutes(30) && e.Private >= 150 * MB)
            {
                advice.Add(new Advice($"idle:{e.Name}", AdviceLevel.Consider,
                    Lang.T("adv.idle.title", e.Title, (int)e.Idle.Value.TotalMinutes, size),
                    Lang.T("adv.idle.detail", e.Title, size),
                    e.Private, Estimated: false, AdviceAction.CloseApp, Lang.T("adv.close", e.Title), e.Name));
            }
            else if (e.Category == AppCategory.Background && e.Private >= 150 * MB && NoForceQuitNames.Contains(e.Name))
            {
                advice.Add(new Advice($"bg:{e.Name}", AdviceLevel.Consider,
                    Lang.T("adv.bg.title", e.Title, size),
                    Lang.T("adv.bg.detailManual", e.Title, size),
                    e.Private, Estimated: false));
            }
            else if (e.Category == AppCategory.Background && e.Private >= 150 * MB)
            {
                var (app, quitNames) = HelperOf.TryGetValue(e.Name, out var helper) ? helper : (e.Title, [e.Name]);
                string part = app == e.Title ? "" : Lang.T("adv.bg.helper", e.Title, app);
                advice.Add(new Advice($"bg:{e.Name}", AdviceLevel.Consider,
                    Lang.T("adv.bg.title", app, size),
                    Lang.T("adv.bg.detail", app, size, part),
                    e.Private, Estimated: false, AdviceAction.ForceQuit, Lang.T("adv.forceQuit", app), string.Join("|", quitNames)));
            }

            if (startupNames.Contains(e.Name) && e.Private >= 100 * MB)
            {
                advice.Add(new Advice($"startup:{e.Name}", AdviceLevel.Consider,
                    Lang.T("adv.startup.title", e.Title, size),
                    Lang.T("adv.startup.detail", e.Title, size),
                    e.Private, Estimated: false, AdviceAction.OpenStartupSettings, Lang.T("adv.startup.button"), e.Name));
            }
        }

        var compression = entries.FirstOrDefault(e => e.Name.Equals("Memory Compression", StringComparison.OrdinalIgnoreCase));
        if (compression != null && compression.Private >= 200 * MB)
        {
            advice.Add(new Advice("info:compression", AdviceLevel.Info,
                Lang.T("adv.compression.title", Format(compression.Private)), Lang.T("adv.compression.detail"),
                0, Estimated: false));
        }

        if (mem.SystemCache >= 512 * MB)
        {
            advice.Add(new Advice("info:cache", AdviceLevel.Info,
                Lang.T("adv.cache.title", Format(mem.SystemCache)), Lang.T("adv.cache.detail"),
                0, Estimated: false));
        }

        if (mem.Load >= 85)
        {
            advice.Add(new Advice("info:upgrade", AdviceLevel.Info,
                Lang.T("adv.upgrade.title", mem.Load), Lang.T("adv.upgrade.detail", Format(mem.Total)),
                0, Estimated: false));
        }

        return advice
            .OrderBy(a => a.Level)
            .ThenByDescending(a => a.Savings)
            .ToList();
    }

    private static Advice BrowserAdvice(AppEntry e, BrowserInfo browser, string? exePath)
    {
        string size = Format(e.Private);
        string key = $"browser:{e.Name}";
        if (browser.BuiltIn)
        {
            return new Advice(key, AdviceLevel.Info,
                Lang.T("adv.browser.builtin.title", browser.Display, size),
                Lang.T("adv.browser.builtin.detail", browser.Display),
                0, Estimated: false);
        }

        string feature = Lang.T(browser.FeatureKey);

        bool canOpen = exePath != null && browser.Url.Length > 0;
        if (IsMemorySaverOn(browser) == true)
        {
            return new Advice(key, AdviceLevel.Info,
                Lang.T("adv.browser.on.title", browser.Display, size),
                Lang.T("adv.browser.on.detail", feature, size),
                0, Estimated: false,
                canOpen ? AdviceAction.OpenUrl : AdviceAction.None, canOpen ? Lang.T("adv.openSettings", browser.Display) : null,
                e.Name, exePath, browser.Url);
        }

        return new Advice(key, AdviceLevel.Recommended,
            Lang.T("adv.browser.off.title", browser.Display, size),
            Lang.T("adv.browser.off.detail", browser.Display, size, feature) + (canOpen ? Lang.T("adv.browser.off.detailButton") : ""),
            e.Private * 3 / 10, Estimated: true,
            canOpen ? AdviceAction.OpenUrl : AdviceAction.None, canOpen ? Lang.T("adv.openSettings", browser.Display) : null,
            e.Name, exePath, browser.Url);
    }

    /// <summary>
    /// Đọc trạng thái Memory Saver trong file Local State của trình duyệt nhân Chromium:
    /// performance_tuning.high_efficiency_mode.state khác 0 là đang bật. Chưa từng bật thì khóa
    /// này chưa có (mặc định tắt). Trả về null khi không đọc được. Chỉ đọc lại khi file thay đổi.
    /// </summary>
    private static bool? IsMemorySaverOn(BrowserInfo browser)
    {
        if (browser.UserData == null) return null;
        string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), browser.UserData, "Local State");
        try
        {
            var writeTime = File.GetLastWriteTimeUtc(path);
            if (memorySaverCache.TryGetValue(path, out var cached) && cached.WriteTime == writeTime) return cached.Enabled;

            bool? enabled = null;
            if (File.Exists(path))
            {
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                using var doc = System.Text.Json.JsonDocument.Parse(stream);
                enabled = doc.RootElement.TryGetProperty("performance_tuning", out var tuning)
                          && tuning.TryGetProperty("high_efficiency_mode", out var mode)
                          && mode.TryGetProperty("state", out var state)
                          && state.TryGetInt32(out int value)
                          && value != 0;
            }
            memorySaverCache[path] = (writeTime, enabled);
            return enabled;
        }
        catch { return null; } // file đang được ghi dở hoặc định dạng lạ: coi như không biết
    }

    private static AppCategory Categorize(AppEntry e, int mySession)
    {
        if (SystemNames.Contains(e.Name)) return AppCategory.System;
        if (e.Name.Equals("svchost", StringComparison.OrdinalIgnoreCase) || e.Procs.Any(p => p.SessionId != mySession))
            return AppCategory.Service;
        if (e.HasWindow) return ShellNames.Contains(e.Name) && !e.Name.Equals("explorer", StringComparison.OrdinalIgnoreCase)
            ? AppCategory.System : AppCategory.Open;
        return ShellNames.Contains(e.Name) ? AppCategory.System : AppCategory.Background;
    }

    private static string DefaultNote(AppEntry e) => e.Category switch
    {
        AppCategory.Service when e.Services.Count > 0 => Lang.T("note.default.serviceWithServices"),
        AppCategory.Service => Lang.T("note.default.service"),
        AppCategory.Background => Lang.T("note.default.background"),
        AppCategory.System => Lang.T("note.default.system"),
        _ => "",
    };

    public static string Format(long bytes) =>
        bytes >= 1024 * MB ? $"{bytes / (1024.0 * MB):0.0} {Lang.T("unit.gb")}" : $"{bytes / MB} {Lang.T("unit.mb")}";
}
