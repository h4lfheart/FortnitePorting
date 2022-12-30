using System.Collections.Generic;
using System.IO;
using CUE4Parse.UE4.Versions;
using MercuryCommons.Framework.Unreal;

namespace FortnitePorting.Views.Extensions;

public class FortnitePortingFileProvider : CustomFileProvider
{
    public FortnitePortingFileProvider(bool isCaseInsensitive = false, VersionContainer? versions = null) : base(isCaseInsensitive, versions) { }

    public FortnitePortingFileProvider(DirectoryInfo mainDirectory, List<DirectoryInfo> extraDirectories, SearchOption searchOption, bool isCaseInsensitive = false, VersionContainer? versions = null)
        : base(mainDirectory, extraDirectories, searchOption, isCaseInsensitive, versions) { }
}