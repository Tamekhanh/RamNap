namespace RamNap;

/// <summary>Nhật ký mọi thay đổi RamNap thực hiện, để bạn luôn biết nó đã làm gì.</summary>
internal static class Log
{
    private const long MaxBytes = 512 * 1024;

    public static string FilePath { get; } = Path.Combine(Settings.Folder, "ramnap.log");

    public static void Write(string message)
    {
        try
        {
            Directory.CreateDirectory(Settings.Folder);
            var info = new FileInfo(FilePath);
            if (info.Exists && info.Length > MaxBytes) File.Move(FilePath, FilePath + ".old", overwrite: true);
            File.AppendAllText(FilePath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}  {message}{Environment.NewLine}");
        }
        catch { /* ghi nhật ký không được thì bỏ qua, không làm sập app */ }
    }
}
