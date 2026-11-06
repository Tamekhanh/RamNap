using System.Diagnostics;

namespace RamNap;

/// <summary>
/// Theo dõi các app theo tên tiến trình (Chrome có nhiều tiến trình thì gom chung một nhóm).
/// App có cửa sổ mà bạn không dùng và gần như không tốn CPU suốt <see cref="Settings.IdleMinutes"/> phút
/// thì được "cho ngủ": hạ memory priority để Windows lấy lại RAM từ nó trước, thay vì từ app
/// bạn đang dùng. Đẩy working set của app ngủ ra (mỗi lượt một app) là tùy chọn, mặc định tắt:
/// xem <see cref="Settings.TrimEnabled"/>. Không tắt, không treo, không dừng app nào.
/// </summary>
internal sealed class Optimizer
{
    private sealed class AppGroup(string name, DateTime now)
    {
        public string Name { get; } = name;
        public DateTime LastActive { get; set; } = now;
        public DateTime LastTrimmed { get; set; } = DateTime.MinValue;
        public Dictionary<int, TimeSpan> CpuByPid { get; set; } = [];
        /// <summary>Chỉ chứa PID đã thật sự bị hạ priority: PID → thời điểm khởi động.</summary>
        public Dictionary<int, long> NappedPids { get; } = [];
        /// <summary>PID không mở được (app admin, anti-cheat): không thử lại mỗi lượt quét.</summary>
        public HashSet<int> DeniedPids { get; } = [];
        public bool Napping { get; set; }
        public long WorkingSet { get; set; }
    }

    private readonly Dictionary<string, AppGroup> groups = new(StringComparer.OrdinalIgnoreCase);
    private readonly int sessionId = Process.GetCurrentProcess().SessionId;
    private readonly int selfPid = Environment.ProcessId;
    private Settings settings;
    private HashSet<string> exclusions;
    private HashSet<string> trayAllowed;
    private DateTime lastTick = DateTime.UtcNow;
    private bool paused;
    private bool stateDirty;

    public Optimizer(Settings settings)
    {
        this.settings = settings;
        exclusions = settings.ExclusionSet();
        trayAllowed = settings.TrayAllowedSet();
    }

    public uint MemoryLoad { get; private set; }
    public int NappingCount => groups.Values.Count(g => g.Napping);

    /// <summary>Các app RamNap không được đụng tới: danh sách có sẵn cộng với Exclusions của bạn.</summary>
    public IReadOnlySet<string> Exclusions => exclusions;

    public bool Paused
    {
        get => paused;
        set
        {
            paused = value;
            if (paused) WakeAll(Lang.T("reason.paused"));
            else foreach (var g in groups.Values) g.LastActive = DateTime.UtcNow;
        }
    }

    public void ApplySettings(Settings newSettings)
    {
        // Đánh thức hết trước khi đổi cài đặt (nhất là DryRun): app nào vẫn rảnh sẽ được cho ngủ lại ở lượt sau.
        WakeAll(Lang.T("reason.settingsChanged"));
        settings = newSettings;
        exclusions = newSettings.ExclusionSet();
        trayAllowed = newSettings.TrayAllowedSet();
        foreach (var name in groups.Keys.Where(exclusions.Contains).ToList()) groups.Remove(name);
    }

    /// <summary>Bao lâu app này không được dùng (tính từ khi RamNap chạy) và có đang ngủ không.</summary>
    public (TimeSpan? Idle, bool Napping) Usage(string name) =>
        groups.TryGetValue(name, out var g) ? (DateTime.UtcNow - g.LastActive, g.Napping) : (null, false);

    public void Tick()
    {
        var now = DateTime.UtcNow;
        double elapsedMs = Math.Max(1, (now - lastTick).TotalMilliseconds);
        lastTick = now;
        MemoryLoad = Native.GetMemoryStatus().MemoryLoad;
        if (paused) return;

        // Máy vừa thức dậy từ Sleep/Hibernate: thời gian "không dùng" lúc máy ngủ không tính.
        if (elapsedMs > Math.Max(60_000, settings.IntervalSeconds * 6_000))
        {
            foreach (var group in groups.Values) group.LastActive = now;
            Log.Write(Lang.T("log.resumed"));
        }

        string? foreground = Native.GetForegroundProcessName();
        var withWindow = settings.RequireWindow ? Native.GetProcessIdsWithWindow() : null;
        var idleSpan = TimeSpan.FromMinutes(settings.IdleMinutes);
        var snapshot = SnapshotByName();

        foreach (var (name, procs) in snapshot)
        {
            if (!groups.TryGetValue(name, out var g)) groups[name] = g = new AppGroup(name, now);

            var cpuNow = new Dictionary<int, TimeSpan>(procs.Count);
            double cpuDeltaMs = 0;
            long workingSet = 0;
            foreach (var p in procs)
            {
                workingSet += p.WorkingSet;
                cpuNow[p.Pid] = p.CpuTime;
                if (g.CpuByPid.TryGetValue(p.Pid, out var previous))
                    cpuDeltaMs += Math.Max(0, (p.CpuTime - previous).TotalMilliseconds);
            }

            // Lượt đầu tiên chưa có mẫu CPU để so sánh thì coi như đang hoạt động: không biết chắc thì không đụng.
            bool noCpuSample = g.CpuByPid.Count == 0;
            g.CpuByPid = cpuNow;
            g.WorkingSet = workingSet;

            // App chỉ chạy ngầm (bộ gõ, hook phím, công cụ khay...) không được cho ngủ, trừ khi bạn cho phép.
            bool eligible = withWindow == null || trayAllowed.Contains(name) || procs.Any(p => withWindow.Contains(p.Pid));

            double cpuPercent = cpuDeltaMs / elapsedMs * 100;
            if (!eligible || noCpuSample || cpuPercent >= settings.CpuActivePercent
                || string.Equals(name, foreground, StringComparison.OrdinalIgnoreCase))
                g.LastActive = now;

            if (now - g.LastActive >= idleSpan) Nap(g, procs);
            else if (g.Napping) Wake(g, Lang.T(eligible ? "reason.active" : "reason.noWindow"));
        }

        foreach (var name in groups.Keys.Where(k => !snapshot.ContainsKey(k)).ToList())
            groups.Remove(name);

        if (settings.TrimEnabled && MemoryLoad >= settings.PressurePercent) TrimOne(snapshot, now);
        SaveStateIfDirty();
    }

