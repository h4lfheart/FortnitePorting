using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Application;
using FortnitePorting.Framework;
using FortnitePorting.Models.Article;
using FortnitePorting.Models.Assets;
using FortnitePorting.Models.Supabase.Tables;
using FortnitePorting.OnlineServices.Packet;
using FortnitePorting.Services;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Services;
using RestSharp;
using Log = Serilog.Log;

namespace FortnitePorting.ViewModels;

public partial class HelpViewModel(SupabaseService supabase) : ViewModelBase
{
    [ObservableProperty] private SupabaseService _supaBase = supabase;
    
    [ObservableProperty] private ObservableCollection<Article> _articles = [];
    
    [ObservableProperty, NotifyPropertyChangedFor(nameof(BuilderButtonText))] private bool _isBuilderOpen = false;
    public string BuilderButtonText => IsBuilderOpen ? "Open Help Articles List" : "Open Help Article Builder";
    public bool AllowedToOpenBuilder => SupaBase.IsLoggedIn && SupaBase.Permissions.Role >= ESupabaseRole.Staff;
    
    [ObservableProperty] private Article _builderArticle = new();
    [ObservableProperty] private int _selectedSectionIndex = 0;

    public override async Task Initialize()
    {
        await UpdateArticles();
    }

    public async Task UpdateArticles()
    {
        var articles = await SupaBase.Client.From<Article>().Get();
        Articles = [..articles.Models];
    }

    public override async Task OnViewOpened()
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
                    BuilderArticle = new Article();
                    newArticle.Author = SupaBase.UserInfo.DisplayName;

                    if (string.IsNullOrWhiteSpace(newArticle.Title)) newArticle.Title = "Untitled";
                    if (string.IsNullOrWhiteSpace(newArticle.Description)) newArticle.Description = "No Description.";

                    var imageBucket =  SupaBase.Client.Storage.From("article-images");
                    for (var i = 0; i < newArticle.Sections.Count; i++)
                    {
                        var section = newArticle.Sections[i];
                        if (section.Type is not (EHelpSectionType.Image or EHelpSectionType.Gif)) continue;
                        if (section.Content.StartsWith("https://")) continue;
                        
                        var imageFile = new FileInfo(section.Content);

                        var imageName = $"{Guid.NewGuid()}.{imageFile.Extension[1..]}";
                        await imageBucket.Upload(await File.ReadAllBytesAsync(section.Content), imageName);

                        section.Content = imageBucket.GetPublicUrl(imageName);
                    }

                    if (!string.IsNullOrEmpty(newArticle.Id))
                    {
                        await SupaBase.Client.From<Article>().Update(newArticle);
                    }
                    else
                    {
                        await SupaBase.Client.From<Article>().Insert(newArticle);
                    }
                    Info.Message("Uploaded Article", $"Successfully uploaded help article entitled \"{newArticle.Title}\"");
                    
                    await UpdateArticles();
                }),
                CloseButtonText = "Go Back",
            };
            await dialog.ShowAsync();
        });
    }
    
    [RelayCommand]
    public async Task EditArticle(Article article)
    {
        BuilderArticle = article;
        IsBuilderOpen = true;
    }
    
    [RelayCommand]
    public async Task DeleteArticle(Article article)
    {
        await TaskService.RunDispatcherAsync(async () =>
        {
            var dialog = new ContentDialog
            {
                Title = $"Are you sure you would like to delete \"{article.Title}\"?",
                Content = "Please make sure that you would like to delete this article.",
                
                PrimaryButtonText = "Delete",
                PrimaryButtonCommand = new RelayCommand(async () =>
                {
                    await SupaBase.Client.From<Article>().Delete(article);
                    await UpdateArticles();
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
        BuilderArticle.Sections.Add(new ArticleSection());
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