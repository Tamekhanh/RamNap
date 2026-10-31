using System.Diagnostics;

namespace RamNap;

/// <summary>
/// Cửa sổ "RAM của bạn đang đi đâu". Chỉ được tạo khi bạn mở và bị hủy khi đóng, nên lúc
/// chạy nền RamNap không phải giữ giao diện trong RAM.
/// </summary>
internal sealed class MainForm : Form
{
    private const int RefreshMs = 5000;
    private static readonly TimeSpan SlowRefresh = TimeSpan.FromSeconds(30);

    private readonly Optimizer optimizer;
    private readonly Action<string> changeLanguage;
    private readonly int mySession = Process.GetCurrentProcess().SessionId;
    private readonly Label summary = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
    private readonly MemoryBar bar = new() { Dock = DockStyle.Top };
    private readonly Label adviceHeader = new() { Dock = DockStyle.Top, TextAlign = ContentAlignment.MiddleLeft };
    private readonly BufferedListView adviceList = new();
    private readonly Label adviceDetail = new() { Dock = DockStyle.Fill };
    private readonly Button actionButton = new() { Dock = DockStyle.Top, Visible = false };
    private readonly BufferedListView appList = new();
    private readonly TextBox details = new()
    {
        Dock = DockStyle.Bottom, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical,
        BorderStyle = BorderStyle.None, BackColor = SystemColors.Window,
    };
    private readonly System.Windows.Forms.Timer refreshTimer = new() { Interval = RefreshMs };
    private readonly Dictionary<string, ListViewItem> appItems = [];

    // Danh sách dịch vụ và mục khởi động thay đổi chậm: đọc 30 giây một lần cho đỡ tốn.
    private Dictionary<int, List<ServiceInfo>> services = [];
    private HashSet<string> startupNames = [];
    private DateTime slowDataAt = DateTime.MinValue;
    private List<AppEntry> entries = [];
    private string appKeys = "";

    /// <param name="changeLanguage">Đổi ngôn ngữ toàn app (TrayApp lưu cài đặt, dựng lại menu và mở lại cửa sổ này).</param>
    public MainForm(Optimizer optimizer, Action<string> changeLanguage)
    {
        this.optimizer = optimizer;
        this.changeLanguage = changeLanguage;
        SuspendLayout();
        Text = Lang.T("ui.title");
        Icon = AppIcon.Create();
        Font = Lang.UiFont(9f);
        StartPosition = FormStartPosition.CenterScreen;
        Size = new Size(S(1040), S(700));
        MinimumSize = new Size(S(760), S(500));

        var refresh = new Button { Text = Lang.T("ui.refresh"), Dock = DockStyle.Right, Width = S(110) };
        refresh.Click += (_, _) => RefreshData(force: true);
        var top = new Panel { Dock = DockStyle.Top, Height = S(40), Padding = new Padding(S(10), S(6), S(10), S(4)) };
        top.Controls.Add(summary);
        top.Controls.Add(refresh);
        top.Controls.Add(new Panel { Dock = DockStyle.Right, Width = S(8) });
        top.Controls.Add(CreateLanguageButton());
        bar.Height = S(46);
        bar.Padding = new Padding(S(10), 0, S(10), 0);

        // Tab 1: gợi ý
        SetupList(adviceList, (Lang.T("ui.colLevel"), 110), (Lang.T("ui.colAdvice"), 560), (Lang.T("ui.colSavings"), 130));
        adviceList.SelectedIndexChanged += (_, _) => ShowAdvice();
        adviceList.DoubleClick += (_, _) => RunAction();
        adviceHeader.Height = S(30);
        adviceHeader.Font = new Font(Font, FontStyle.Bold);
        actionButton.Height = S(34);
        actionButton.Click += (_, _) => RunAction();
        var actionPanel = new Panel { Dock = DockStyle.Right, Width = S(230), Padding = new Padding(S(10), S(4), 0, 0) };
        actionPanel.Controls.Add(actionButton);
        var adviceBottom = new Panel { Dock = DockStyle.Bottom, Height = S(104), Padding = new Padding(S(6)) };
        adviceBottom.Controls.Add(adviceDetail);
        adviceBottom.Controls.Add(actionPanel);
        var adviceTab = new TabPage(Lang.T("ui.tabAdvice")) { Padding = new Padding(S(6)) };
        adviceTab.Controls.Add(adviceList);
        adviceTab.Controls.Add(adviceBottom);
        adviceTab.Controls.Add(adviceHeader);
        adviceList.BringToFront();

        // Tab 2: tất cả
        SetupList(appList, (Lang.T("ui.colApp"), 300), (Lang.T("ui.colProcs"), 85), (Lang.T("ui.colRam"), 85),
            (Lang.T("ui.colIdle"), 100), (Lang.T("ui.colNote"), 440));
        appList.ShowGroups = true;
        appList.SelectedIndexChanged += (_, _) => ShowDetails();
        details.Height = S(130);
        var footnote = new Label
        {
            Dock = DockStyle.Bottom, Height = S(22), ForeColor = SystemColors.GrayText, TextAlign = ContentAlignment.MiddleLeft,
            Text = Lang.T("ui.footnote"),
        };
        var appsTab = new TabPage(Lang.T("ui.tabAll")) { Padding = new Padding(S(6)) };
        appsTab.Controls.Add(appList);
        appsTab.Controls.Add(details);
        appsTab.Controls.Add(footnote);
        appList.BringToFront();

        var tabs = new TabControl { Dock = DockStyle.Fill };
        tabs.TabPages.Add(adviceTab);
        tabs.TabPages.Add(appsTab);
        Controls.Add(tabs);
        Controls.Add(bar);
        Controls.Add(top);
        tabs.BringToFront();
        ResumeLayout(true);

        refreshTimer.Tick += (_, _) => { if (WindowState != FormWindowState.Minimized) RefreshData(force: false); };
        Shown += (_, _) => { RefreshData(force: true); refreshTimer.Start(); };
        FormClosed += (_, _) => refreshTimer.Stop();
    }

