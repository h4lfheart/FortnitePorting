using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Views.Controls;
using FortnitePorting.Views.Extensions;

namespace FortnitePorting.ViewModels;

public partial class MusicViewModel : ObservableObject
{
    public Visibility QueueVisibility => Queue.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    public Visibility FallbackVisibility => ActiveTrack is null ? Visibility.Visible : Visibility.Collapsed;
    
    [ObservableProperty]
    private bool isPaused;
    
    [ObservableProperty]
    private bool isRandom;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FallbackVisibility))]
    private MusicQueueItem? activeTrack;
    
    [ObservableProperty]
    private MusicQueueItem? fallbackTrack = AppVM.CUE4ParseVM.PlaceholderMusicPack;

    [ObservableProperty]
    private ObservableCollection<MusicQueueItem> queue = new();
    
    public void Add(MusicQueueItem queueItem)
    {
        Queue.Add(queueItem);
        if (ActiveTrack is null)
        {
            ContinueQueue();
        }
        
        OnPropertyChanged(nameof(QueueVisibility));
    }

    public void ContinueQueue()
    {
        ActiveTrack?.Dispose();

        MusicQueueItem? nextQueueItem = null;
        if (Queue.Count == 0)
        {
            if (IsRandom)
            {
                nextQueueItem = new MusicQueueItem(AppVM.AssetHandlerVM.Handlers[EAssetType.Music].TargetCollection!.Random());
            }
            else
            {
                ActiveTrack = null;
                return;
            }
        }
        
        nextQueueItem ??= IsRandom ? Queue.Random() : Queue.First();
        ActiveTrack = nextQueueItem;
        ActiveTrack.Initialize();
        Queue.Remove(nextQueueItem);
        OnPropertyChanged(nameof(QueueVisibility));
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