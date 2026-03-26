using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Controls;
using FortnitePorting.Framework;
using FortnitePorting.Models.ApricotFudge.GPT;
using FortnitePorting.Models.ApricotFudge.RenderGenerator;
using FortnitePorting.Services;
using FortnitePorting.Shared.Extensions;

namespace FortnitePorting.ViewModels;

public partial class RenderGeneratorViewModel(SupabaseService supabaseService) : ViewModelBase
{
    [ObservableProperty] private SupabaseService _supaBase = supabaseService;

    [ObservableProperty] private DynamicFadeScrollViewer _scroll;
    [ObservableProperty] private TextBox _textBox;

    [ObservableProperty] private bool _isWelcomeVisible = true;

    [ObservableProperty] private string _promptText = string.Empty;
    [ObservableProperty] private bool _isChatEnabled = false;

    [ObservableProperty] private ObservableCollection<RenderGeneratorMessage> _messages = [];

    public string WelcomeText => $"Hello {SupaBase.UserInfo?.DisplayName ?? "there"}, what do you want to create?";

    private List<string> _imageUrls = [];

    private readonly string[] _textResponses =
    [
        "this one is actually insane 🔥",
        "lowkey masterpiece",
        "we cooked",
        "this goes unbelievably hard",
        "frame this immediately",
        "AI peaked here",
        "museum worthy tbh",
        "this might be the best one yet",
        "epic would approve",
        "this didn’t make it past QA",
        "close enough 👍",
        "someone got fired for this",
        "this looked better in my head",
        "trust the process (please)",
        "we’re just gonna go with it"
    ];

    public override async Task OnViewOpened()
    {
        AppWM.UpdateChippy([
            "i've made some bangers on here",
            "lots of great cat renders made here",
            "im basically an artist y'know",
            "this one looks kinda fire not gonna lie",
            "boom!! masterpiece!! probably!!"
        ]);
    }


    public override async Task Initialize()
    {
        _imageUrls = await Api.FortnitePorting.RenderGeneratorImages();
    }

    public async Task SendPrompt()
    {
        var userMessage = new RenderGeneratorMessage()
        {
            IsUser = true,
            Text = PromptText
        };

        Messages.Add(userMessage);
        PromptText = string.Empty;

        var generatorMessage = new RenderGeneratorMessage
        {
            IsUser = false,
            IsThinking = true
        };
        Messages.Add(generatorMessage);

        generatorMessage.IsThinking = true;
        IsChatEnabled = false;

        generatorMessage.ThinkingText = "Thinking...";
        await Task.Delay(Random.Shared.Next(1500, 3000));

        generatorMessage.ThinkingText = "Processing Nodes...";
        await Task.Delay(Random.Shared.Next(1500, 3000));

        if (Random.Shared.NextDouble() < 0.1)
        {
            generatorMessage.ThinkingText = "Unhandled exception.";
            await Task.Delay(1200);

            generatorMessage.ThinkingText = "Just Kidding";
            await Task.Delay(1500);
        }

        int progress = 0;
        while (progress < 100)
        {
            progress += Random.Shared.Next(4, 12);

            var display = Math.Min(progress, 100);

            generatorMessage.ThinkingText = $"Rendering... {display}%";
            await Task.Delay(Random.Shared.Next(120, 350));
        }

        await Task.Delay(750);

        generatorMessage.ThinkingText = "Diffusing Image...";
        await Task.Delay(Random.Shared.Next(2500, 5000));

        generatorMessage.ImageUrl = _imageUrls.Random()!;
        generatorMessage.IsThinking = false;
        generatorMessage.Text = _textResponses.Random()!;
        IsChatEnabled = true;
        await TaskService.RunDispatcherAsync(async () =>
        {
            TextBox.Focus();
            await Task.Delay(500);
            Scroll.ScrollToEnd();
        });
    }
}