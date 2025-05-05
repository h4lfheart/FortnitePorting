using System;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;

namespace FortnitePorting.Models.Article;

public partial class ArticleSection : ObservableObject
{
    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(HasText))]
    [NotifyPropertyChangedFor(nameof(HasFile))]
    private EHelpSectionType _type;
    
    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(ContentUri))]
    [NotifyPropertyChangedFor(nameof(ContentStream))]
    private string _content;
    
    [JsonIgnore] public bool HasText => Type is not EHelpSectionType.Separator;
    [JsonIgnore] public bool HasFile => Type is EHelpSectionType.Image or EHelpSectionType.Gif;
    
    
    [JsonIgnore] public Uri? ContentUri => Type is EHelpSectionType.Gif ? new Uri(Content) : null;
    [JsonIgnore] public Task<MemoryStream>? ContentStream => Type is EHelpSectionType.Gif ? GetContentStream(Content) : null;

    
    public async Task BrowseSectionFile()
    {
        var fileType = Type switch
        {
            EHelpSectionType.Image => Globals.ImageFileType,
            EHelpSectionType.Gif => Globals.GIFFileType
        };
        
        if (await App.BrowseFileDialog(fileTypes: fileType) is { } path)
        {
            Content = path;
        }
    }

    public async Task<MemoryStream> GetContentStream(string url)
    {
        var bytes = await Api.GetBytesAsync(url);
        return new MemoryStream(bytes);
    }

}

public enum EHelpSectionType
{
    Text,
    Heading,
    Image,
    Gif,
    Separator,
    Hyperlink
}