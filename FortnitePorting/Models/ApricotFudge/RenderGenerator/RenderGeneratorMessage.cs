using CommunityToolkit.Mvvm.ComponentModel;

namespace FortnitePorting.Models.ApricotFudge.RenderGenerator;

public partial class RenderGeneratorMessage : ObservableObject
{
    [ObservableProperty] private bool _isUser;
    [ObservableProperty] private bool _isThinking;
    [ObservableProperty] private string _thinkingText = string.Empty;
    [ObservableProperty] private string _text = string.Empty;
    [ObservableProperty] private string _imageUrl = string.Empty;
}