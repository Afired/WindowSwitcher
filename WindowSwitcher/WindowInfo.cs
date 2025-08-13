using System;
using System.Collections.Generic;
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
        
        ReadOnlySpan<char> windowDisplayTitle = Title.AsSpan();
        if(Title.AsSpan().LastIndexOfAny(['–', '-', '—']) is > 0 and var i)
        {
            windowDisplayTitle = windowDisplayTitle[..i];
        }
        return $"[{processDisplayName}] {windowDisplayTitle}";
    }

    public string GetProcessDisplayName()
    {
        if (ProcessName.Length > 0)
        {
            return $"{char.ToUpper(ProcessName[0])}{ProcessName[1..]}";
        }
        else
        {
            return ProcessName;
        }
    }
    
    public ReadOnlySpan<char> GetDisplayTitle()
    {
        if(Title.LastIndexOfAny(['–', '-', '—']) is > 0 and var i)
        {
            return Title.AsSpan()[..i];
        }
        else
        {
            return Title.AsSpan();
        }
    }
}