using Avalonia.Media.Imaging;

namespace WindowSwitcher;

public class WindowEntry
{
    public required Bitmap? Icon { get; init; }
    public required WindowInfo Info { get; init; }
}