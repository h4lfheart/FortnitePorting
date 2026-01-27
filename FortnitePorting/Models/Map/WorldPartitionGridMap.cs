using System;
using System.Threading.Tasks;
using System.Windows.Forms.VisualStyles;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CUE4Parse.Utils;

namespace FortnitePorting.Models.Map;

public partial class WorldPartitionGridMap : ObservableObject
{
    [ObservableProperty, NotifyPropertyChangedFor(nameof(Name))] private string _path;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(BackgroundBrush))] private EWorldPartitionGridMapStatus _status;
    public string Name => Path.SubstringBeforeLast(".").SubstringAfterLast("/");

    public SolidColorBrush BackgroundBrush => Status switch
    {
        EWorldPartitionGridMapStatus.None => SolidColorBrush.Parse("#0DFFFFFF"),
        EWorldPartitionGridMapStatus.Waiting => SolidColorBrush.Parse("#80FF0000"),
        EWorldPartitionGridMapStatus.Exporting => SolidColorBrush.Parse("#80FFBA00"),
        EWorldPartitionGridMapStatus.Finished => SolidColorBrush.Parse("#8000FF00")
    };

    public WorldPartitionGridMap(string path)
    {
        Path = path;
    }
    
    public async Task CopyID()
    {
        await Clipboard.SetTextAsync(Name);
    }
}

public enum EWorldPartitionGridMapStatus
{
    None,
    Waiting,
    Exporting,
    Finished
}