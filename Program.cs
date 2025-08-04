using System.Collections.Frozen;
using System.Runtime.InteropServices;

namespace WindowSwitcher;

internal static class Program
{
    public static readonly FrozenSet<string> BlacklistedProcessNames = new HashSet<string>()
    {
        "ApplicationFrameHost",
        "SystemSettings",
        "TextInputHost",
    }.ToFrozenSet();

    public static int Main(string[] args)
    {
        bool ctrlDown = false;
        bool spaceDown = false;
        string currentInput = "";

        IntPtr hookID = IntPtr.Zero;
        InputBindings.LowLevelKeyboardProc hook = HookCallback; // stored in variable to not be garbage collected
        hookID = InputBindings.SetHook(hook);
        Console.WriteLine($"Set Hook: '{hookID}'");

        Console.CancelKeyPress += (s, e) =>
        {
            Console.WriteLine("Ctrl+C detected, unhooking...");
            InputBindings.UnhookWindowsHookEx(hookID);
            e.Cancel = true; // prevent default exit
            Environment.Exit(0);
        };

        while (InputBindings.GetMessage(out var msg, IntPtr.Zero, 0, 0) > 0)
        {
            InputBindings.TranslateMessage(ref msg);
            InputBindings.DispatchMessage(ref msg);
        }

        InputBindings.UnhookWindowsHookEx(hookID);
        Console.WriteLine($"Unhooked: '{hookID}'");
        return 0;

        IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                InputKey key = (InputKey)vkCode;

                bool keyDown = wParam is InputBindings.WM_KEYDOWN or InputBindings.WM_SYSKEYDOWN;
                bool keyUp = wParam is InputBindings.WM_KEYUP or InputBindings.WM_SYSKEYUP;

                if (key is InputKey.LeftControl)
                    ctrlDown = keyDown;

                if (key is InputKey.Space)
                    spaceDown = keyDown;

                if (keyDown)
                {
                    if (ctrlDown && spaceDown)
                    {
                        bool isAlphaNumeric = key is (>= InputKey.A and <= InputKey.Z) or (>= InputKey.D0 and <= InputKey.D9);
                        if (isAlphaNumeric)
                        {
                            currentInput += key.ToString();
                        
                            Console.Clear();
                            Console.WriteLine($"Filtering input: {currentInput}");
                    
                            // Return a non-zero value to consume input
                            return 1;
                        }
                    }
                }
                else if (keyUp)
                {
                    if (key is InputKey.LeftControl or InputKey.Space)
                    {
                        if (currentInput.Length > 0)
                        {
                            try
                            {
                                QueryWindows(currentInput);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e.StackTrace);
                            }
                            currentInput = string.Empty;
                        }
                    }
                }
            }

            return InputBindings.CallNextHookEx(hookID, nCode, wParam, lParam);
        }


        void QueryWindows(string input)
        {
            IEnumerable<WindowInfo> filteredWindows = Desktop.GetWindows();

            filteredWindows = filteredWindows.Where(x => x is
            {
                IsVisible: true,
                Title.Length: > 0,
                IsToolWindow: false,
            });

            filteredWindows = filteredWindows.Where(x => !BlacklistedProcessNames.Contains(x.ProcessName));

            filteredWindows = filteredWindows.Where(x => x.ProcessName.StartsWith(input, StringComparison.OrdinalIgnoreCase));

            if (filteredWindows.FirstOrDefault() is { } pWindow)
            {
                ActivateWindow(pWindow.Handle);
            }
    
            filteredWindows = filteredWindows.OrderBy(x => x.ProcessName);

            foreach (WindowInfo userWindow in filteredWindows)
            {
                string processDisplayName = userWindow.ProcessName;
                string windowDisplayTitle = userWindow.Title[..userWindow.Title.LastIndexOfAny(['–', '-', '—'])];
                Console.WriteLine($"[{processDisplayName}] {windowDisplayTitle}");
            }
        }

        void ActivateWindow(IntPtr hWnd)
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
    }
}