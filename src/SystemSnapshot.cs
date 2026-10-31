using System.Runtime.InteropServices;

namespace RamNap;

internal sealed record ProcInfo(int Pid, int ParentPid, string Name, int SessionId,
    long PrivateWorkingSet, long WorkingSet, TimeSpan CpuTime, DateTime StartTimeUtc);

internal sealed record ServiceInfo(string Name, string DisplayName);

internal sealed record MemoryOverview(long Total, long Available, uint Load, long SystemCache,
    long KernelPaged, long KernelNonpaged, long CommitTotal, long CommitLimit)
{
    public long Used => Total - Available;
}

/// <summary>
/// Đọc toàn bộ tiến trình của máy (kể cả dịch vụ chạy ngầm ở session 0) bằng một lần gọi
/// NtQuerySystemInformation: không cần mở handle tới từng tiến trình, không cần quyền admin.
/// </summary>
internal static class SystemSnapshot
{
    private const int SystemProcessInformation = 5;
    private const int STATUS_INFO_LENGTH_MISMATCH = unchecked((int)0xC0000004);

    [DllImport("ntdll.dll")]
    private static extern int NtQuerySystemInformation(int infoClass, IntPtr buffer, int length, out int returnLength);

    [StructLayout(LayoutKind.Sequential)]
    private struct PerformanceInformation
    {
        public uint cb;
        public nuint CommitTotal, CommitLimit, CommitPeak, PhysicalTotal, PhysicalAvailable,
                     SystemCache, KernelTotal, KernelPaged, KernelNonpaged, PageSize;
        public uint HandleCount, ProcessCount, ThreadCount;
    }

    [DllImport("kernel32.dll", EntryPoint = "K32GetPerformanceInfo")]
    private static extern bool GetPerformanceInfo(out PerformanceInformation info, uint size);

    [StructLayout(LayoutKind.Sequential)]
    private struct EnumServiceStatusProcess
    {
        public IntPtr ServiceName;
        public IntPtr DisplayName;
        public uint ServiceType, CurrentState, ControlsAccepted, Win32ExitCode,
                    ServiceSpecificExitCode, CheckPoint, WaitHint, ProcessId, ServiceFlags;
    }

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr OpenSCManager(string? machine, string? database, uint access);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool EnumServicesStatusEx(IntPtr scm, int infoLevel, uint serviceType, uint serviceState,
        IntPtr buffer, uint bufferSize, out uint bytesNeeded, out uint servicesReturned, ref uint resumeHandle, string? group);

    [DllImport("advapi32.dll")]
    private static extern bool CloseServiceHandle(IntPtr handle);

    public static List<ProcInfo> Processes()
    {
        if (IntPtr.Size != 8) throw new PlatformNotSupportedException("RamNap requires 64-bit Windows.");

        int size = 1 << 20;
        while (true)
        {
            IntPtr buffer = Marshal.AllocHGlobal(size);
            try
            {
                int status = NtQuerySystemInformation(SystemProcessInformation, buffer, size, out int needed);
                if (status == STATUS_INFO_LENGTH_MISMATCH)
                {
                    size = Math.Max(size * 2, needed + 64 * 1024);
                    continue;
                }
                if (status != 0) throw new InvalidOperationException($"NtQuerySystemInformation failed: 0x{status:X8}");
                return Parse(buffer);
            }
            finally { Marshal.FreeHGlobal(buffer); }
        }
    }

