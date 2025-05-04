using System;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CUE4Parse.Utils;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Services;
using Newtonsoft.Json;

namespace FortnitePorting.Models.Help;

public partial class HelpSection : ObservableObject
{
    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(HasText))]
    [NotifyPropertyChangedFor(nameof(HasFile))]
    private EHelpSectionType _type;
    
    [JsonIgnore] public bool HasText => Type is not EHelpSectionType.Separator;
    [JsonIgnore] public bool HasFile => Type is EHelpSectionType.Image or EHelpSectionType.Gif;
    
    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(ContentUri))]
    [NotifyPropertyChangedFor(nameof(ContentStream))]
    private string _content;
    
    [JsonIgnore] public Uri? ContentUri => Type is EHelpSectionType.Gif ? new Uri(Content) : null;
    [JsonIgnore] public Task<MemoryStream> ContentStream => Type is EHelpSectionType.Gif ? GetContentStream(Content) : null;

    
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
        /*var fileName = url.SubstringAfterLast("/");
        var filePath = Path.Combine(App.CacheFolder.FullName, fileName);
        if (File.Exists(filePath))
        {
            return new MemoryStream(await File.ReadAllBytesAsync(filePath));

        }*/

        var bytes = await Api.GetBytesAsync(url);
        //await File.WriteAllBytesAsync(filePath, bytes);
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