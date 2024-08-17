using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using FortnitePorting.Models.API.Responses;
using FortnitePorting.Shared.Framework;
using FortnitePorting.ViewModels;
using Serilog;
using Poll = FortnitePorting.Models.Voting.Poll;
using PollItem = FortnitePorting.Models.Voting.PollItem;

namespace FortnitePorting.Views;

public partial class VotingView : ViewBase<VotingViewModel>
{
    public VotingView()
    {
        InitializeComponent();
    }
}