using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CUE4Parse.Utils;
using FortnitePorting.Extensions;

namespace FortnitePorting.Models.Files;

public partial class TreeItem : ObservableObject
{
    [ObservableProperty, NotifyPropertyChangedFor(nameof(NameWithoutExtension))]
    private string _name;

    public string NameWithoutExtension => Name.SubstringBefore(".");
    public string Extension => Name.SubstringAfterLast(".");

    [ObservableProperty] private string _filePath;
    [ObservableProperty] private ENodeType _type;

    [ObservableProperty] private bool _selected;
    [ObservableProperty] private bool _expanded;

    [ObservableProperty] private Bitmap? _fileBitmap;
    [ObservableProperty] private string? _exportType;

    [ObservableProperty] private TreeItem? _parent;

    [ObservableProperty, NotifyPropertyChangedFor(nameof(TotalChildCount))]
    private int _fileChildCount;

    [ObservableProperty, NotifyPropertyChangedFor(nameof(TotalChildCount))]
    private int _folderChildCount;

    public int TotalChildCount => FileChildCount + FolderChildCount;

    [ObservableProperty] private ObservableCollection<TreeItem> _folderChildren = [];

    public bool HasFolders => FolderChildCount > 0;

    public FileNode? SourceNode { get; }

    private readonly Action<TreeItem>? _onExpand;

    public TreeItem(string name, ENodeType type, string filePath = "", TreeItem? parent = null,
        FileNode? sourceNode = null, Action<TreeItem>? onExpand = null)
    {
        Name = name;
        Type = type;
        FilePath = filePath;
        Parent = parent;
        SourceNode = sourceNode;
        _onExpand = onExpand;

        FileChildCount = sourceNode?.FileChildCount ?? 0;
        FolderChildCount = sourceNode?.FolderChildCount ?? 0;
    }

    public void AddFolderChild(TreeItem child)
    {
        FolderChildren.Add(child);
    }

    public bool TryGetFolderChild(string name, out TreeItem child)
    {
        child = FolderChildren.FirstOrDefault(x => x.Name == name)!;
        return child is not null;
    }

    partial void OnExpandedChanged(bool value)
    {
        if (!value) return;
        _onExpand?.Invoke(this);
    }

    [RelayCommand]
    public async Task CopyPath(bool withoutExtension = false)
    {
        await App.Clipboard.SetTextAsync(withoutExtension ? FilePath.SubstringBefore(".") : FilePath);
    }
}

public enum ENodeType
{
    Folder,
    File
}