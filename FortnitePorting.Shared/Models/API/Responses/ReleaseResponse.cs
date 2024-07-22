using CommunityToolkit.Mvvm.ComponentModel;

namespace FortnitePorting.Shared.Models.API.Responses;

public class ReleaseResponse : ObservableObject
{
    public FPVersion Version { get; set; }
    public string Download { get; set; }
    public string Changelog { get; set; }
    public bool IsMandatory { get; set; }
    public List<ReleaseDependency> Dependencies { get; set; }
}

public class ReleaseDependency
{
    public string Name { get; set; }
    public string URL { get; set; }
}
