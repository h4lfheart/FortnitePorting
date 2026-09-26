using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FortnitePorting.Models.Information;

public partial class DialogQueue : ObservableObject
{
    [ObservableProperty] private DialogData? _current;
    
    private readonly Queue<DialogData> _queue = new();
    private readonly object _queueLock = new();

    public void Enqueue(DialogData dialog)
    {
        lock (_queueLock)
        {
            _queue.Enqueue(dialog);
            
            if (Current == null)
            {
                ShowNext();
            }
        }
    }

    public async Task Close()
    {
        if (Current is null) return;

        Current.IsOpen = false;
        
        lock (_queueLock)
        {
            ShowNext();
        }
    }

    private void ShowNext()
    {
        lock (_queueLock)
        {
            if (_queue.Count > 0)
            {
                Current = _queue.Dequeue();
                Current.IsOpen = true;
            }
            else
            {
                Current?.IsOpen = false;
                Current = null;
            }
        }
    }
}