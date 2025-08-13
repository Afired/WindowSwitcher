using System;
using System.Collections.Generic;

namespace WindowSwitcher.Services;

public interface IWindowService
{
    public abstract IReadOnlyCollection<WindowInfo> GetWindows();

    public abstract void ActivateWindow(IntPtr hWnd);
}