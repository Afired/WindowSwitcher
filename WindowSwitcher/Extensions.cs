using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace WindowSwitcher;

public static class Extensions
{
    public static readonly StyledProperty<ColumnDefinition?> ColumnDefinitionProperty = AvaloniaProperty.Register<Control, ColumnDefinition?>(nameof(ColumnDefinition));
    public static readonly StyledProperty<RowDefinition?> RowDefinitionProperty = AvaloniaProperty.Register<Control, RowDefinition?>(nameof(RowDefinition));

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
        
        public IEnumerable<Control> Rows
        {
            set
            {
                foreach (Control row in value)
                {
                    grid.Children.Add(row);
                    grid.RowDefinitions.Add(row.RowDefinition ?? new RowDefinition(GridLength.Auto));
                    Grid.SetRow(row, grid.Children.Count - 1);
                }
            }
        }
    }

    extension(Control control)
    {
        public static StyledProperty<ColumnDefinition?> ColumnDefinitionProperty => Extensions.ColumnDefinitionProperty;
        public static StyledProperty<RowDefinition?> RowDefinitionProperty => Extensions.RowDefinitionProperty;
        
        public ColumnDefinition? ColumnDefinition
        {
            get => control.GetValue(ColumnDefinitionProperty);
            set => control.SetValue(ColumnDefinitionProperty, value);
        }
        
        public RowDefinition? RowDefinition
        {
            get => control.GetValue(RowDefinitionProperty);
            set => control.SetValue(RowDefinitionProperty, value);
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

