using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Framework;
using FortnitePorting.Shared.Extensions;

namespace FortnitePorting.ViewModels;

public partial class SetupViewModel : ViewModelBase
{
    [ObservableProperty] private ObservableCollection<string> _imagePaths = [];

    public override async Task Initialize()
    {
        var galleryResponse = await Api.FortnitePorting.Gallery();

        ImagePaths =
        [
            ..galleryResponse.FileNames
                .Select(fileName => Path.Combine(galleryResponse.BaseUrl, fileName))
                .Shuffle()
                .ToArray()
        ];
    }
}