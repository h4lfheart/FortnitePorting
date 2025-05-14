using System;
using System.Collections.Generic;
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
    
    [ObservableProperty, NotifyPropertyChangedFor(nameof(BackgroundBlur)), NotifyPropertyChangedFor(nameof(BackgroundOpacity))] private bool _useBlur = false;

    public BlurEffect BackgroundBlur => new()
    {
        Radius = UseBlur ? 20 : 3
    };

    public float BackgroundOpacity => UseBlur ? 0.1f : 0.25f;

    [ObservableProperty] private ObservableCollection<string[]> _galleryPaths = new(new List<string[]>(3));

    public override async Task Initialize()
    {
        var bucket = SupaBase.Client.Storage.From("gallery-images");
        var images = await bucket.List();
        if (images is null) return;
        
        var imagePaths = images.Select(image => bucket.GetPublicUrl(image.Name!)).ToList();
        imagePaths.Shuffle();

        GalleryPaths = [..imagePaths.Chunk(Math.Max(1, (int) Math.Ceiling(images.Count / 3.0f)))];
    }
}