using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Application;

using FortnitePorting.Export;
using FortnitePorting.Export.Models;
using FortnitePorting.Framework;
using FortnitePorting.Models.Assets;
using FortnitePorting.Models.Help;
using FortnitePorting.OnlineServices.Packet;
using FortnitePorting.Services;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Services;
using RestSharp;
using Log = Serilog.Log;

namespace FortnitePorting.ViewModels;

public partial class HelpViewModel : ViewModelBase
{
    [ObservableProperty] private ObservableCollection<HelpArticle> _articles = [];
    
    [ObservableProperty, NotifyPropertyChangedFor(nameof(AllowedToOpenBuilder))] private EPermissions _permissions = EPermissions.None;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(BuilderButtonText))] private bool _isBuilderOpen = false;
    public string BuilderButtonText => IsBuilderOpen ? "Open Help Articles List" : "Open Help Article Builder";
    public bool AllowedToOpenBuilder => AppSettings.Current.Online.UseIntegration && Permissions.HasFlag(EPermissions.Staff);
    
    [ObservableProperty] private HelpArticle _builderArticle = new();
    [ObservableProperty] private int _selectedSectionIndex = 0;

    public override async Task Initialize()
    {
        await UpdateArticles();
    }

    public async Task UpdateArticles()
    {
        var helpArticles = await ApiVM.FortnitePorting.GetHelpAsync();
        if (helpArticles is not null) Articles = [..helpArticles];
    }

    [RelayCommand]
    public async Task Refresh()
    {
        await UpdateArticles();
    }
    
    [RelayCommand]
    public async Task Upload()
    {
        TaskService.RunDispatcher(async () =>
        {
            var dialog = new ContentDialog
            {
                Title = "Are you sure you would like to upload?",
                Content = "Please make sure that all of these changes are final before uploading your article.",
                
                PrimaryButtonText = "Upload",
                PrimaryButtonCommand = new RelayCommand(async () =>
                {
                    var newArticle = BuilderArticle;
                    BuilderArticle = new HelpArticle();
                    newArticle.Author ??= AppSettings.Current.Online.GlobalName;
                    newArticle.PostTime = DateTime.UtcNow;

                    if (string.IsNullOrWhiteSpace(newArticle.Title)) newArticle.Title = "Untitled";
                    if (string.IsNullOrWhiteSpace(newArticle.Description)) newArticle.Description = "No Description.";

                    for (var i = 0; i < newArticle.Sections.Count; i++)
                    {
                        var section = newArticle.Sections[i];
                        if (section.Type is not (EHelpSectionType.Image or EHelpSectionType.Gif)) continue;
                        if (section.Content.StartsWith("https://")) continue;
                        
                        var imageFile = new FileInfo(section.Content);
                        var newImageUrl =
                            await ApiVM.FortnitePorting.PostHelpImageAsync(await File.ReadAllBytesAsync(section.Content),
                                $"image/{imageFile.Extension[1..]}");

                        section.Content = newImageUrl;
                    }

                    await ApiVM.FortnitePorting.PostHelpAsync(newArticle);
                    AppWM.Message("Uploaded Article", $"Successfully uploaded help article entitled \"{newArticle.Title}\"");
                    
                    await UpdateArticles();
                }),
                CloseButtonText = "Go Back",
            };
            await dialog.ShowAsync();
        });
    }
    
    [RelayCommand]
    public async Task EditArticle(HelpArticle article)
    {
        BuilderArticle = article;
        IsBuilderOpen = true;
    }
    
    [RelayCommand]
    public async Task DeleteArticle(string title)
    {
        await TaskService.RunDispatcherAsync(async () =>
        {
            var dialog = new ContentDialog
            {
                Title = $"Are you sure you would like to delete \"{title}\"?",
                Content = "Please make sure that you would like to delete this article.",
                
                PrimaryButtonText = "Delete",
                PrimaryButtonCommand = new RelayCommand(async () =>
                {
                    await ApiVM.FortnitePorting.DeleteHelpAsync(title);
                    await HelpVM.UpdateArticles();
                }),
                CloseButtonText = "Go Back",
            };
            await dialog.ShowAsync();
        });
    }

    [RelayCommand]
    public async Task ToggleBuilder()
    {
        IsBuilderOpen = !IsBuilderOpen;
    }
    
    [RelayCommand]
    public async Task AddSection()
    {
        BuilderArticle.Sections.Add(new HelpSection());
    }
    
    [RelayCommand]
    public async Task RemoveSection()
    {
        var selectedIndexToRemove = SelectedSectionIndex;
        BuilderArticle.Sections.RemoveAt(selectedIndexToRemove);
        SelectedSectionIndex = selectedIndexToRemove == 0 ? 0 : selectedIndexToRemove - 1;
    }
    
    [RelayCommand]
    public async Task MoveUp()
    {
        if (SelectedSectionIndex == 0) return;
        
        var selectedIndexToMove = SelectedSectionIndex;
        BuilderArticle.Sections.Move(SelectedSectionIndex, SelectedSectionIndex - 1);
        SelectedSectionIndex = selectedIndexToMove - 1;
    } 
    
    [RelayCommand]
    public async Task MoveDown()
    {
        if (SelectedSectionIndex == BuilderArticle.Sections.Count - 1) return;
        
        var selectedIndexToMove = SelectedSectionIndex;
        BuilderArticle.Sections.Move(SelectedSectionIndex, SelectedSectionIndex + 1);
        SelectedSectionIndex = selectedIndexToMove + 1;
    }
}