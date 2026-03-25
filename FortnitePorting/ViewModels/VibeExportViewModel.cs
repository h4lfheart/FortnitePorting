using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse.UE4.Assets.Exports;
using FortnitePorting.Exporting;
using FortnitePorting.Framework;
using FortnitePorting.Models.ApricotFudge.GPT;
using FortnitePorting.Services;
using FortnitePorting.Shared.Extensions;

namespace FortnitePorting.ViewModels;

public partial class VibeExportViewModel(SupabaseService supabaseService) : ViewModelBase
{
    [ObservableProperty] private SupabaseService _supaBase = supabaseService;
    
    [ObservableProperty] private TextBox _textBox;

    [ObservableProperty] private bool _isWelcomeVisible = true;

    [ObservableProperty] private string _promptText = string.Empty;
    [ObservableProperty] private bool _isChatEnabled = false;

    [ObservableProperty] private ObservableCollection<GPTMessage> _messages = [];

    public string WelcomeText => $"Hello {SupaBase.UserInfo?.DisplayName ?? "there"}, what do you want to export?";
    

    public async Task SendPrompt()
    {
        var userMessage = new GPTMessage
        {
            IsUser = true,
            Text = PromptText
        };

        Messages.Add(userMessage);
        PromptText = string.Empty;

        var gptMessage = new GPTMessage
        {
            IsUser = false,
            IsThinking = true
        };
        Messages.Add(gptMessage);

        gptMessage.IsThinking = true;
        IsChatEnabled = false;

        var cheese = await UEParse.Provider.LoadPackageObjectAsync(
            "/CR_Legacy/Creative/Maps/Prefabs/Props/PPIDs/PPID_CR_Legacy_Creative_Prop_CheeseSlice01_61868c3c");
        
        bool exported;
        try
        {
            exported = await Exporter.Export(cheese, EExportType.Prop, AppSettings.ExportSettings.CreateExportMeta());
        }
        catch (Exception)
        {
            exported = false;
        }

        await Task.Delay(1000);
        gptMessage.IsThinking = false;
        
        var targetText = exported ? "Exported!" : "Failed to export.";
        while (targetText.Length > 0)
        {
            gptMessage.Text += targetText[0];
            targetText = targetText[1..];
            await Task.Delay(10);
        }

        IsChatEnabled = true;
        await TaskService.RunDispatcherAsync(() =>
        {
            TextBox.Focus();
        });
        
    }
}