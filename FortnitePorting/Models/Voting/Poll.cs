using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FortnitePorting.Application;

namespace FortnitePorting.Models.Voting;

public partial class Poll : ObservableObject
{
    [ObservableProperty] private string _identifier;
    [ObservableProperty] private string _title;
    [ObservableProperty] private ObservableCollection<PollItem> _items = [];
    [ObservableProperty] private PollItem _selectedItem;

    public bool Submitted => Items.Any(item => item.VotedFor);

    [RelayCommand]
    public async Task Submit()
    {
        await ApiVM.FortnitePorting.PostVoteAsync(Identifier, SelectedItem.Name);
        await VotingVM.RefreshPolls();
    }
}

public partial class PollItem : ObservableObject
{
    [ObservableProperty] private string _name;
    [ObservableProperty] private string? _imageURL;
    [ObservableProperty] private int _votes;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(VotedFor))]
    [NotifyPropertyChangedFor(nameof(Background))]
    private ObservableCollection<Guid> _voters = [];

    public bool VotedFor => AppSettings.Current.Online.Identification is not null && Voters.Contains(AppSettings.Current.Online.Identification.Identifier);
    public SolidColorBrush Background => VotedFor ? SolidColorBrush.Parse("#0FFFFFFF") : SolidColorBrush.Parse("#1E000000");
    
    [ObservableProperty] private bool _isChecked;
}