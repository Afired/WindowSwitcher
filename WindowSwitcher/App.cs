using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;

namespace WindowSwitcher;

public class App : Application
{
    private LauncherWindow? _launcher;
    
    public override void Initialize()
    {
        RequestedThemeVariant = ThemeVariant.Dark;
        Styles.Add(new FluentTheme());
        
        Extensions.RegisterExtendedProperties();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            GlobalHotkey.RegisterHotkey(() =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (_launcher is null || !_launcher.IsVisible)
                    {
                        _launcher = new LauncherWindow();
                        _launcher.Closed += (_, _) => _launcher = null;
                        _launcher.Show();
                        _launcher.Activate();
                    }
                });
            });
        }

        base.OnFrameworkInitializationCompleted();
    }
}