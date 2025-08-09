using System.Threading;
using System;
using System.Runtime.InteropServices;

namespace WindowSwitcher;

public static class GlobalHotkey
{
    private const int MOD_CONTROL = 0x2;
    private const int WM_HOTKEY = 0x0312;
    private static IntPtr _windowHandle;
    private static ushort _hotkeyId = 1;

    public static void RegisterHotkey(Action onHotkeyPressed)
    {
        // Create a message loop window
        Thread thread = new Thread(new MessageOnlyWindow(onHotkeyPressed).RunMessageLoop)
        {
            IsBackground = true,
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
    }

    private class MessageOnlyWindow
    {
        private readonly Action _callback;
        private IntPtr _handle;

        public MessageOnlyWindow(Action callback)
        {
            _callback = callback;
        }

        public void RunMessageLoop()
        {
            _handle = CreateMessageWindow();
            RegisterHotKey(_handle, _hotkeyId, MOD_CONTROL, (uint)ConsoleKey.Spacebar);

            while (GetMessage(out MSG msg, IntPtr.Zero, 0, 0))
            {
                if (msg.message == WM_HOTKEY)
                {
                    _callback();
                }

                TranslateMessage(ref msg);
                DispatchMessage(ref msg);
            }
        }

        private IntPtr CreateMessageWindow()
        {
            WNDCLASS wc = new WNDCLASS
            {
                lpszClassName = "MsgOnlyWnd",
                lpfnWndProc = DefWindowProc
            };

            RegisterClass(ref wc);
            return CreateWindowEx(0, wc.lpszClassName, "", 0, 0, 0, 0, 0,
                HWND_MESSAGE, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
        }

        private const int HWND_MESSAGE = -3;

        [StructLayout(LayoutKind.Sequential)]
        private struct WNDCLASS
        {
            public uint style;
            public IntPtr lpfnWndProc;
            public int cbClsExtra;
            public int cbWndExtra;
            public IntPtr hInstance;
            public IntPtr hIcon;
            public IntPtr hCursor;
            public IntPtr hbrBackground;
            public string lpszMenuName;
            public string lpszClassName;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSG
        {
            public IntPtr hwnd;
            public uint message;
            public IntPtr wParam;
            public IntPtr lParam;
            public uint time;
            public System.Drawing.Point pt;
        }

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

        [DllImport("user32.dll")]
        private static extern bool TranslateMessage(ref MSG lpMsg);

        [DllImport("user32.dll")]
        private static extern IntPtr DispatchMessage(ref MSG lpmsg);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern ushort RegisterClass(ref WNDCLASS lpWndClass);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr CreateWindowEx(
            int dwExStyle, string lpClassName, string lpWindowName, int dwStyle,
            int x, int y, int nWidth, int nHeight,
            int hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

        private static readonly IntPtr DefWindowProc = GetProcAddress(GetModuleHandle("user32.dll"), "DefWindowProcW");

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);
    }
}