    /// <summary>Người dùng vừa chuyển sang cửa sổ của app này: đánh thức ngay, không đợi lượt quét sau.</summary>
    public void OnForeground(string name)
    {
        if (!groups.TryGetValue(name, out var g)) return;
        g.LastActive = DateTime.UtcNow;
        if (g.Napping) Wake(g, Lang.T("reason.reopened"));
        SaveStateIfDirty();
    }

    public void WakeAll(string reason)
    {
        foreach (var g in groups.Values.Where(g => g.Napping)) Wake(g, reason);
        SaveStateIfDirty();
    }

    private void Nap(AppGroup g, List<ProcInfo> procs)
    {
        var alive = procs.Select(p => p.Pid).ToHashSet();
        foreach (var pid in g.NappedPids.Keys.Where(pid => !alive.Contains(pid)).ToList())
        {
            g.NappedPids.Remove(pid);
            stateDirty = true;
        }
        g.DeniedPids.IntersectWith(alive);

        if (!settings.DryRun)
        {
            foreach (var p in procs)
            {
                if (g.NappedPids.ContainsKey(p.Pid) || g.DeniedPids.Contains(p.Pid)) continue;
                if (Native.SetMemoryPriority(p.Pid, Native.MEMORY_PRIORITY_LOW))
                {
                    g.NappedPids[p.Pid] = p.StartTimeUtc.Ticks;
                    stateDirty = true;
                }
                else g.DeniedPids.Add(p.Pid);
            }
        }

        if (!g.Napping && (settings.DryRun || g.NappedPids.Count > 0))
        {
            g.Napping = true;
            Log.Write(Lang.T("log.nap", g.Name, procs.Count, Mb(g.WorkingSet), DryTag));
        }
    }

    private void Wake(AppGroup g, string reason)
    {
        // Luôn khôi phục, kể cả ở chế độ thử: NappedPids chỉ chứa PID đã thật sự bị hạ priority.
        foreach (var pid in g.NappedPids.Keys) Native.SetMemoryPriority(pid, Native.MEMORY_PRIORITY_NORMAL);
        if (g.NappedPids.Count > 0) stateDirty = true;
        g.NappedPids.Clear();
        g.Napping = false;
        Log.Write(Lang.T("log.wake", g.Name, reason, DryTag));
    }

    /// <summary>
    /// Mỗi lượt quét chỉ giải phóng một app (app ngủ to nhất chưa bị đụng gần đây). Trang bẩn cần
    /// thời gian để được nén hoặc ghi ra, nên lượt sau đo lại RAM rồi mới quyết định có làm tiếp không.
    /// </summary>
    private void TrimOne(Dictionary<string, List<ProcInfo>> snapshot, DateTime now)
    {
        var cooldown = TimeSpan.FromSeconds(settings.TrimCooldownSeconds);
        var g = groups.Values
            .Where(g => g.Napping && (settings.DryRun || g.NappedPids.Count > 0) && now - g.LastTrimmed >= cooldown)
            .OrderByDescending(g => g.WorkingSet)
            .FirstOrDefault();
        if (g == null || !snapshot.TryGetValue(g.Name, out var procs)) return;

        g.LastTrimmed = now;
        if (settings.DryRun)
        {
            Log.Write(Lang.T("log.wouldTrim", g.Name, Mb(g.WorkingSet), MemoryLoad, DryTag));
            return;
        }

        long moved = procs.Where(p => g.NappedPids.ContainsKey(p.Pid))
                          .Sum(p => Native.TrimWorkingSet(p.Pid, dryRun: false));
        Log.Write(Lang.T("log.trim", g.Name, Mb(moved), MemoryLoad));
    }

    private void SaveStateIfDirty()
    {
        if (!stateDirty) return;
        stateDirty = false;
        NapState.Save(groups.Values.SelectMany(g => g.NappedPids));
    }

    private Dictionary<string, List<ProcInfo>> SnapshotByName()
    {
        var result = new Dictionary<string, List<ProcInfo>>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in SystemSnapshot.Processes())
        {
            if (p.Pid == selfPid || p.SessionId != sessionId || exclusions.Contains(p.Name)) continue;
            if (!result.TryGetValue(p.Name, out var list)) result[p.Name] = list = [];
            list.Add(p);
        }
        return result;
    }

    private string DryTag => settings.DryRun ? Lang.T("log.dryTag") : "";

    public static long Mb(long bytes) => bytes / (1024 * 1024);
}
