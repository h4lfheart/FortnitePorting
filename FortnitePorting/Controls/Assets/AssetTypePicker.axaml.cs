using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.VisualTree;
using FortnitePorting.Framework.Extensions;
using FortnitePorting.Services;

namespace FortnitePorting.Controls.Assets;

public partial class AssetTypePicker : UserControl
{

    public static readonly StyledProperty<IImage> ImageProperty = AvaloniaProperty.Register<ImageText, IImage>(nameof(Image));

    public IImage Image
    {
        get => GetValue(ImageProperty);
        set => SetValue(ImageProperty, value);
    }
    
    public static readonly StyledProperty<EAssetType> TypeProperty = AvaloniaProperty.Register<ImageText, EAssetType>(nameof(Type));

    public EAssetType Type
    {
        get => GetValue(TypeProperty);
        set => SetValue(TypeProperty, value);
    }

    public AssetTypePicker()
    {
        InitializeComponent();
    }
    
    private async void OnToggleButtonClick(object? sender, RoutedEventArgs e)
    {
        if (AssetsVM.CurrentLoader is null) return;
        if (AssetsVM.CurrentLoader.Type == Type) return;

        DiscordService.Update(Type);
        AssetsVM.SetLoader(Type);
        foreach (var expander in AssetsVM.ExpanderContainer.GetVisualDescendants().OfType<Expander>())
        {
            expander.IsExpanded = false;
        }

        var loaders = AssetsVM.Loaders;
        foreach (var loader in loaders)
            if (loader.Type == Type)
                loader.Pause.Unpause();
            else
                loader.Pause.Pause();

        await AssetsVM.CurrentLoader.Load();
    }
}