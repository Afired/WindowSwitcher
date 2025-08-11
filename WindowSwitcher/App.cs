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
        Extensions.RegisterExtendedProperties();
        
        Styles.Add(new FluentTheme());
        Styles.Add(new WindowSwitcherTheme());
        
        // FluentTheme.DensityStyleProperty;
        
        RequestedThemeVariant = ThemeVariant.Dark;
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
                    if (_launcher is null)
                    {
                        _launcher = new LauncherWindow();
                        _launcher.Closed += (_, _) => _launcher = null;
                    }
                    
                    if (!_launcher.IsVisible)
                    {
                        _launcher.Show();
                        _launcher.Activate();
                    }
                });
            });
        }

        base.OnFrameworkInitializationCompleted();
    }
}