    private int S(int px) => (int)Math.Round(px * DeviceDpi / 96f);

    /// <summary>Nút "🌐 Tên ngôn ngữ ▾" ở góc trên bên phải; bấm vào thả xuống danh sách ngôn ngữ.</summary>
    private Button CreateLanguageButton()
    {
        string current = Settings.Load().Language ?? "auto";
        string activeName = Lang.Available.First(l => l.Code == Lang.Code).Name;

        var button = new Button
        {
            Dock = DockStyle.Right,
            Width = S(170),
            Text = $"{activeName}  ▾",
            Image = AppIcon.Globe(S(16), SystemColors.ControlText),
            ImageAlign = ContentAlignment.MiddleLeft,
            TextAlign = ContentAlignment.MiddleRight,
            TextImageRelation = TextImageRelation.ImageBeforeText,
            Padding = new Padding(S(6), 0, S(6), 0),
        };
        var tip = new ToolTip();
        tip.SetToolTip(button, Lang.T("tray.language"));

        var menu = new ContextMenuStrip { Font = Font };
        Disposed += (_, _) => { tip.Dispose(); menu.Dispose(); };
        menu.Items.Add(Choice("auto", Lang.T("tray.languageAuto")));
        menu.Items.Add(new ToolStripSeparator());
        foreach (var (code, name) in Lang.Available) menu.Items.Add(Choice(code, name));
        button.Click += (_, _) => menu.Show(button, new Point(0, button.Height));
        return button;

        ToolStripMenuItem Choice(string code, string name) => new(name, null, (_, _) =>
        {
            // Đổi ngôn ngữ sẽ đóng cửa sổ này: đợi menu đóng xong rồi mới làm.
            SynchronizationContext.Current?.Post(_ => changeLanguage(code), null);
        })
        {
            Checked = string.Equals(code, current, StringComparison.OrdinalIgnoreCase),
        };
    }

