using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Views.Controls;

namespace FortnitePorting.ViewModels;

public partial class MusicViewModel : ObservableObject
{
    [ObservableProperty] 
    private bool isPaused;
    
    [ObservableProperty] 
    private MusicQueueItem? activeTrack;

    [ObservableProperty] 
    private ObservableCollection<MusicQueueItem> queue = new();

    public void Add(MusicQueueItem queueItem)
    {
        Queue.Add(queueItem);
        if (ActiveTrack is null)
        {
            ContinueQueue();
        }
    }

    public void ContinueQueue()
    {
        ActiveTrack?.Dispose();
        
        var nextQueueItem = Queue.FirstOrDefault();
        if (nextQueueItem is null)
        {
            ActiveTrack = null;
            return;
        }
        
        Queue.RemoveAt(0);
        ActiveTrack = nextQueueItem;
        ActiveTrack.Initialize();
    }

    public void Pause()
    {
        ActiveTrack?.Pause();
    }
    
    public void Resume()
    {
        ActiveTrack?.Resume();
    }
}