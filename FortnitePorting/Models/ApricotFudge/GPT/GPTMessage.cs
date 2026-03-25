using CommunityToolkit.Mvvm.ComponentModel;

namespace FortnitePorting.Models.ApricotFudge.GPT;

public partial class GPTMessage : ObservableObject
{
    [ObservableProperty] private bool _isUser;
    [ObservableProperty] private bool _isThinking;
    [ObservableProperty] private string text;
}