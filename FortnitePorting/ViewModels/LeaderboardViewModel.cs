using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Framework;
using FortnitePorting.Models.Leaderboard;
using FortnitePorting.Services;

namespace FortnitePorting.ViewModels;

public partial class LeaderboardViewModel : ViewModelBase
{
    public override async Task OnViewOpened()
    {
        AppWM.UpdateChippy([
            "CAN YOU PLEASE EXPORT ME!!!! I NEED TO BE TOP OF THE LEADERBOARD RIGHT NOW!!!",
            "i wish i could export stuff, I would export myself so much",
            "i’m cheering for you from down here!!",
            "woah you’re kinda cracked at this"
        ]);
    }
}