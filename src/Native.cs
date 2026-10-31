using System.Runtime.InteropServices;
using System.Text;

namespace RamNap;

/// <summary>Các hàm Win32 RamNap cần dùng.</summary>
internal static class Native
{
    private const uint PROCESS_SET_QUOTA = 0x0100;
    private const uint PROCESS_SET_INFORMATION = 0x0200;
    private const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

    private const int ProcessMemoryPriority = 0;
    public const uint MEMORY_PRIORITY_LOW = 2;
    public const uint MEMORY_PRIORITY_NORMAL = 5;

    public const uint EVENT_SYSTEM_FOREGROUND = 0x0003;
    public const uint WINEVENT_OUTOFCONTEXT = 0x0000;

    [StructLayout(LayoutKind.Sequential)]
    public struct MemoryStatusEx
    {
        public uint Length;
        public uint MemoryLoad;
        public ulong TotalPhys;
        public ulong AvailPhys;
        public ulong TotalPageFile;
        public ulong AvailPageFile;
        public ulong TotalVirtual;
        public ulong AvailVirtual;
        public ulong AvailExtendedVirtual;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ProcessMemoryCounters
    {
        public uint cb;
        public uint PageFaultCount;
        public nuint PeakWorkingSetSize;
        public nuint WorkingSetSize;
        public nuint QuotaPeakPagedPoolUsage;
        public nuint QuotaPagedPoolUsage;
        public nuint QuotaPeakNonPagedPoolUsage;
        public nuint QuotaNonPagedPoolUsage;
        public nuint PagefileUsage;
        public nuint PeakPagefileUsage;
    }

