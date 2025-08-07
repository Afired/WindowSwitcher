using Avalonia;

namespace WindowSwitcher;

public static class Extensions
{
    public static T Set<T>(this T obj, ref T? field)
        where T : AvaloniaObject
    {
        return field = obj;
    }
}