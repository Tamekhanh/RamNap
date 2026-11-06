using System.Diagnostics;
using Microsoft.Win32;

namespace RamNap;

/// <summary>Biểu tượng ở khay hệ thống và vòng quét định kỳ.</summary>
internal sealed class TrayApp : ApplicationContext
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

    private readonly Optimizer optimizer;
    private readonly NotifyIcon tray;
    private readonly System.Windows.Forms.Timer timer = new();
    private readonly Native.WinEventProc foregroundProc; // giữ tham chiếu để GC không thu hồi callback
    private readonly IntPtr foregroundHook;
    private ToolStripMenuItem statusItem = new();
    private ToolStripMenuItem pauseItem = new();
    private MainForm? window;

    public TrayApp(bool showWindow)
    {
        NapState.RestoreLeftovers();
        var settings = Settings.Load();
        optimizer = new Optimizer(settings);

        tray = new NotifyIcon { Icon = AppIcon.Create(), Text = "RamNap", Visible = true };
        tray.DoubleClick += (_, _) => ShowWindow();
        BuildMenu();
        if (settings.LoadError != null)
            tray.ShowBalloonTip(8000, "RamNap", Lang.T("tray.settingsSafeMode", Lang.T("tray.reloadSettings")), ToolTipIcon.Warning);

        foregroundProc = OnForegroundChanged;
        foregroundHook = Native.SetWinEventHook(Native.EVENT_SYSTEM_FOREGROUND, Native.EVENT_SYSTEM_FOREGROUND,
            IntPtr.Zero, foregroundProc, 0, 0, Native.WINEVENT_OUTOFCONTEXT);

        timer.Interval = settings.IntervalSeconds * 1000;
        timer.Tick += (_, _) => OnTick();
        timer.Start();

        Log.Write(Lang.T(settings.DryRun ? "log.startedDry" : "log.started"));
        OnTick();

        // Bản thân RamNap cũng nhường RAM: priority thấp, và bỏ các trang chỉ dùng lúc khởi động.
        Native.SetMemoryPriority(Environment.ProcessId, Native.MEMORY_PRIORITY_LOW);
        Native.TrimWorkingSet(Environment.ProcessId, dryRun: false);

        if (showWindow) ShowWindow();
    }

    /// <summary>Dựng menu khay theo ngôn ngữ hiện tại (gọi lại khi đổi ngôn ngữ).</summary>
    private void BuildMenu()
    {
        statusItem = new ToolStripMenuItem { Enabled = false };
        pauseItem = new ToolStripMenuItem(Lang.T("tray.pause")) { Checked = optimizer.Paused };
        pauseItem.Click += (_, _) =>
        {
            optimizer.Paused = !optimizer.Paused;
            pauseItem.Checked = optimizer.Paused;
            UpdateStatus();
        };

        var startupItem = new ToolStripMenuItem(Lang.T("tray.startup")) { Checked = IsStartupEnabled() };
        startupItem.Click += (_, _) =>
        {
            SetStartup(!startupItem.Checked);
            startupItem.Checked = IsStartupEnabled();
        };

        string current = Settings.Load().Language ?? "auto";
        var languageItem = new ToolStripMenuItem(Lang.T("tray.language"));
        languageItem.DropDownItems.Add(LanguageChoice("auto", Lang.T("tray.languageAuto"), current));
        languageItem.DropDownItems.Add(new ToolStripSeparator());
        foreach (var (code, name) in Lang.Available) languageItem.DropDownItems.Add(LanguageChoice(code, name, current));

        var menu = new ContextMenuStrip { Font = Lang.UiFont(9f) };
        menu.Items.AddRange(new ToolStripItem[]
        {
            new ToolStripMenuItem(Lang.T("tray.open"), null, (_, _) => ShowWindow()) { Font = Lang.UiFont(9f, FontStyle.Bold) },
            statusItem,
            new ToolStripSeparator(),
            pauseItem,
            languageItem,
            new ToolStripSeparator(),
            new ToolStripMenuItem(Lang.T("tray.openSettings"), null, (_, _) => OpenFile(Settings.FilePath)),
            new ToolStripMenuItem(Lang.T("tray.reloadSettings"), null, (_, _) => ReloadSettings()),
            new ToolStripMenuItem(Lang.T("tray.openLog"), null, (_, _) => OpenFile(Log.FilePath)),
            startupItem,
            new ToolStripSeparator(),
            new ToolStripMenuItem(Lang.T("tray.exit"), null, (_, _) => ExitThread()),
        });

        var old = tray.ContextMenuStrip;
        tray.ContextMenuStrip = menu;
        old?.Dispose();
        UpdateStatus();
    }

    private ToolStripMenuItem LanguageChoice(string code, string name, string current) =>
        new(name, null, (_, _) => ChangeLanguage(code))
        {
            Checked = string.Equals(code, current, StringComparison.OrdinalIgnoreCase),
        };

    private void ChangeLanguage(string code)
    {
        var settings = Settings.Load();
        settings.Language = code;
        settings.Save();
        Lang.Load(code);
        BuildMenu();
        if (settings.LoadError != null)
            tray.ShowBalloonTip(8000, "RamNap", Lang.T("tray.settingsNotSaved"), ToolTipIcon.Warning);

        // Cửa sổ đang mở thì dựng lại bằng ngôn ngữ mới.
        if (window != null)
        {
            window.Close();
            ShowWindow();
        }
    }

    private void ShowWindow()
    {
        if (window == null)
        {
            window = new MainForm(optimizer, ChangeLanguage);
            window.FormClosed += (_, _) =>
            {
                window = null; // form không modal tự Dispose sau khi đóng
                ReleaseOwnMemory();
            };
            window.Show();
        }
        if (window.WindowState == FormWindowState.Minimized) window.WindowState = FormWindowState.Normal;
        window.Activate();
    }

    /// <summary>Đóng cửa sổ xong thì trả lại RAM của giao diện, để RamNap chạy nền nhẹ như trước.</summary>
    private static void ReleaseOwnMemory()
    {
        GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
        Native.TrimWorkingSet(Environment.ProcessId, dryRun: false);
    }

    private void OnTick()
    {
        try { optimizer.Tick(); }
        catch (Exception ex) { Log.Write(Lang.T("log.scanError", ex.Message)); }
        UpdateStatus();
    }

    private void OnForegroundChanged(IntPtr hook, uint evt, IntPtr hwnd, int idObject, int idChild, uint thread, uint time)
    {
        try
        {
            var name = Native.ProcessNameOfWindow(hwnd);
            if (name != null) optimizer.OnForeground(name);
        }
        catch (Exception ex) { Log.Write(Lang.T("log.wakeError", ex.Message)); }
    }

    private void UpdateStatus()
    {
        string status = optimizer.Paused
            ? Lang.T("tray.statusPaused", optimizer.MemoryLoad)
            : Lang.T("tray.status", optimizer.MemoryLoad, optimizer.NappingCount);
        statusItem.Text = status;

        string tip = $"RamNap\n{status}";
        tray.Text = tip.Length > 127 ? tip[..127] : tip;
    }

    private void ReloadSettings()
    {
        var settings = Settings.Load();
        if (settings.LoadError != null)
        {
            // Giữ nguyên cài đặt đang dùng: lỗi gõ trong file không được làm mất DryRun hay Exclusions.
            tray.ShowBalloonTip(8000, "RamNap", Lang.T("tray.settingsInvalid", Lang.T("tray.reloadSettings")), ToolTipIcon.Warning);
            return;
        }

        optimizer.ApplySettings(settings);
        timer.Interval = settings.IntervalSeconds * 1000;
        if (!string.Equals(Lang.Code, settings.Language, StringComparison.OrdinalIgnoreCase))
        {
            Lang.Load(settings.Language);
            BuildMenu();
        }
        Log.Write(Lang.T("log.settingsReloaded"));
        tray.ShowBalloonTip(2000, "RamNap", Lang.T("tray.settingsReloaded"), ToolTipIcon.Info);
    }

    private static void OpenFile(string path)
    {
        if (!File.Exists(path))
        {
            if (path == Settings.FilePath) Settings.Load();
            else Log.Write(Lang.T("log.logCreated"));
        }
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true })?.Dispose();
    }

    private static bool IsStartupEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey);
        return key?.GetValue("RamNap") is string;
    }

    private static void SetStartup(bool enable)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKey);
        if (enable) key.SetValue("RamNap", $"\"{Environment.ProcessPath}\" --tray");
        else key.DeleteValue("RamNap", throwOnMissingValue: false);
    }

    protected override void ExitThreadCore()
    {
        timer.Stop();
        window?.Close();
        if (foregroundHook != IntPtr.Zero) Native.UnhookWinEvent(foregroundHook);
        optimizer.WakeAll(Lang.T("reason.exit"));
        Log.Write(Lang.T("log.exited"));
        tray.Visible = false;
        tray.ContextMenuStrip?.Dispose();
        tray.Dispose();
        base.ExitThreadCore();
    }
}