    public delegate void WinEventProc(IntPtr hook, uint evt, IntPtr hwnd, int idObject, int idChild, uint thread, uint time);
    private delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint processId);
    [DllImport("user32.dll")] private static extern bool EnumChildWindows(IntPtr parent, EnumWindowsProc proc, IntPtr lParam);
    [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowsProc proc, IntPtr lParam);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern IntPtr GetWindow(IntPtr hwnd, uint cmd);
    [DllImport("user32.dll")] public static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmod, WinEventProc proc, uint pid, uint tid, uint flags);
    [DllImport("user32.dll")] public static extern bool UnhookWinEvent(IntPtr hook);
    [DllImport("user32.dll")] public static extern bool DestroyIcon(IntPtr icon);

    [DllImport("kernel32.dll", SetLastError = true)] private static extern IntPtr OpenProcess(uint access, bool inherit, uint pid);
    [DllImport("kernel32.dll")] private static extern bool CloseHandle(IntPtr handle);
    [DllImport("kernel32.dll")] private static extern bool SetProcessInformation(IntPtr process, int infoClass, ref uint info, uint size);
    [DllImport("kernel32.dll")] private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx status);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] private static extern bool QueryFullProcessImageName(IntPtr process, uint flags, StringBuilder name, ref uint size);
    [DllImport("kernel32.dll")] private static extern bool AttachConsole(int pid);
    [DllImport("kernel32.dll")] private static extern IntPtr GetStdHandle(int std);
    [DllImport("kernel32.dll", EntryPoint = "K32EmptyWorkingSet")] private static extern bool EmptyWorkingSet(IntPtr process);
    [DllImport("kernel32.dll", EntryPoint = "K32GetProcessMemoryInfo")] private static extern bool GetProcessMemoryInfo(IntPtr process, out ProcessMemoryCounters counters, uint size);

    public static MemoryStatusEx GetMemoryStatus()
    {
        var status = new MemoryStatusEx { Length = (uint)Marshal.SizeOf<MemoryStatusEx>() };
        GlobalMemoryStatusEx(ref status);
        return status;
    }

    /// <summary>Đặt memory priority. Trang nhớ của tiến trình priority thấp bị Windows thu hồi trước khi thiếu RAM.</summary>
    public static bool SetMemoryPriority(int pid, uint priority)
    {
        var handle = OpenProcess(PROCESS_SET_INFORMATION, false, (uint)pid);
        if (handle == IntPtr.Zero) return false;
        try { return SetProcessInformation(handle, ProcessMemoryPriority, ref priority, sizeof(uint)); }
        finally { CloseHandle(handle); }
    }

    /// <summary>
    /// Chuyển working set của tiến trình sang standby list. Dữ liệu vẫn còn trong RAM làm cache
    /// cho tới khi app khác cần, nên mở lại app vẫn nhanh. Trả về số byte rời khỏi working set.
    /// </summary>
    public static long TrimWorkingSet(int pid, bool dryRun)
    {
        var handle = OpenProcess(PROCESS_SET_QUOTA | PROCESS_QUERY_LIMITED_INFORMATION, false, (uint)pid);
        if (handle == IntPtr.Zero) return 0;
        try
        {
            long before = WorkingSetOf(handle);
            if (dryRun || !EmptyWorkingSet(handle)) return 0;
            return Math.Max(0, before - WorkingSetOf(handle));
        }
        finally { CloseHandle(handle); }
    }

    private static long WorkingSetOf(IntPtr handle)
    {
        uint size = (uint)Marshal.SizeOf<ProcessMemoryCounters>();
        var counters = new ProcessMemoryCounters { cb = size };
        return GetProcessMemoryInfo(handle, out counters, size) ? (long)counters.WorkingSetSize : 0;
    }

    public static string? GetForegroundProcessName() => ProcessNameOfWindow(GetForegroundWindow());

    /// <summary>PID của các tiến trình có cửa sổ chính đang hiện (kể cả thu nhỏ xuống taskbar).</summary>
    public static HashSet<int> GetProcessIdsWithWindow()
    {
        const uint GW_OWNER = 4;
        var pids = new HashSet<int>();
        EnumWindows((hwnd, _) =>
        {
            if (IsWindowVisible(hwnd) && GetWindow(hwnd, GW_OWNER) == IntPtr.Zero)
            {
                GetWindowThreadProcessId(hwnd, out uint pid);
                pids.Add((int)pid);
            }
            return true;
        }, IntPtr.Zero);
        return pids;
    }

    public static string? ProcessNameOfWindow(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero) return null;
        GetWindowThreadProcessId(hwnd, out uint pid);
        string? name = ProcessNameOf(pid);

        // App UWP (Máy tính, Cài đặt...) nằm trong ApplicationFrameHost: tìm tiến trình thật bên trong.
        if (string.Equals(name, "ApplicationFrameHost", StringComparison.OrdinalIgnoreCase))
        {
            uint inner = 0;
            EnumChildWindows(hwnd, (child, _) =>
            {
                GetWindowThreadProcessId(child, out uint childPid);
                if (childPid == pid) return true;
                inner = childPid;
                return false;
            }, IntPtr.Zero);
            if (inner != 0) name = ProcessNameOf(inner) ?? name;
        }
        return name;
    }

    /// <summary>Tên tiến trình giống Process.ProcessName (không có ".exe"), nhưng rẻ hơn nhiều.</summary>
    private static string? ProcessNameOf(uint pid) =>
        ProcessPathOf(pid) is { } path ? Path.GetFileNameWithoutExtension(path) : null;

    /// <summary>Đường dẫn file exe; null nếu tiến trình được bảo vệ hoặc đã thoát.</summary>
    public static string? ProcessPathOf(uint pid)
    {
        var handle = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
        if (handle == IntPtr.Zero) return null;
        try
        {
            var buffer = new StringBuilder(1024);
            uint size = (uint)buffer.Capacity;
            return QueryFullProcessImageName(handle, 0, buffer, ref size) ? buffer.ToString() : null;
        }
        finally { CloseHandle(handle); }
    }

    /// <summary>App dạng WinExe không có console; gắn vào console của cmd/PowerShell khi chạy --scan.</summary>
    public static void AttachParentConsoleIfNeeded()
    {
        const int STD_OUTPUT_HANDLE = -11;
        if (GetStdHandle(STD_OUTPUT_HANDLE) == IntPtr.Zero) AttachConsole(-1);
    }
}
