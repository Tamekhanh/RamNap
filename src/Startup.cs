using Microsoft.Win32;

namespace RamNap;

/// <summary>Đọc danh sách app đang được bật tự chạy cùng Windows (giống tab Startup của Task Manager).</summary>
internal static class Startup
{
    private const string Run = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string Run32 = @"Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Run";
    private const string Approved = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\";

    /// <summary>Tên exe và tên mục khởi động (vd. "Discord", "Steam") đang được bật.</summary>
    public static HashSet<string> EnabledNames()
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        ReadRunKey(Registry.CurrentUser, Run, "Run", names);
        ReadRunKey(Registry.LocalMachine, Run, "Run", names);
        ReadRunKey(Registry.LocalMachine, Run32, "Run32", names);
        ReadStartupFolder(Registry.CurrentUser, Environment.GetFolderPath(Environment.SpecialFolder.Startup), names);
        ReadStartupFolder(Registry.LocalMachine, Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup), names);
        return names;
    }

    private static void ReadRunKey(RegistryKey root, string path, string approvedName, HashSet<string> names)
    {
        try
        {
            using var run = root.OpenSubKey(path);
            if (run == null) return;
            using var approved = root.OpenSubKey(Approved + approvedName);
            foreach (var valueName in run.GetValueNames())
            {
                if (IsDisabled(approved, valueName)) continue;
                names.Add(valueName);
                if (ExeName(run.GetValue(valueName) as string) is { } exe) names.Add(exe);
            }
        }
        catch { /* không đọc được thì bỏ qua */ }
    }

    private static void ReadStartupFolder(RegistryKey root, string folder, HashSet<string> names)
    {
        try
        {
            if (!Directory.Exists(folder)) return;
            using var approved = root.OpenSubKey(Approved + "StartupFolder");
            foreach (var file in Directory.EnumerateFiles(folder))
            {
                if (IsDisabled(approved, Path.GetFileName(file))) continue;
                names.Add(Path.GetFileNameWithoutExtension(file));
            }
        }
        catch { }
    }

    /// <summary>Task Manager đánh dấu mục bị tắt bằng byte đầu tiên là số lẻ trong StartupApproved.</summary>
    private static bool IsDisabled(RegistryKey? approved, string name) =>
        approved?.GetValue(name) is byte[] { Length: > 0 } data && (data[0] & 1) == 1;

    private static string? ExeName(string? command)
    {
        if (string.IsNullOrWhiteSpace(command)) return null;
        command = Environment.ExpandEnvironmentVariables(command.Trim());
        int end = command.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
        if (end < 0) return null;
        return Path.GetFileNameWithoutExtension(command[..(end + 4)].Trim('"'));
    }
}
