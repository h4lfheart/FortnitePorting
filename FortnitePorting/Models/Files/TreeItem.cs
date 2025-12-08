using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CUE4Parse.Utils;
using FortnitePorting.Extensions;
using Serilog;

namespace FortnitePorting.Models.Files;

public partial class TreeItem : ObservableObject
{
    [ObservableProperty, NotifyPropertyChangedFor(nameof(NameWithoutExtension))] private string _name;
    public string NameWithoutExtension => Name.SubstringBefore(".");
    
    [ObservableProperty] private string _filePath;
    [ObservableProperty] private ENodeType _type;
    
    [ObservableProperty] private bool _selected;
    [ObservableProperty] private bool _expanded;
    
    [ObservableProperty] private Bitmap? _fileBitmap;
    [ObservableProperty] private string? _exportType;

    [ObservableProperty] private TreeItem? _parent;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(TotalChildCount))] private int _fileChildCount = 0;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(TotalChildCount))] private int _folderChildCount = 0;

    public int TotalChildCount => FileChildCount + FolderChildCount;
    
    [ObservableProperty] private ObservableCollection<TreeItem> _folderChildren = [];
    
    public bool HasFolders => _childrenLookup.Values.Any(x => x.Type == ENodeType.Folder);
    
    private Dictionary<string, TreeItem> _childrenLookup = new();
    
    private bool _isSorted;
    private bool _childrenLoaded;

    public TreeItem(string name, ENodeType type, string filePath = "", TreeItem? parent = null)
    {
        Name = name;
        Type = type;
        FilePath = filePath;
        Parent = parent;
    }

    public void AddChild(string name, TreeItem child)
    {
        _childrenLookup[name] = child;
        _isSorted = false;
        _childrenLoaded = false;

        if (child.Type is ENodeType.Folder)
            FolderChildCount++;
        else
            FileChildCount++;
    }

    public bool TryGetChild(string name, out TreeItem child)
    {
        return _childrenLookup.TryGetValue(name, out child);
    }

    public IEnumerable<TreeItem> GetAllChildren()
    {
        EnsureChildrenSorted();
        return _childrenLookup.Values;
    }

    public void EnsureChildrenSorted()
    {
        if (!_isSorted)
        {
            _childrenLookup = _childrenLookup
                .OrderByDescending(item => item.Value.Type == ENodeType.Folder)
                .ThenBy(item => item.Value.Name, new CustomComparer<string>(ComparisonExtensions.CompareNatural))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            _isSorted = true;
        }

        if (!_childrenLoaded)
        {
            FolderChildren = [.._childrenLookup.Values
                .Where(x => x.Type == ENodeType.Folder)
            ];
        
            _childrenLoaded = true;
        }
    }

    [RelayCommand]
    public async Task CopyPath()
    {
        await App.Clipboard.SetTextAsync(FilePath);
    }

    partial void OnExpandedChanged(bool value)
    {
        if (value)
        {
            EnsureChildrenSorted();
        }
    }
}

public enum ENodeType
{
    Folder,
    File
}