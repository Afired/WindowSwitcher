using System;
using System.Collections.Frozen;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace WindowSwitcher;

public class LauncherWindow : Window
{
    private TextBox _searchBox;
    private ListBox _resultsList;
    private List<WindowEntry> _availableWindows;
    
    public static readonly FrozenSet<string> BlacklistedProcessNames = new HashSet<string>()
    {
        "ApplicationFrameHost",
        "SystemSettings",
        "TextInputHost",
    }.ToFrozenSet();

    public LauncherWindow()
    {
        Width = 600;
        SizeToContent = SizeToContent.Height;
        Screen? screen = Screens.Primary;
        PixelRect workingArea = screen.WorkingArea;
        Position = new PixelPoint(
            (int)(workingArea.X + (workingArea.Width - this.Width) / 2),
            (int)(workingArea.Y + workingArea.Height * 0.1)
        );
        CanResize = false;
        ShowInTaskbar = false;
        Topmost = true;
        SystemDecorations = SystemDecorations.None;
        TransparencyLevelHint = [WindowTransparencyLevel.AcrylicBlur];
        Background = Brushes.Transparent;

        _availableWindows = Desktop.GetWindows()
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
                var icon = WindowBindings.GetIconForWindow(x.Handle);
                Bitmap? bitmap = null;
                if (icon != null)
                {
                    bitmap = WindowBindings.ConvertToAvaloniaBitmap(icon);
                }

                return new WindowEntry
                {
                    Icon = bitmap,
                    Info = x,
                };
            })
            .ToList();

        Content = new Border
        {
            Child = new StackPanel
            {
                Orientation = Orientation.Vertical,
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Items =
                [
                    _searchBox = new TextBox
                    {
                        Watermark = "Search...",
                        Margin = new Thickness(10),
                    },
                    _resultsList = new ListBox
                    {
                        Margin = new Thickness(10),
                        MaxHeight = 800,
                        ItemsSource = _availableWindows.ToArray(),
                        ItemTemplate = new FuncDataTemplate<WindowEntry>((windowEntry, _) => new Grid
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
                                    Columns =
                                    [
                                        new TextBlock
                                        {
                                            Text = windowEntry.Info.GetProcessDisplayName(),
                                            FontSize = 14,
                                            VerticalAlignment = VerticalAlignment.Bottom,
                                        },
                                        new TextBlock
                                        {
                                            Text = windowEntry.Info.GetDisplayTitle().ToString(),
                                            FontSize = 11,
                                            Foreground = Brushes.Gray,
                                            TextTrimming = TextTrimming.CharacterEllipsis,
                                            VerticalAlignment = VerticalAlignment.Bottom,
                                            ColumnDefinition = new ColumnDefinition(GridLength.Star)
                                        }
                                    ]
                                }
                            ],
                        }, true),
                    },
                ]
            }
        };
        
        _searchBox.KeyDown += SearchBoxOnKeyDown;
        _searchBox.TextChanged += SearchChanged;
        _searchBox.KeyUp += SearchBoxOnKeyUp;
        KeyDown += OnKeyDown;
        
        Opened += (_, _) =>
        {
            WindowBindings.SetRoundedCorners(this, WindowBindings.DwmWindowCornerPreference.RoundSmall);
            _searchBox.Focus();
            // _resultsList.SelectedIndex = 0;
        };
        
        LostFocus += (_, _) => Close();
        Deactivated += (_, _) => Close();
    }

    private void SearchChanged(object? sender, TextChangedEventArgs e)
    {
        string query = _searchBox.Text ?? string.Empty;
        IEnumerable<WindowEntry> matches = _availableWindows.Where(x => x.Info.ProcessName.StartsWith(query, StringComparison.OrdinalIgnoreCase));
        _resultsList.ItemsSource = matches.ToArray();
        
        if (!((IEnumerable<WindowEntry>)_resultsList.ItemsSource).Contains(_resultsList.SelectedItem as WindowEntry))
        {
            _resultsList.SelectedIndex = 0;
        }
    }

    private void SearchBoxOnKeyUp(object? sender, KeyEventArgs e)
    {
        Console.WriteLine($"Up: {e.Key}");

        bool isQuickSwitch = e
            is { Key: Key.Space, KeyModifiers: KeyModifiers.Control }
            or { Key: Key.LeftCtrl };
        if (isQuickSwitch)
        {
            if (_resultsList.SelectedItem is WindowEntry windowEntry)
            {
                WindowBindings.ActivateWindow(windowEntry.Info.Handle);
                e.Handled = true;
                Close();
            }
            else if (_resultsList.Items.Count == 0)
            {
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
                WindowBindings.ActivateWindow(windowEntry.Info.Handle);
            }
            e.Handled = true;
            Close();
        }
        
        // can also cancel with ctrl
        if (e.Key is Key.LeftCtrl)
        {
            e.Handled = true;
            Close();
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        Console.WriteLine($"Down: {e.Key}");
        if (e.Key == Key.Enter)
        {
            if (_resultsList.SelectedItem is WindowEntry windowEntry)
            {
                WindowBindings.ActivateWindow(windowEntry.Info.Handle);
            }
            e.Handled = true;
            Close();
        }

        if (e.Key == Key.Escape)
        {
            Close();
        }
    }
}