    private void SetupList(ListView list, params (string Text, int Width)[] columns)
    {
        list.Dock = DockStyle.Fill;
        list.View = View.Details;
        list.FullRowSelect = true;
        list.MultiSelect = false;
        list.HideSelection = false;
        foreach (var (text, width) in columns) list.Columns.Add(text, S(width));
    }

    private void RefreshData(bool force)
    {
        try
        {
            if (force || DateTime.UtcNow - slowDataAt >= SlowRefresh)
            {
                services = SystemSnapshot.ServicesByPid();
                startupNames = Startup.EnabledNames();
                slowDataAt = DateTime.UtcNow;
            }

            var mem = SystemSnapshot.Overview();
            entries = Advisor.BuildEntries(SystemSnapshot.Processes(), services,
                Native.GetProcessIdsWithWindow(), mySession, optimizer.Usage);
            Advisor.AddWindowsMemory(entries, mem);

            UpdateSummary(mem);
            UpdateAdvice(Advisor.BuildAdvice(entries, mem, startupNames));
            UpdateApps(force);
        }
        catch (Exception ex)
        {
            summary.Text = Lang.T("ui.readError", ex.Message);
            Log.Write(Lang.T("log.windowError", ex));
        }
    }

    private void UpdateSummary(MemoryOverview mem)
    {
        long Sum(params AppCategory[] categories) => entries.Where(e => categories.Contains(e.Category)).Sum(e => e.Private);
        long open = Sum(AppCategory.Open), background = Sum(AppCategory.Background);
        long hidden = Sum(AppCategory.Service, AppCategory.System);
        long windows = Math.Max(0, mem.Used - open - background - hidden);

        summary.Text = Lang.T("ui.summary", Advisor.Format(mem.Used), Advisor.Format(mem.Total), mem.Load,
            Advisor.Format(mem.Available), optimizer.NappingCount);
        bar.SetSegments(mem.Total,
        [
            new(open, Color.FromArgb(37, 99, 235), Lang.T("ui.segOpen")),
            new(background, Color.FromArgb(234, 88, 12), Lang.T("ui.segBackground")),
            new(hidden, Color.FromArgb(147, 51, 234), Lang.T("ui.segHidden")),
            new(windows, Color.FromArgb(100, 116, 139), Lang.T("ui.segShared")),
            new(mem.Available, Color.FromArgb(220, 252, 231), Lang.T("ui.segFree")),
        ]);
    }

    private void UpdateAdvice(List<Advice> advice)
    {
        string? selected = (adviceList.SelectedItems.Count > 0 ? adviceList.SelectedItems[0].Tag as Advice : null)?.Key;
        long savings = advice.Sum(a => a.Savings);
        int actionable = advice.Count(a => a.Level != AdviceLevel.Info);
        adviceHeader.Text = actionable == 0
            ? Lang.T("ui.noAdvice")
            : Lang.T("ui.adviceHeader", actionable, Advisor.Format(savings));

        adviceList.BeginUpdate();
        adviceList.Items.Clear();
        foreach (var a in advice)
        {
            var (level, color) = a.Level switch
            {
                AdviceLevel.Recommended => (Lang.T("ui.levelRecommended"), Color.FromArgb(21, 128, 61)),
                AdviceLevel.Consider => (Lang.T("ui.levelConsider"), Color.FromArgb(180, 83, 9)),
                _ => (Lang.T("ui.levelInfo"), SystemColors.GrayText),
            };
            string amount = a.Savings > 0 ? (a.Estimated ? "~" : "") + Advisor.Format(a.Savings) : "—";
            var item = new ListViewItem([level, a.Title, amount]) { Tag = a, ForeColor = color, UseItemStyleForSubItems = false };
            item.SubItems[1].ForeColor = SystemColors.WindowText;
            adviceList.Items.Add(item);
            if (a.Key == selected) item.Selected = true;
        }
        if (adviceList.SelectedItems.Count == 0 && adviceList.Items.Count > 0) adviceList.Items[0].Selected = true;
        adviceList.EndUpdate();
        ShowAdvice();
    }

