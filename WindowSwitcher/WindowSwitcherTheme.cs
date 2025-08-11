using System;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Styling;

namespace WindowSwitcher;

public class WindowSwitcherTheme : Styles
{
    public WindowSwitcherTheme()
    {
        Add(new Style(x => x.OfType<ListBox>().Class("WindowList"))
        {
            Setters = { },
            Children =
            {
                new Style(x => x.Nesting().Child().OfType<ListBoxItem>())
                {
                    Setters =
                    {
                        new Setter(TemplatedControl.PaddingProperty, new Thickness(0)),
                    },
                    Children =
                    {
                        new Style(x => x.Nesting().Template().OfType<ContentPresenter>().Name("PART_ContentPresenter"))
                        {
                            Setters =
                            {
                                new Setter(TemplatedControl.BackgroundProperty, new DynamicResourceExtension("SystemControlListNormalBrush")),
                                new Setter(Animatable.TransitionsProperty, new Transitions
                                {
                                    new BrushTransition
                                    {
                                        Property = Border.BackgroundProperty,
                                        Duration = TimeSpan.FromSeconds(0.1),
                                        Easing = new LinearEasing()
                                    }
                                }),
                            },
                            Children =
                            {
                                new Style(x => x.Nesting().Class(":pointerover"))
                                {
                                    Setters = { new Setter(TemplatedControl.BackgroundProperty, new DynamicResourceExtension("SystemControlHighlightListLowBrush")) }
                                },
                            },
                            Resources =
                            {
                                ["SystemControlListNormalBrush"] = new LinearGradientBrush  // normal
                                {
                                    StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                                    EndPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                                    GradientStops =
                                    {
                                        new GradientStop(new Color(25, 255, 255, 255), 0),
                                        new GradientStop(Colors.Transparent, 1),
                                    }
                                },
                                ["SystemControlHighlightListLowBrush"] = new LinearGradientBrush  // hovered
                                {
                                    StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                                    EndPoint = new RelativePoint(1, 0, RelativeUnit.Relative),
                                    GradientStops =
                                    {
                                        new GradientStop(new Color(25, 255, 255, 255), 0),
                                        new GradientStop(Colors.Transparent, 1),
                                    }
                                },
                                ["SystemControlHighlightListAccentLowBrush"] =   new LinearGradientBrush // selected
                                {
                                    StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                                    EndPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                                    GradientStops =
                                    {
                                        new GradientStop(new Color(25, 255, 255, 255), 0),
                                        new GradientStop(new Color(25, 255, 255, 255), 1),
                                    }
                                },
                                ["SystemControlHighlightListAccentMediumBrush"] =  new LinearGradientBrush // selected hovered
                                {
                                    StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                                    EndPoint = new RelativePoint(1, 0, RelativeUnit.Relative),
                                    GradientStops =
                                    {
                                        new GradientStop(new Color(50, 255, 255, 255), 0),
                                        new GradientStop(new Color(25, 255, 255, 255), 1),
                                    }
                                },
                                // because of overrides, these are currently not triggered anyway:
                                ["SystemControlHighlightListMediumBrush"] = null, // pressed
                                ["SystemControlHighlightListHighBrush"] = null, // selected pressed
                            }
                        },
                        // selected pseudo class is only on parent ListBoxItem
                        new Style(x => x.Nesting().Class(":selected").Template().OfType<ContentPresenter>().Name("PART_ContentPresenter"))
                        {
                            Setters = { new Setter(TemplatedControl.BackgroundProperty, new DynamicResourceExtension("SystemControlHighlightListAccentLowBrush")) },
                            Children =
                            {
                                new Style(x => x.Nesting().Class(":pointerover"))
                                {
                                    Setters = { new Setter(TemplatedControl.BackgroundProperty, new DynamicResourceExtension("SystemControlHighlightListAccentMediumBrush")) },
                                }
                            }
                        }
                    }
                }
            },
        });
        
        Add(new Style(x => x.OfType<TextBox>().Class("WindowSearch"))
        {
            Resources =
            {
                ["TextControlBorderBrushPointerOver"] = new SolidColorBrush(Color.Parse("#99ffffff")),
                ["TextControlBorderBrushFocused"] = new SolidColorBrush(Color.Parse("#99ffffff")),
                ["TextControlBorderThemeThicknessFocused"] = new Thickness(1),
            }
        });
    }
}