using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using WindowSwitcher.Bindings;

namespace WindowSwitcher.Services;

public class WindowService : IWindowService
{
    public IReadOnlyCollection<WindowInfo> GetWindows()
    {
        List<WindowInfo> windows = new List<WindowInfo>();

        WindowBindings.EnumDesktopWindows(IntPtr.Zero, (hWnd, lParam) =>
        {
            // 1. Filter early — skip invisible windows
            if (!WindowBindings.IsWindowVisible(hWnd))
            {
                return true;
            }

            // 2. Skip windows with no title
            int length = WindowBindings.GetWindowTextLength(hWnd);
            if (length == 0)
            {
                return true;
            }

            // 3. Skip tool windows
            uint exStyle = WindowBindings.GetWindowLong(hWnd, WindowBindings.GWL_EXSTYLE);
            if ((exStyle & WindowBindings.WS_EX_TOOLWINDOW) == WindowBindings.WS_EX_TOOLWINDOW)
            {
                return true;
            }

            // 4. Get title
            StringBuilder titleBuilder = new StringBuilder(length + 1);
            WindowBindings.GetWindowText(hWnd, titleBuilder, titleBuilder.Capacity);
            string title = titleBuilder.ToString();

            // 5. Get process ID and name (fast path)
            WindowBindings.GetWindowThreadProcessId(hWnd, out uint processId);
            string processName = GetProcessNameFast(processId);

            // 6. Get minimized status
            WindowBindings.WindowPlacement placement = new WindowBindings.WindowPlacement { length = Marshal.SizeOf<WindowBindings.WindowPlacement>() };
            bool isMinimized = WindowBindings.GetWindowPlacement(hWnd, ref placement) && placement.showCmd == 2;

            windows.Add(new WindowInfo(hWnd)
            {
                Title = title,
                ProcessName = processName,
                ProcessId = processId,
                IsVisible = true,
                IsMinimized = isMinimized,
                IsToolWindow = false
            });

            return true;
        }, IntPtr.Zero);

        return windows;
    }
    
    public void ActivateWindow(IntPtr hWnd)
    {
        WindowBindings.WindowPlacement placement = new WindowBindings.WindowPlacement();
        placement.length = Marshal.SizeOf(placement);

        WindowBindings.GetWindowPlacement(hWnd, ref placement);

        if (placement.showCmd == WindowBindings.SW_SHOWMAXIMIZED)
        {
            WindowBindings.ShowWindow(hWnd, WindowBindings.SW_SHOWMAXIMIZED);
        }
        else
        {
            WindowBindings.ShowWindow(hWnd, WindowBindings.SW_RESTORE);
        }

        WindowBindings.SetForegroundWindow(hWnd);
    }

    private static string GetProcessNameFast(uint pid)
    {
        if (pid == 0)
        {
            return string.Empty;
        }

        IntPtr process = WindowBindings.OpenProcess(WindowBindings.PROCESS_QUERY_INFORMATION | WindowBindings.PROCESS_VM_READ, false, pid);
        if (process == IntPtr.Zero)
        {
            // protected/system processes fail here
            return string.Empty;
        }

        try
        {
            StringBuilder name = new StringBuilder(256);
            uint len = WindowBindings.GetModuleBaseName(process, IntPtr.Zero, name, name.Capacity);
            return len == 0 ? string.Empty : name.ToString();
        }
        finally
        {
            WindowBindings.CloseHandle(process);
        }
    }
}