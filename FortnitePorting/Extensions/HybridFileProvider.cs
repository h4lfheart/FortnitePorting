using System.IO;
using CUE4Parse.UE4.Versions;
using MercuryCommons.Framework.Unreal;

namespace FortnitePorting.Extensions;

public class HybridFileProvider : CustomFileProvider
{
    private const bool CaseInsensitive = true;
    private static readonly SearchOption SearchOption = SearchOption.AllDirectories;
    
    // Live
    public HybridFileProvider(VersionContainer? version = null) : base(CaseInsensitive, version)
    {
    }

    // Local + Custom
    public HybridFileProvider(string mainDirectory, VersionContainer? version = null) : base(mainDirectory, SearchOption, CaseInsensitive, version)
    {
    }
    
    public override void Initialize()
    {
        base.Initialize();
    }
}