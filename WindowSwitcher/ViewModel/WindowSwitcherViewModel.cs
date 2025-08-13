using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using WindowSwitcher.Bindings;
using WindowSwitcher.Services;

namespace WindowSwitcher.ViewModel;

public class WindowSwitcherViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
    
    public ObservableCollection<WindowEntry> AllWindows { get; } = [];
    public ObservableCollection<WindowEntry> FilteredWindows { get; } = [];

    public string SearchTerm
    {
        get;
        set
        {
            if (SetField(ref field, value))
            {
                ApplyFilter();
            }
        }
    } = string.Empty;

    private readonly IWindowService _windowService;

    public static readonly FrozenSet<string> BlacklistedProcessNames = new HashSet<string>()
    {
        "ApplicationFrameHost",
        "SystemSettings",
        "TextInputHost",
    }.ToFrozenSet();

    public WindowSwitcherViewModel(IWindowService windowService)
    {
        _windowService = windowService;
        AllWindows.CollectionChanged += (_, _) => ApplyFilter();
        ApplyFilter();
    }
    
    public void RequestToActivateWindow(WindowEntry windowEntry)
    {
        _windowService.ActivateWindow(windowEntry.Info.Handle);
    }
    
    private void ApplyFilter()
    {
        string query = SearchTerm;
        IEnumerable<WindowEntry> matches = AllWindows.Where(x => x.Info.ProcessName.StartsWith(query, StringComparison.InvariantCultureIgnoreCase));

        FilteredWindows.Clear();
        foreach (WindowEntry item in matches)
        {
            FilteredWindows.Add(item);
        }

        // if (query != string.Empty) //TODO:
        // {
        //     if (!((IEnumerable<WindowEntry>)_resultsList.ItemsSource).Contains(_resultsList.SelectedItem as WindowEntry))
        //     {
        //         _resultsList.SelectedIndex = 0;
        //     }
        // }
    }
    
    public void FetchWindows()
    {
        // fetch available windows
        new Thread(() =>
        {
            WindowEntry[] windowEntries = _windowService.GetWindows()
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
                AllWindows.Clear();
                foreach (WindowEntry windowEntry in windowEntries)
                {
                    AllWindows.Add(windowEntry);
                }
            });
        })
        {
            IsBackground = true,
        }.Start();
    }
}