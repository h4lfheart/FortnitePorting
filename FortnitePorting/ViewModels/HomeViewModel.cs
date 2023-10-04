using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Controls;
using FortnitePorting.Framework;
using FortnitePorting.Services;

namespace FortnitePorting.ViewModels;

public partial class HomeViewModel : ViewModelBase
{
    [ObservableProperty] private ObservableCollection<ChangelogItem> changelogs = new();
    
    public override async Task Initialize()
    {
        var changelogEntries = await EndpointService.FortnitePorting.GetChangelogsAsync();
        if (changelogEntries is null) return;
        
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            foreach (var entry in changelogEntries)
            {
                Changelogs.Add(new ChangelogItem(entry));
            }
        });
    }
}