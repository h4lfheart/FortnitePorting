using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DynamicData;
using FortnitePorting.Application;
using FortnitePorting.Framework;
using FortnitePorting.Models.API.Responses;
using FortnitePorting.Models.Voting;
using FortnitePorting.Services;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.ViewModels.Settings;
using Poll = FortnitePorting.Models.Voting.Poll;

namespace FortnitePorting.ViewModels;

public partial class VotingViewModel : ViewModelBase
{
    [ObservableProperty] private SupabaseService _supaBase;

    public VotingViewModel(SupabaseService supabase)
    {
        SupaBase = supabase;
    }
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IncompletePollCount))]
    [NotifyPropertyChangedFor(nameof(IncompletePollNotification))]
    private ObservableCollection<Poll> _polls = [];

    public int IncompletePollCount => Polls.Count(poll => !poll.VotedForPoll);
    public string IncompletePollNotification => $"{IncompletePollCount} {(IncompletePollCount == 1 ? "Poll" : "Polls")} to Complete";
    
    public override async Task Initialize()
    {
        await RefreshPolls();
    }

    public override async Task OnViewOpened()
    {
        await RefreshPolls();
    }

    public async Task RefreshPolls()
    {
        Polls = [..await SupaBase.Client.Rpc<Poll[]>("polls", new { }) ?? []];

        AppWM.UnsubmittedPolls = IncompletePollCount;
    }
}