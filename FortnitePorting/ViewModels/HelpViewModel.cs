using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Framework;
using FortnitePorting.Models.Article;
using FortnitePorting.Models.Information;
using FortnitePorting.Models.Supabase.Tables;
using FortnitePorting.Services;
using FortnitePorting.Shared.Extensions;
using ReactiveUI;

namespace FortnitePorting.ViewModels;

public partial class HelpViewModel : ViewModelBase
{
    [ObservableProperty] private SupabaseService _supaBase;

    [ObservableProperty] private string _searchFilter = string.Empty;
    [ObservableProperty] private ReadOnlyObservableCollection<Article> _articles = new([]);

    [ObservableProperty, NotifyPropertyChangedFor(nameof(BuilderButtonText))] private bool _isBuilderOpen = false;
    public string BuilderButtonText => IsBuilderOpen ? "Open Help Articles List" : "Open Help Article Builder";
    public bool AllowedToOpenBuilder => SupaBase is { IsLoggedIn: true, Permissions.Role: >= ESupabaseRole.Support };

    [ObservableProperty] private Article _builderArticle = new();
    [ObservableProperty] private int _selectedSectionIndex = 0;

    private SourceCache<Article, string> _articleCache = new(x => x.Id);

    public HelpViewModel(SupabaseService supabase) : this()
    {
        SupaBase = supabase;
    }

    private HelpViewModel()
    {
        var filterObservable = this
            .WhenAnyValue(vm => vm.SearchFilter)
            .Select(CreateFilter);

        _articleCache.Connect()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Filter(filterObservable)
            .Bind(out var readOnlyArticles)
            .Subscribe();

        Articles = readOnlyArticles;
    }

    public override async Task Initialize()
    {
        await UpdateArticles();
    }

    public async Task UpdateArticles()
    {
        var response = await Api.FortnitePorting.GetArticles();
        if (response is null) return;

        var articles = response.Entries.Select(e => new Article
        {
            Id = e.Id,
            Author = e.Author,
            Timestamp = e.Timestamp,
            Title = e.Title,
            Description = e.Description,
            Tag = e.Tag,
            Sections = new ObservableCollection<ArticleSection>(
                e.Sections.Select(s => new ArticleSection { Type = s.Type, Content = s.Content }))
        }).ToList();

        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            _articleCache.Edit(updater =>
            {
                updater.Clear();
                updater.AddOrUpdate(articles);
            });
        });
    }

    public override async Task OnViewOpened()
    {
        await UpdateArticles();
    }

    [RelayCommand]
    public async Task Upload()
    {
        Info.Dialog("Are you sure you would like to upload?", "Please make sure that all of these changes are final before uploading your article.", buttons:
        [
            new DialogButton
            {
                Text = "Upload",
                Action = async () =>
                {
                    var newArticle = BuilderArticle;
                    BuilderArticle = new Article();
                    newArticle.Author = SupaBase.UserInfo.DisplayName;

                    if (string.IsNullOrWhiteSpace(newArticle.Title)) newArticle.Title = "Untitled";
                    if (string.IsNullOrWhiteSpace(newArticle.Description)) newArticle.Description = "No Description.";

                    for (var i = 0; i < newArticle.Sections.Count; i++)
                    {
                        var section = newArticle.Sections[i];
                        if (section.Type is not (EHelpSectionType.Image or EHelpSectionType.Gif)) continue;
                        if (section.Content.StartsWith("https://")) continue;

                        var imageFile = new FileInfo(section.Content);
                        var bytes = await File.ReadAllBytesAsync(section.Content);
                        var fileName = $"{Guid.NewGuid()}{imageFile.Extension}";

                        var uploaded = await Api.FortnitePorting.UploadArticleImage(bytes, fileName);
                        if (uploaded is not null)
                            section.Content = uploaded.Path;
                    }

                    var request = new
                    {
                        title = newArticle.Title,
                        description = newArticle.Description,
                        tag = newArticle.Tag,
                        author = newArticle.Author,
                        sections = newArticle.Sections.Select(s => new { type = s.Type, content = s.Content })
                    };

                    if (!string.IsNullOrEmpty(newArticle.Id))
                        await Api.FortnitePorting.UpdateArticle(newArticle.Id, request);
                    else
                        await Api.FortnitePorting.CreateArticle(request);

                    Info.Message("Uploaded Article", $"Successfully uploaded help article entitled \"{newArticle.Title}\"");

                    await UpdateArticles();
                }
            }
        ]);
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
        Info.Dialog($"Are you sure you would like to delete \"{article.Title}\"?",
            "Please make sure that you would like to delete this article.", buttons:
            [
                new DialogButton
                {
                    Text = "Delete",
                    Action = async () =>
                    {
                        await Api.FortnitePorting.DeleteArticle(article.Id);
                        await UpdateArticles();
                    }
                }
            ]);
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

    private Func<Article, bool> CreateFilter(string filter)
    {
        return article => MiscExtensions.Filter(article.Title, filter)
                          || MiscExtensions.Filter(article.Description, filter)
                          || MiscExtensions.Filter(article.Tag, filter);
    }
}
