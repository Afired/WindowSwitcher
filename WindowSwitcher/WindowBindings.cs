using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Avalonia.Controls;
using Avalonia.Media.Imaging;

namespace WindowSwitcher;

public static class WindowBindings
{
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("user32.dll")]
    public static extern IntPtr GetShellWindow();

    [DllImport("user32.dll")]
    public static extern uint GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    public static extern bool GetWindowPlacement(IntPtr hWnd, ref WindowPlacement lpwndpl);

    public const int GWL_EXSTYLE = -20;
    public const uint WS_EX_TOOLWINDOW = 0x00000080;

    [StructLayout(LayoutKind.Sequential)]
    public struct WindowPlacement
    {
        public int length;
        public int flags;
        public int showCmd;
        public System.Drawing.Point ptMinPosition;
        public System.Drawing.Point ptMaxPosition;
        public System.Drawing.Rectangle rcNormalPosition;
    }
    
    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);
    
    public static void ActivateWindow(IntPtr hWnd)
    {
        var placement = new WindowBindings.WindowPlacement();
        placement.length = Marshal.SizeOf(placement);

        WindowBindings.GetWindowPlacement(hWnd, ref placement);

        if (placement.showCmd == WindowBindings.SW_SHOWMAXIMIZED)
            WindowBindings.ShowWindow(hWnd, WindowBindings.SW_SHOWMAXIMIZED);
        else
            WindowBindings.ShowWindow(hWnd, WindowBindings.SW_RESTORE);

        WindowBindings.SetForegroundWindow(hWnd);
    }

    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    
    public const int SW_SHOWMAXIMIZED = 3;
    public const int SW_RESTORE = 9;
    
    
    public enum DwmWindowCornerPreference
    {
        Default = 0,
        DoNotRound = 1,
        Round = 2,
        RoundSmall = 3
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(
        IntPtr hwnd,
        uint dwAttribute,
        ref DwmWindowCornerPreference pvAttribute,
        uint cbAttribute);

    public static void SetRoundedCorners(Window window, DwmWindowCornerPreference cornerPreference)
    {
        var platformHandle = window.TryGetPlatformHandle();
        if (platformHandle?.Handle is { } hwnd)
        {
            uint size = sizeof(DwmWindowCornerPreference);
            DwmSetWindowAttribute(hwnd, 33 /* DWMWA_WINDOW_CORNER_PREFERENCE */, ref cornerPreference, size);
        }
    }
    
    public static System.Drawing.Icon? GetIconForWindow(IntPtr hWnd)
    {
        if (hWnd == IntPtr.Zero)
            return null;

        // Get the process ID
        GetWindowThreadProcessId(hWnd, out uint processId);

        try
        {
            var process = Process.GetProcessById((int)processId);
            var exePath = process.MainModule?.FileName;

            if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
            {
                return System.Drawing.Icon.ExtractAssociatedIcon(exePath);
            }
        }
        catch
        {
            // Access to some system processes may be denied
        }

        return null;
    }
    
    public static Bitmap? ConvertToAvaloniaBitmap(System.Drawing.Icon icon)
    {
        using var ms = new MemoryStream();
        icon.ToBitmap().Save(ms, System.Drawing.Imaging.ImageFormat.Png);
        ms.Seek(0, SeekOrigin.Begin);
        return new Bitmap(ms);
    }
    
    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hWnd, out Rect lpRect);

    [StructLayout(LayoutKind.Sequential)]
    public struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}