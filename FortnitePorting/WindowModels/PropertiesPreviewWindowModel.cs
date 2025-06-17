using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using AvaloniaEdit;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Highlighting.Xshd;
using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Texture;
using FluentAvalonia.Core;
using FortnitePorting.Framework;
using FortnitePorting.Services;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.ViewModels;
using PropertiesContainer = FortnitePorting.Models.Viewers.PropertiesContainer;

namespace FortnitePorting.WindowModels;

public partial class PropertiesPreviewWindowModel(SettingsService settings) : WindowModelBase
{
    [ObservableProperty] private SettingsService _settings = settings;

    [ObservableProperty] private ObservableCollection<PropertiesContainer> _assets = [];
    [ObservableProperty] private PropertiesContainer? _selectedAsset;

    public static IHighlightingDefinition JsonHighlighter { get; set; }

    static PropertiesPreviewWindowModel()
    {
        using var stream = AssetLoader.Open(new Uri("avares://FortnitePorting/Assets/Highlighters/Json.xshd"));
        using var reader = new XmlTextReader(stream);
        JsonHighlighter = HighlightingLoader.Load(reader, HighlightingManager.Instance);
    }
}