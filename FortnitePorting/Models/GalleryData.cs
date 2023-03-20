using System.Collections.Generic;
using FortnitePorting.Views.Controls;
using Newtonsoft.Json;

namespace FortnitePorting.Models;

public class GalleryData
{
    public string Name;
    public string ID;
    public string Path;
    public List<string> Props = new();

    [JsonIgnore] public PropExpander Expander;

    public GalleryData(string name, string id, string path)
    {
        Name = name;
        ID = id;
        Path = path;
    }
}