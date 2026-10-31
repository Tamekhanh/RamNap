using System.Drawing.Drawing2D;

namespace RamNap;

/// <summary>Vẽ biểu tượng trăng khuyết lúc chạy, khỏi cần file .ico.</summary>
internal static class AppIcon
{
    public static Icon Create()
    {
        using var bitmap = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            using var moon = new SolidBrush(Color.FromArgb(255, 200, 90));
            g.FillEllipse(moon, 2, 3, 27, 27);
            g.CompositingMode = CompositingMode.SourceCopy;
            using var cut = new SolidBrush(Color.Transparent);
            g.FillEllipse(cut, 11, -2, 24, 24);
        }

        IntPtr handle = bitmap.GetHicon();
        try
        {
            using var temp = Icon.FromHandle(handle);
            return (Icon)temp.Clone();
        }
        finally { Native.DestroyIcon(handle); }
    }

    /// <summary>Quả địa cầu (ký hiệu "ngôn ngữ") cho nút chọn ngôn ngữ, vẽ theo kích thước DPI hiện tại.</summary>
    public static Bitmap Globe(int size, Color color)
    {
        var bitmap = new Bitmap(size, size);
        using var g = Graphics.FromImage(bitmap);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        float stroke = Math.Max(1f, size / 14f);
        using var pen = new Pen(color, stroke);

        var r = new RectangleF(stroke / 2, stroke / 2, size - stroke - 1, size - stroke - 1);
        float cx = r.Left + r.Width / 2, cy = r.Top + r.Height / 2;
        g.DrawEllipse(pen, r);                                                     // vành ngoài
        g.DrawEllipse(pen, r.Left + r.Width * 0.28f, r.Top, r.Width * 0.44f, r.Height); // kinh tuyến
        g.DrawLine(pen, r.Left, cy, r.Right, cy);                                  // xích đạo
        float half = r.Width * 0.43f;                                              // vĩ tuyến ở 1/4 và 3/4 chiều cao
        g.DrawLine(pen, cx - half, r.Top + r.Height * 0.25f, cx + half, r.Top + r.Height * 0.25f);
        g.DrawLine(pen, cx - half, r.Top + r.Height * 0.75f, cx + half, r.Top + r.Height * 0.75f);
        return bitmap;
    }
}
