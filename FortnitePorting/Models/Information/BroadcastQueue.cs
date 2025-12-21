using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FortnitePorting.Models.Information;

public partial class BroadcastQueue : ObservableObject
{
    [ObservableProperty] private BroadcastData? _current;
    
    private readonly Queue<BroadcastData> _queue = new();
    private readonly object _queueLock = new();

    public void Enqueue(BroadcastData broadcast)
    {
        lock (_queueLock)
        {
            _queue.Enqueue(broadcast);
            
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
            }
        }
    }
}