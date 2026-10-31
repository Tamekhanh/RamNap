using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace RamNap;

/// <summary>
/// Bản dịch giao diện. Mỗi ngôn ngữ là một file JSON nhúng trong exe (thư mục lang\), nhưng chỉ
/// ngôn ngữ đang dùng được nạp vào RAM. Tiếng Anh chỉ được nạp thêm khi thiếu chuỗi nào đó.
/// </summary>
internal static class Lang
{
    public static readonly (string Code, string Name)[] Available =
    [
        ("vi", "Tiếng Việt"),
        ("en", "English"),
        ("zh", "简体中文"),
        ("ja", "日本語"),
        ("ko", "한국어"),
        ("fr", "Français"),
        ("es", "Español"),
        ("pt", "Português"),
        ("id", "Bahasa Indonesia"),
        ("ru", "Русский"),
    ];

    private static Dictionary<string, string> strings = [];
    private static Dictionary<string, string>? english;

    public static string Code { get; private set; } = "en";

    [DllImport("kernel32.dll")]
    private static extern ushort GetUserDefaultUILanguage();

    /// <param name="setting">"auto" (theo ngôn ngữ Windows) hoặc một mã trong <see cref="Available"/>.</param>
    public static void Load(string? setting)
    {
        string code = string.IsNullOrWhiteSpace(setting) || setting.Equals("auto", StringComparison.OrdinalIgnoreCase)
            ? Detect()
            : setting.Trim().ToLowerInvariant();
        if (!Available.Any(l => l.Code == code)) code = "en";

        Code = code;
        strings = Read(code) ?? [];
        english = code == "en" ? strings : null;
    }

    /// <summary>Ngôn ngữ giao diện của Windows (RamNap chạy ở chế độ invariant culture nên phải hỏi thẳng Windows).</summary>
    public static string Detect() => (GetUserDefaultUILanguage() & 0x3FF) switch
    {
        0x2A => "vi",
        0x04 => "zh",
        0x11 => "ja",
        0x12 => "ko",
        0x0C => "fr",
        0x0A => "es",
        0x16 => "pt",
        0x21 => "id",
        0x19 => "ru",
        _ => "en",
    };

    public static string T(string key) =>
        strings.TryGetValue(key, out var text) ? text : (english ??= Read("en") ?? []).GetValueOrDefault(key, key);

    public static string T(string key, params object?[] args) =>
        string.Format(CultureInfo.InvariantCulture, T(key), args);

    /// <summary>Font hợp với ngôn ngữ: chữ Trung, Nhật, Hàn hiển thị đẹp hơn với font riêng của Windows.</summary>
    public static Font UiFont(float size, FontStyle style = FontStyle.Regular) => new(Code switch
    {
        "zh" => "Microsoft YaHei UI",
        "ja" => "Yu Gothic UI",
        "ko" => "Malgun Gothic",
        _ => "Segoe UI",
    }, size, style);

    private static Dictionary<string, string>? Read(string code)
    {
        try
        {
            using var stream = typeof(Lang).Assembly.GetManifestResourceStream($"lang.{code}.json");
            return stream == null ? null : JsonSerializer.Deserialize<Dictionary<string, string>>(stream);
        }
        catch { return null; }
    }
}
