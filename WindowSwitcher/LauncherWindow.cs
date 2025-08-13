using System;
using System.Collections.Frozen;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Threading;
using WindowSwitcher.Services;

namespace WindowSwitcher;

public class LauncherWindow : Window
{
    private readonly TextBox _searchBox;
    private readonly ListBox _resultsList;
    private readonly ObservableCollection<WindowEntry> _allWindows = new();
    private readonly ObservableCollection<WindowEntry> _filteredWindows = new();
    
    private readonly IWindowService _windowService;
    
    public static readonly FrozenSet<string> BlacklistedProcessNames = new HashSet<string>()
    {
        "ApplicationFrameHost",
        "SystemSettings",
        "TextInputHost",
    }.ToFrozenSet();

    public Screen? GetLastFocusedScreen()
    {
        Screens screens = Screens;
        IntPtr hwnd = WindowBindings.GetForegroundWindow();
        if (hwnd != IntPtr.Zero && WindowBindings.GetWindowRect(hwnd, out WindowBindings.Rect rect))
        {
            // Use the window's center point
            int centerX = (rect.Left + rect.Right) / 2;
            int centerY = (rect.Top + rect.Bottom) / 2;

            return screens.ScreenFromPoint(new PixelPoint(centerX, centerY));
        }

        return null;
    }

    public LauncherWindow(IWindowService windowService)
    {
        _windowService =  windowService;
        
        Screen? screen = GetLastFocusedScreen() ?? Screens.Primary;
        PixelRect workingArea = screen.WorkingArea;
        
        Width = 600;
        SizeToContent = SizeToContent.Height;
        MaxHeight = workingArea.Height * 0.7d;
        Position = new PixelPoint(
            (int)(workingArea.X + (workingArea.Width - Width) / 2d),
            (int)(workingArea.Y + (workingArea.Height - MaxHeight) / 2d)
        );
        
        CanResize = false;
        ShowInTaskbar = false;
        Topmost = true;
        SystemDecorations = SystemDecorations.None;
        TransparencyLevelHint = [ WindowTransparencyLevel.AcrylicBlur ];
        Background = Brushes.Transparent;
        
        Content = new Border
        {
            Background = new  SolidColorBrush(new Color(50, 0, 0, 0)),
            Padding = new Thickness(10),
            Child = new Grid
            {
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                RowSpacing = 10,
                Rows =
                [
                    _searchBox = new TextBox
                    {
                        Classes = { "WindowSearch" },
                        Watermark = "Search...",
                    },
                    _resultsList = new ListBox
                    {
                        Classes = { "WindowList" },
                        RowDefinition = new RowDefinition(GridLength.Star),
                        [!ListBox.BackgroundProperty] = new DynamicResourceExtension("ComplimentaryBrushLow"),
                        CornerRadius = new CornerRadius(3),
                        ItemsSource = _filteredWindows,
                        ItemTemplate = new FuncDataTemplate<WindowEntry>((windowEntry, _) => new Border
                        {
                            Padding = new Thickness(10),
                            Background = Brushes.Transparent, // important to be a hit target for callback
                            Child = new Grid
                            {
                                ColumnSpacing = 12,
                                Columns =
                                [
                                    new Image
                                    {
                                        Width = 20,
                                        Height = 20,
                                        Source = windowEntry.Icon,
                                        VerticalAlignment = VerticalAlignment.Center,
                                    },
                                    new Grid
                                    {
                                        ColumnSpacing = 8,
                                        VerticalAlignment = VerticalAlignment.Center,
                                        ColumnDefinition = new ColumnDefinition(GridLength.Star),
                                        Columns =
                                        [
                                            new TextBlock
                                            {
                                                Text = windowEntry.Info.GetProcessDisplayName(),
                                                FontSize = 14,
                                                [!TextBlock.ForegroundProperty] = new DynamicResourceExtension("ContrastBrushHigh"),
                                                VerticalAlignment = VerticalAlignment.Bottom,
                                            },
                                            new TextBlock
                                            {
                                                Text = windowEntry.Info.GetDisplayTitle().ToString(),
                                                FontSize = 11,
                                                [!TextBlock.ForegroundProperty] = new DynamicResourceExtension("ContrastBrushMedium"),
                                                TextTrimming = TextTrimming.CharacterEllipsis,
                                                VerticalAlignment = VerticalAlignment.Bottom,
                                                ColumnDefinition = new ColumnDefinition(GridLength.Star),
                                            },
                                        ]
                                    }
                                ],
                            }
                        }.WithPointerPressedEvent((_, e) =>
                        {
                            _windowService.ActivateWindow(windowEntry.Info.Handle);
                            e.Handled = true;
                            Close();
                        }), true),
                    },
                ]
            }
        };
        
        _searchBox.KeyDown += SearchBoxOnKeyDown;
        _searchBox.TextChanged += (_, _) => ApplyFilter();
        _allWindows.CollectionChanged += (_, _) => ApplyFilter();
        _searchBox.KeyUp += SearchBoxOnKeyUp;
        KeyDown += OnKeyDown;
        
        Opened += (_, _) =>
        {
            WindowBindings.SetRoundedCorners(this, WindowBindings.DwmWindowCornerPreference.RoundSmall);
            _searchBox.Focus();
            // _resultsList.SelectedIndex = 0;
        };

#if DEBUG
        this.AttachDevTools();
#else
        // for debugging purposes we don't automatically close the window in debug builds
        Deactivated += (_, _) => Close();
#endif
        FetchWindows(windowService);
        AdjustTheme();
    }

