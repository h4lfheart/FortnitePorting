using System.Collections.Generic;

namespace FortnitePorting.Models.Files;

public class FileNode
{
    public string Name { get; }
    public string Path { get; }
    public string VfsName { get; }
    public ENodeType Type { get; }
    public int FileChildCount { get; set; }
    public int FolderChildCount { get; set; }

    public Dictionary<string, FileNode> Children { get; } = new();

    public FileNode(string name, string path, ENodeType type, string vfsName = "")
    {
        Name = name;
        Path = path;
        Type = type;
        VfsName = vfsName;
    }

    public void AddChild(string name, FileNode child)
    {
        Children[name] = child;

        if (child.Type is ENodeType.Folder)
            FolderChildCount++;
        else
            FileChildCount++;
    }

    public bool TryGetChild(string name, out FileNode child)
        => Children.TryGetValue(name, out child!);
}