    private void ShowAdvice()
    {
        var a = adviceList.SelectedItems.Count > 0 ? adviceList.SelectedItems[0].Tag as Advice : null;
        adviceDetail.Text = a?.Detail ?? "";
        actionButton.Visible = a?.ActionText != null;
        actionButton.Text = a?.ActionText ?? "";
    }

    private void UpdateApps(bool force)
    {
        string keys = string.Join("|", entries.Select(e => e.Key));
        if (!force && keys == appKeys)
        {
            // Cùng danh sách: chỉ cập nhật số liệu tại chỗ, giữ nguyên vị trí cuộn và lựa chọn.
            foreach (var e in entries)
                if (appItems.TryGetValue(e.Key, out var item)) FillRow(item, e);
            UpdateGroupHeaders();
            return;
        }

        appKeys = keys;
        string? selected = appList.SelectedItems.Count > 0 ? (appList.SelectedItems[0].Tag as AppEntry)?.Key : null;
        string? topKey = (appList.TopItem?.Tag as AppEntry)?.Key;

        appList.BeginUpdate();
        appList.Items.Clear();
        appList.Groups.Clear();
        appItems.Clear();
        var groups = new Dictionary<AppCategory, ListViewGroup>();
        foreach (var e in entries)
        {
            if (!groups.TryGetValue(e.Category, out var group))
                appList.Groups.Add(groups[e.Category] = group = new ListViewGroup(e.Category.ToString(), ""));
            var item = new ListViewItem(["", "", "", "", ""], group) { Tag = e };
            FillRow(item, e);
            appList.Items.Add(item);
            appItems[e.Key] = item;
            if (e.Key == selected) item.Selected = true;
        }
        UpdateGroupHeaders();
        appList.EndUpdate();
        if (topKey != null && appItems.TryGetValue(topKey, out var top)) appList.TopItem = top;
    }

    private static void FillRow(ListViewItem item, AppEntry e)
    {
        item.Tag = e;
        item.SubItems[0].Text = e.Title;
        item.SubItems[1].Text = e.Procs.Count > 0 ? e.Procs.Count.ToString() : "";
        item.SubItems[2].Text = Advisor.Format(e.Private);
        item.SubItems[3].Text = e.Napping ? Lang.T("ui.napping")
            : e.Idle is { } idle ? (idle.TotalMinutes < 1 ? Lang.T("ui.inUse") : Lang.T("ui.minutes", (int)idle.TotalMinutes)) : "";
        item.SubItems[4].Text = e.Note;
    }

    private void UpdateGroupHeaders()
    {
        foreach (ListViewGroup group in appList.Groups)
        {
            var category = Enum.Parse<AppCategory>(group.Name!);
            var inGroup = entries.Where(e => e.Category == category).ToList();
            group.Header = Lang.T("ui.groupHeader", Advisor.CategoryName(category), Advisor.Format(inGroup.Sum(e => e.Private)), inGroup.Count);
        }
    }

    private void ShowDetails()
    {
        if (appList.SelectedItems.Count == 0 || appList.SelectedItems[0].Tag is not AppEntry e)
        {
            details.Text = "";
            return;
        }

        var lines = new List<string>
        {
            $"{e.Title} — {Advisor.Format(e.Private)} · {Advisor.CategoryName(e.Category)}",
            e.Note,
        };
        if (e.Procs.Count > 0 && Native.ProcessPathOf((uint)e.Procs[0].Pid) is { } path) lines.Add(Lang.T("ui.detailFile", path));
        if (e.Services.Count > 0) lines.Add(Lang.T("ui.detailServices", string.Join(", ", e.Services)));
        foreach (var p in e.Procs.OrderByDescending(p => p.PrivateWorkingSet).Take(40))
        {
            string started = p.StartTimeUtc > DateTime.MinValue ? p.StartTimeUtc.ToLocalTime().ToString("HH:mm dd/MM") : "?";
            lines.Add(Lang.T("ui.detailProc", p.Pid, Advisor.Format(p.PrivateWorkingSet), started));
        }
        if (e.Procs.Count > 40) lines.Add(Lang.T("ui.detailMore", e.Procs.Count - 40));
        details.Text = string.Join(Environment.NewLine, lines);
    }