    private void AdjustTheme()
    {
        float brightness = WindowContrastHelper.GetBackdropBrightness(this);
        float threshold = 0.3f;
        RequestedThemeVariant = brightness < threshold ? ThemeVariant.Dark : ThemeVariant.Light;
    }

    private void FetchWindows(IWindowService windowService)
    {
        // fetch available windows
        new Thread(() =>
        {
            WindowEntry[] windowEntries = windowService.GetWindows()
                .Where(x => x is
                {
                    IsVisible: true,
                    Title.Length: > 0,
                    IsToolWindow: false,
                })
                .Where(x => !BlacklistedProcessNames.Contains(x.ProcessName))
                .OrderBy(x => x.ProcessName)
                .Select(x =>
                {
                    System.Drawing.Icon? icon = WindowBindings.GetIconForWindow(x.Handle);
                    Avalonia.Media.Imaging.Bitmap? bitmap = null;
                    if (icon != null)
                    {
                        bitmap = WindowBindings.ConvertToAvaloniaBitmap(icon);
                    }

                    return new WindowEntry
                    {
                        Icon = bitmap,
                        Info = x,
                    };
                }).ToArray();
            
            Dispatcher.UIThread.Post(() =>
            {
                foreach (WindowEntry windowEntry in windowEntries)
                {
                    _allWindows.Add(windowEntry);
                }
            });
        })
        {
            IsBackground = true,
        }.Start();
    }

    private void ApplyFilter()
    {
        string query = _searchBox.Text ?? string.Empty;
        IEnumerable<WindowEntry> matches = _allWindows.Where(x => x.Info.ProcessName.StartsWith(query, StringComparison.OrdinalIgnoreCase));

        _filteredWindows.Clear();
        foreach (var item in matches)
        {
            _filteredWindows.Add(item);
        }

        if (query != string.Empty)
        {
            if (!((IEnumerable<WindowEntry>)_resultsList.ItemsSource).Contains(_resultsList.SelectedItem as WindowEntry))
            {
                _resultsList.SelectedIndex = 0;
            }
        }
    }

    private void SearchBoxOnKeyUp(object? sender, KeyEventArgs e)
    {
        bool isQuickSwitch = e
            is { Key: Key.Space, KeyModifiers: KeyModifiers.Control }
            or { Key: Key.LeftCtrl };
        if (isQuickSwitch)
        {
            if (_resultsList.SelectedItem is WindowEntry windowEntry)
            {
                _windowService.ActivateWindow(windowEntry.Info.Handle);
                e.Handled = true;
                Close();
            }
        }
    }

    private void SearchBoxOnKeyDown(object? sender, KeyEventArgs e)
    {
        int? navigationInfo = e switch
        {
            { Key: Key.Tab, KeyModifiers: not KeyModifiers.Shift } => 1,
            { Key: Key.Tab, KeyModifiers: KeyModifiers.Shift } => -1,
            { Key: Key.Down } => 1,
            { Key: Key.Up } => -1,
            _ => null,
        };
        if (navigationInfo is { } navigationDifference)
        {
            _resultsList.SelectedIndex = Math.Clamp(_resultsList.SelectedIndex + navigationDifference, 0, _resultsList.Items.Count - 1);
            e.Handled = true;
        }
        
        // forward typed characters when ctr is held
        if (e.KeyModifiers is KeyModifiers.Control)
        {
            if (e.Key.ToString() is { Length: 1 } symbol)
            {
                string oldText = _searchBox.Text ?? string.Empty;
                int oldCaretIndex = Math.Clamp(_searchBox.CaretIndex, 0, oldText.Length);
                string newText = oldText[..oldCaretIndex] + symbol + oldText[oldCaretIndex..];
                int newCaretIndex = oldCaretIndex + symbol.Length;
                _searchBox.Text = newText;
                _searchBox.CaretIndex = newCaretIndex;
            }
        }
        
        // can also confirm with space
        if (e.Key is Key.Space)
        {
            if (_resultsList.SelectedItem is WindowEntry windowEntry)
            {
                _windowService.ActivateWindow(windowEntry.Info.Handle);
            }
            e.Handled = true;
            Close();
        }
        // can also cancel with ctrl
        else if (e.Key is Key.LeftCtrl)
        {
            e.Handled = true;
            Close();
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (_resultsList.SelectedItem is WindowEntry windowEntry)
            {
                _windowService.ActivateWindow(windowEntry.Info.Handle);
            }
            e.Handled = true;
            Close();
        }
        else if (e.Key == Key.Escape)
        {
            Close();
        }
    }
}