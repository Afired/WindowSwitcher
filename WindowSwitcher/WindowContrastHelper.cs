using Avalonia.Platform;

namespace WindowSwitcher;

using System;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Media;

public static class WindowContrastHelper
{
    [DllImport("user32.dll")]
    static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("user32.dll")]
    static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

    [DllImport("gdi32.dll")]
    static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest,
        int nWidth, int nHeight, IntPtr hdcSrc,
        int nXSrc, int nYSrc, System.Drawing.CopyPixelOperation dwRop);

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    public static float GetBackdropBrightness(Window window)
    {
        IPlatformHandle? platformHandle = window.TryGetPlatformHandle();
        if (platformHandle?.Handle is not { } hwnd)
        {
            return 1;
        }

        GetWindowRect(hwnd, out var rect);
        int width = rect.Right - rect.Left;
        int height = rect.Bottom - rect.Top;

        // Capture small sample area in center
        int sampleWidth = Math.Min(width, 200);
        int sampleHeight = Math.Min(height, 100);

        using var bmp = new System.Drawing.Bitmap(sampleWidth, sampleHeight);
        using (var g = System.Drawing.Graphics.FromImage(bmp))
        {
            IntPtr desktopDC = GetDC(IntPtr.Zero);
            IntPtr gHdc = g.GetHdc();

            BitBlt(gHdc, 0, 0, sampleWidth, sampleHeight, desktopDC,
                rect.Left + (width - sampleWidth) / 2,
                rect.Top + (height - sampleHeight) / 2,
                System.Drawing.CopyPixelOperation.SourceCopy | System.Drawing.CopyPixelOperation.CaptureBlt);

            g.ReleaseHdc(gHdc);
            ReleaseDC(IntPtr.Zero, desktopDC);
        }

        // Calculate average brightness
        float brightness = 0;
        int count = 0;
        for (int y = 0; y < bmp.Height; y++)
        {
            for (int x = 0; x < bmp.Width; x++)
            {
                var color = bmp.GetPixel(x, y);
                brightness += (color.R * 0.299f + color.G * 0.587f + color.B * 0.114f) / 255f;
                count++;
            }
        }
        brightness /= count;
        
        return brightness;
    }
}
