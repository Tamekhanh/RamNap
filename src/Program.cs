namespace RamNap;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        Lang.Load(Settings.Load().Language);
        if (args.Contains("--scan", StringComparer.OrdinalIgnoreCase)) return Scan.Run();

        using var mutex = new Mutex(true, @"Local\RamNap.SingleInstance", out bool isFirst);
        if (!isFirst)
        {
            MessageBox.Show(Lang.T("app.alreadyRunning"), "RamNap");
            return 0;
        }

        ApplicationConfiguration.Initialize();
        // Mở bằng tay thì hiện cửa sổ; tự chạy cùng Windows (--tray) thì chỉ nằm im ở khay.
        Application.Run(new TrayApp(showWindow: !args.Contains("--tray", StringComparer.OrdinalIgnoreCase)));
        return 0;
    }
}
