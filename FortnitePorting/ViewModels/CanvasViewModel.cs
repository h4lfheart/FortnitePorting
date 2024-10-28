using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using FortnitePorting.Application;
using FortnitePorting.Models.API.Responses;
using FortnitePorting.Models.Place;
using FortnitePorting.Models.Voting;
using FortnitePorting.OnlineServices.Models;
using FortnitePorting.OnlineServices.Packet;
using FortnitePorting.Services;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Shared.Framework;
using FortnitePorting.ViewModels.Settings;
using Color = Avalonia.Media.Color;
using Poll = FortnitePorting.Models.Voting.Poll;

namespace FortnitePorting.ViewModels;

public partial class CanvasViewModel : ViewModelBase
{
    [ObservableProperty, NotifyPropertyChangedFor(nameof(Brush))] private Color _color;
    public SolidColorBrush Brush => new(Color);
    
    [ObservableProperty] private ushort _X;
    [ObservableProperty] private ushort _Y;

    [ObservableProperty] private WriteableBitmap _bitmap;
    [ObservableProperty] private Image _bitmapSource;
    
    public OnlineSettingsViewModel OnlineRef => AppSettings.Current.Online;

    public override async Task OnViewOpened()
    {
        await OnlineService.Send([], EPacketType.PlaceCanvasInfo);
    }
}