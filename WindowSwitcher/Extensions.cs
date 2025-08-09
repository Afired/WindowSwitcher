using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace WindowSwitcher;

public static class Extensions
{
    public static readonly StyledProperty<ColumnDefinition?> ColumnDefinitionProperty = AvaloniaProperty.Register<Control, ColumnDefinition?>(nameof(ColumnDefinition));

    public static void RegisterExtendedProperties()
    {
        // triggers static constructor
    }
    
    extension(Panel panel)
    {
        public IEnumerable<Control> Items
        {
            set
            {
                foreach (Control item in value)
                {
                    panel.Children.Add(item);
                }
            }
        }
    }

    extension(Grid grid)
    {
        public IEnumerable<Control> Columns
        {
            set
            {
                foreach (Control column in value)
                {
                    grid.Children.Add(column);
                    grid.ColumnDefinitions.Add(column.ColumnDefinition ?? new ColumnDefinition(GridLength.Auto));
                    Grid.SetColumn(column, grid.Children.Count - 1);
                }
            }
        }
    }

    extension(Control control)
    {
        public static StyledProperty<ColumnDefinition?> ColumnDefinitionProperty => Extensions.ColumnDefinitionProperty;

        public ColumnDefinition? ColumnDefinition
        {
            get => control.GetValue(ColumnDefinitionProperty);
            set => control.SetValue(ColumnDefinitionProperty, value);
        }
    }

    extension<TInputElement>(TInputElement inputElement) where TInputElement : InputElement
    {
        public TInputElement WithPointerPressedEvent(EventHandler<PointerPressedEventArgs> eventHandler)
        {
            inputElement.PointerPressed += eventHandler;
            return inputElement;
        }
    }
}

