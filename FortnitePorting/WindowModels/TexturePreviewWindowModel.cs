using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Application;
using FortnitePorting.Framework;
using FortnitePorting.Models.Viewers;
using FortnitePorting.Services;

namespace FortnitePorting.WindowModels;

[Transient]
public partial class TexturePreviewWindowModel(SettingsService settings) : WindowModelBase
{
    [ObservableProperty] private SettingsService _settings = settings;

    [ObservableProperty] private ObservableCollection<TextureContainer> _textures = [];
    [ObservableProperty] private TextureContainer _selectedTexture;

}