using System.IO;
using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Versions;

namespace FortnitePorting.Extensions;

public class HybridFileProvider : DefaultFileProvider
{
    private const bool CaseInsensitive = true;
    private static readonly SearchOption SearchOption = SearchOption.AllDirectories;
    
    // Live
    public HybridFileProvider(VersionContainer? version = null) : base(string.Empty, SearchOption, CaseInsensitive, version)
    {
    }

    // Local + Custom
    public HybridFileProvider(string mainDirectory, VersionContainer? version = null) : base(mainDirectory, SearchOption, CaseInsensitive, version)
    {
    }
}