    private async void RunAction()
    {
        if (adviceList.SelectedItems.Count == 0 || adviceList.SelectedItems[0].Tag is not Advice a || a.ActionText == null) return;
        try
        {
            switch (a.Action)
            {
                case AdviceAction.CloseApp:
                    int closed = 0;
                    foreach (var p in Process.GetProcessesByName(a.ProcessName!))
                        using (p) if (p.MainWindowHandle != IntPtr.Zero && p.CloseMainWindow()) closed++;
                    if (closed == 0)
                        MessageBox.Show(this, Lang.T("ui.noWindowToClose"), "RamNap");
                    break;

                case AdviceAction.ForceQuit:
                    var answer = MessageBox.Show(this, Lang.T("ui.forceQuitConfirm", a.ActionText), "RamNap", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                    if (answer != DialogResult.Yes) return;
                    // App gốc trước, tiến trình phụ sau, để app gốc không kịp bật lại tiến trình phụ.
                    foreach (var name in a.ProcessName!.Split('|'))
                        foreach (var p in Process.GetProcessesByName(name))
                            using (p) try { p.Kill(); } catch { /* đã thoát hoặc không có quyền */ }
                    break;

                case AdviceAction.OpenStartupSettings:
                    Process.Start(new ProcessStartInfo("ms-settings:startupapps") { UseShellExecute = true })?.Dispose();
                    break;

                case AdviceAction.OpenUrl when a.ExePath != null && a.Url != null:
                    Process.Start(new ProcessStartInfo(a.ExePath, a.Url) { UseShellExecute = false })?.Dispose();
                    break;
            }
            Log.Write(Lang.T("log.userAction", a.ActionText));
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, Lang.T("ui.actionFailed", ex.Message), "RamNap", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        await Task.Delay(2000);
        if (!IsDisposed) RefreshData(force: true);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) refreshTimer.Dispose();
        base.Dispose(disposing);
    }

    private sealed class BufferedListView : ListView
    {
        public BufferedListView() => DoubleBuffered = true;
    }
}

/// <summary>Thanh RAM chia màu theo nhóm, kèm chú thích bên dưới.</summary>
internal sealed class MemoryBar : Control
{
    public sealed record Segment(long Bytes, Color Color, string Label);

    private Segment[] segments = [];
    private long total = 1;

    public MemoryBar()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
    }

    public void SetSegments(long totalBytes, Segment[] newSegments)
    {
        total = Math.Max(1, totalBytes);
        segments = newSegments;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        var area = new Rectangle(Padding.Left, 2, Width - Padding.Horizontal, Height / 2 - 4);
        float x = area.Left;
        foreach (var s in segments)
        {
            float w = area.Width * (float)(s.Bytes / (double)total);
            using var brush = new SolidBrush(s.Color);
            g.FillRectangle(brush, x, area.Top, w, area.Height);
            x += w;
        }
        g.DrawRectangle(SystemPens.ControlDark, area);

        // Chú thích: ô màu + tên + dung lượng
        float lx = area.Left, ly = area.Bottom + 6;
        int box = Font.Height - 4;
        foreach (var s in segments)
        {
            string text = $"{s.Label} {Advisor.Format(s.Bytes)}";
            using var brush = new SolidBrush(s.Color);
            g.FillRectangle(brush, lx, ly + 2, box, box);
            g.DrawRectangle(SystemPens.ControlDark, lx, ly + 2, box, box);
            TextRenderer.DrawText(g, text, Font, new Point((int)(lx + box + 4), (int)ly), ForeColor, TextFormatFlags.NoPrefix);
            lx += box + 4 + TextRenderer.MeasureText(text, Font, Size.Empty, TextFormatFlags.NoPrefix).Width + 14;
        }
    }
}
