using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace WindowSwitcher;

public class WindowInfo
{
    public IntPtr Handle { get; }
    public required string Title { get; init; }
    public required string ProcessName { get; init; }
    public required uint ProcessId { get; init; }
    public required bool IsVisible { get; init; }
    public required bool IsMinimized { get; init; }
    public required bool IsToolWindow { get; init; }

    public WindowInfo(IntPtr handle)
    {
        Handle = handle;
    }

    public override string ToString()
    {
        Span<char> processDisplayName = stackalloc char[ProcessName.Length];
        ProcessName.AsSpan().CopyTo(processDisplayName);
        if (processDisplayName.Length >= 1)
        {
            processDisplayName[0] = char.ToUpper(processDisplayName[0]);
        }
        
        Span<char> windowDisplayTitle = stackalloc char[Title.Length];
        Title.AsSpan().CopyTo(windowDisplayTitle);
        if(Title.LastIndexOfAny(['–', '-', '—']) is > 0 and var i)
        {
            windowDisplayTitle = windowDisplayTitle[..i];
        }
        return $"[{processDisplayName}] {windowDisplayTitle}";
    }
}

public static class Desktop
{
    public static IReadOnlyCollection<WindowInfo> GetWindows()
    {
        List<WindowInfo> windows = new List<WindowInfo>();

        WindowBindings.EnumWindows((hWnd, lParam) =>
        {
            WindowBindings.GetWindowThreadProcessId(hWnd, out var processId);

            Process process;
            try
            {
                process = Process.GetProcessById((int)processId);
            }
            catch(ArgumentException)
            {
                return true; // skip if process is no longer running
            }

            string processName = process.ProcessName;

            int length = WindowBindings.GetWindowTextLength(hWnd);
            StringBuilder builder = new StringBuilder(length + 1);
            WindowBindings.GetWindowText(hWnd, builder, builder.Capacity);
            string title = builder.ToString();

            uint exStyle = WindowBindings.GetWindowLong(hWnd, WindowBindings.GWL_EXSTYLE);
            bool isToolWindow = (exStyle & WindowBindings.WS_EX_TOOLWINDOW) == WindowBindings.WS_EX_TOOLWINDOW;

            WindowBindings.WindowPlacement placement = new WindowBindings.WindowPlacement();
            placement.length = Marshal.SizeOf(placement);
            bool isMinimized = WindowBindings.GetWindowPlacement(hWnd, ref placement) && placement.showCmd == 2;

            windows.Add(new WindowInfo(hWnd)
            {
                Title = title,
                ProcessName = processName,
                ProcessId = processId,
                IsVisible = WindowBindings.IsWindowVisible(hWnd),
                IsMinimized = isMinimized,
                IsToolWindow = isToolWindow
            });

            return true;
        }, IntPtr.Zero);

        return windows;
    }
}