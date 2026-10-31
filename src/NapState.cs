using System.Diagnostics;
using System.Text.Json;

namespace RamNap;

/// <summary>
/// Ghi lại các tiến trình đang bị hạ memory priority. Nếu RamNap bị tắt đột ngột (crash, End task),
/// lần chạy sau sẽ trả chúng về bình thường, nên không app nào bị kẹt ở priority thấp.
/// </summary>
internal static class NapState
{
    private static string FilePath { get; } = Path.Combine(Settings.Folder, "napping.json");

    /// <param name="pids">PID → thời điểm tiến trình khởi động (UTC ticks), để không nhầm PID bị tái sử dụng.</param>
    public static void Save(IEnumerable<KeyValuePair<int, long>> pids)
    {
        try
        {
            var data = pids.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value);
            if (data.Count == 0) { File.Delete(FilePath); return; }
            Directory.CreateDirectory(Settings.Folder);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(data));
        }
        catch (Exception ex) { Log.Write(Lang.T("log.stateSaveError", ex.Message)); }
    }

    public static void RestoreLeftovers()
    {
        if (!File.Exists(FilePath)) return;
        int restored = 0;
        try
        {
            var data = JsonSerializer.Deserialize<Dictionary<string, long>>(File.ReadAllText(FilePath)) ?? [];
            foreach (var (pidText, startTicks) in data)
            {
                if (!int.TryParse(pidText, out int pid)) continue;
                try
                {
                    using var p = Process.GetProcessById(pid);
                    if (StartTicks(p) == startTicks && Native.SetMemoryPriority(pid, Native.MEMORY_PRIORITY_NORMAL))
                        restored++;
                }
                catch { /* tiến trình đã thoát */ }
            }
            File.Delete(FilePath);
        }
        catch (Exception ex) { Log.Write(Lang.T("log.stateRestoreError", ex.Message)); }

        if (restored > 0) Log.Write(Lang.T("log.stateRestored", restored));
    }

    public static long StartTicks(Process p)
    {
        try { return p.StartTime.ToUniversalTime().Ticks; }
        catch { return 0; }
    }
}