    /// <summary>Đọc từng SYSTEM_PROCESS_INFORMATION theo offset của Windows 64-bit.</summary>
    private static List<ProcInfo> Parse(IntPtr buffer)
    {
        var list = new List<ProcInfo>(512);
        IntPtr entry = buffer;
        while (true)
        {
            int next = Marshal.ReadInt32(entry, 0);
            long privateWs = Marshal.ReadInt64(entry, 8);
            long createTime = Marshal.ReadInt64(entry, 32);
            long userTime = Marshal.ReadInt64(entry, 40);
            long kernelTime = Marshal.ReadInt64(entry, 48);
            int nameBytes = (ushort)Marshal.ReadInt16(entry, 56);
            IntPtr namePtr = Marshal.ReadIntPtr(entry, 64);
            int pid = (int)Marshal.ReadIntPtr(entry, 80);
            int parentPid = (int)Marshal.ReadIntPtr(entry, 88);
            int sessionId = Marshal.ReadInt32(entry, 100);
            long workingSet = (long)Marshal.ReadIntPtr(entry, 144);

            string name = namePtr == IntPtr.Zero
                ? (pid == 0 ? "Idle" : "?")
                : Marshal.PtrToStringUni(namePtr, nameBytes / 2);
            if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) name = name[..^4];

            list.Add(new ProcInfo(pid, parentPid, name, sessionId, privateWs, workingSet,
                TimeSpan.FromTicks(userTime + kernelTime),
                createTime > 0 ? DateTime.FromFileTimeUtc(createTime) : DateTime.MinValue));

            if (next == 0) break;
            entry += next;
        }
        return list;
    }

    /// <summary>PID → các dịch vụ Windows đang chạy trong tiến trình đó (svchost chứa nhiều dịch vụ).</summary>
    public static Dictionary<int, List<ServiceInfo>> ServicesByPid()
    {
        const uint SC_MANAGER_ENUMERATE_SERVICE = 0x0004;
        const uint SERVICE_WIN32 = 0x30, SERVICE_USER_SERVICES = 0xC0, SERVICE_ACTIVE = 1;

        var result = new Dictionary<int, List<ServiceInfo>>();
        IntPtr scm = OpenSCManager(null, null, SC_MANAGER_ENUMERATE_SERVICE);
        if (scm == IntPtr.Zero) return result;
        try
        {
            // Gồm cả dịch vụ theo từng người dùng (thông báo, clipboard...). Windows cũ không nhận thì lùi về SERVICE_WIN32.
            foreach (uint type in new[] { SERVICE_WIN32 | SERVICE_USER_SERVICES, SERVICE_WIN32 })
            {
                uint resume = 0;
                EnumServicesStatusEx(scm, 0, type, SERVICE_ACTIVE, IntPtr.Zero, 0, out uint needed, out _, ref resume, null);
                if (needed == 0) continue;

                uint size = needed + 8192;
                IntPtr buffer = Marshal.AllocHGlobal((int)size);
                try
                {
                    resume = 0;
                    if (!EnumServicesStatusEx(scm, 0, type, SERVICE_ACTIVE, buffer, size, out _, out uint count, ref resume, null))
                        continue;

                    int stride = Marshal.SizeOf<EnumServiceStatusProcess>();
                    for (int i = 0; i < count; i++)
                    {
                        var s = Marshal.PtrToStructure<EnumServiceStatusProcess>(buffer + i * stride);
                        if (s.ProcessId == 0) continue;
                        if (!result.TryGetValue((int)s.ProcessId, out var services))
                            result[(int)s.ProcessId] = services = [];
                        services.Add(new ServiceInfo(
                            Marshal.PtrToStringUni(s.ServiceName) ?? "?",
                            Marshal.PtrToStringUni(s.DisplayName) ?? "?"));
                    }
                    return result;
                }
                finally { Marshal.FreeHGlobal(buffer); }
            }
            return result;
        }
        finally { CloseServiceHandle(scm); }
    }

    public static MemoryOverview Overview()
    {
        var status = Native.GetMemoryStatus();
        var info = new PerformanceInformation { cb = (uint)Marshal.SizeOf<PerformanceInformation>() };
        GetPerformanceInfo(out info, info.cb);
        long page = (long)info.PageSize;
        return new MemoryOverview(
            (long)status.TotalPhys, (long)status.AvailPhys, status.MemoryLoad,
            (long)info.SystemCache * page, (long)info.KernelPaged * page, (long)info.KernelNonpaged * page,
            (long)info.CommitTotal * page, (long)info.CommitLimit * page);
    }
}
