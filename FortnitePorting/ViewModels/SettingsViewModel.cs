using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CUE4Parse.UE4.Versions;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Application;
using FortnitePorting.Framework;
using FortnitePorting.Models.API;
using FortnitePorting.Models.API.Responses;
using FortnitePorting.Models.Radio;
using FortnitePorting.Services;
using FortnitePorting.Shared;
using FortnitePorting.ViewModels.Settings;
using NAudio.Wave;
using Newtonsoft.Json;
using RestSharp;

namespace FortnitePorting.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    public override async Task OnViewExited()
    {
        if (AppSettings.ShouldSaveOnExit)
            AppSettings.Save();
    }
    
}