using System.Diagnostics;
using System.Text;

namespace RamNap;

/// <summary>"RamNap.exe --scan": in ra tình trạng RAM và tự kiểm tra, không thay đổi app nào.</summary>
internal static class Scan
{
    public static int Run()
    {
        Native.AttachParentConsoleIfNeeded();
        Console.OutputEncoding = Encoding.UTF8;

        var mem = Native.GetMemoryStatus();
        Console.WriteLine(Lang.T("scan.ram", mem.MemoryLoad, Optimizer.Mb((long)mem.AvailPhys), Optimizer.Mb((long)mem.TotalPhys)));
        Console.WriteLine(Lang.T("scan.foreground", Native.GetForegroundProcessName() ?? Lang.T("scan.unknown")));

        var exclusions = Settings.Load().ExclusionSet();
        int session = Process.GetCurrentProcess().SessionId;
        var rows = SystemSnapshot.Processes()
            .Where(p => p.SessionId == session)
            .GroupBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .Select(g => (Name: g.Key, Count: g.Count(), Private: g.Sum(p => p.PrivateWorkingSet)))
            .OrderByDescending(r => r.Private)
            .Take(15);

        Console.WriteLine();
        Console.WriteLine(Lang.T("scan.top"));
        foreach (var (name, count, bytes) in rows)
            Console.WriteLine(Lang.T("scan.row", name, count, Optimizer.Mb(bytes), exclusions.Contains(name) ? Lang.T("scan.excluded") : ""));

        Console.WriteLine();
        int self = Environment.ProcessId;
        bool low = Native.SetMemoryPriority(self, Native.MEMORY_PRIORITY_LOW);
        bool normal = Native.SetMemoryPriority(self, Native.MEMORY_PRIORITY_NORMAL);
        long trimmed = Native.TrimWorkingSet(self, dryRun: false);
        Console.WriteLine(Lang.T("scan.selftest",
            Lang.T(low && normal ? "scan.ok" : "scan.error"),
            Lang.T(trimmed > 0 ? "scan.ok" : "scan.unchanged"),
            Optimizer.Mb(trimmed)));
        Console.WriteLine(Lang.T("scan.settings", Settings.FilePath));
        return low && normal ? 0 : 1;
    }
}
