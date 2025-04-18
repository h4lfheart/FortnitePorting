using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using FortnitePorting.Framework;
using FortnitePorting.Models.API.Responses;
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