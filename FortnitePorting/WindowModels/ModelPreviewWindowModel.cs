using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse.UE4.Assets.Exports;
using FortnitePorting.Application;
using FortnitePorting.Framework;
using FortnitePorting.Services;

namespace FortnitePorting.WindowModels;

[Transient]
public partial class ModelPreviewWindowModel(SettingsService settings) : WindowModelBase
{
    [ObservableProperty] private SettingsService _settings = settings;
    
}