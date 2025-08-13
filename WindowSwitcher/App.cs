using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using WindowSwitcher.Services;
using WindowSwitcher.ViewModel;
using WindowSwitcher.Views;

namespace WindowSwitcher;

public class App : Application
{
    private WindowSwitcherView? _launcher;
    
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
                        _launcher = new WindowSwitcherView(new WindowSwitcherViewModel(new WindowService()));
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