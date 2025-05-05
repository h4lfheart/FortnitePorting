using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FortnitePorting.Application;
using Newtonsoft.Json;

namespace FortnitePorting.Models.Voting;

public partial class Poll : ObservableObject
{
    [JsonProperty("poll_id")] public string PollId;
    
    [ObservableProperty] [JsonProperty("title")] private string _title;

    [ObservableProperty] [JsonProperty("options")] [NotifyPropertyChangedFor(nameof(VotedForPoll))]
    private ObservableCollection<PollItem> _options = [];

    public bool VotedForPoll => Options.Any(option => option.Voted);
    
    [ObservableProperty] private PollItem? _selectedItem;
    
    public async Task Submit()
    {
        if (SelectedItem is null) return;
        
        await SupaBase.Client.Rpc("vote_poll", new
        {
            id = PollId,
            option = SelectedItem.Text
        });
        
        await VotingVM.RefreshPolls();
    }
}

public partial class PollItem : ObservableObject
{
    [ObservableProperty] [JsonProperty("text")] private string _text;
    [ObservableProperty] [JsonProperty("image_url")] private string? _imageURL;
    [ObservableProperty] [JsonProperty("votes")] private int _votes;

    [ObservableProperty, NotifyPropertyChangedFor(nameof(Background))] [JsonProperty("did_user_vote")]
    private bool _voted;
    
    public SolidColorBrush Background => Voted ? SolidColorBrush.Parse("#0FFFFFFF") : SolidColorBrush.Parse("#1E000000");
    
    [ObservableProperty] private bool _isChecked;
}