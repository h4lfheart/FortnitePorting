namespace FortnitePorting.Framework.ViewModels.Endpoints.Models;

public class ReleaseResponse
{
    public string Version;
    public FPVersion ProperVersion => new(Version);
    
    public string DownloadUrl;
    public string ChangelogUrl;
    public bool IsMandatory;

    public DependencyResponse[] Dependencies;
}

public class DependencyResponse
{
    public string Name;
    public string URL;
}

public class FPVersion
{
    public readonly int Major;
    public readonly int Minor;
    public readonly int Patch;

    public FPVersion(string inVersion)
    {
        var mainVersioning = inVersion.Split(".");
        Major = int.Parse(mainVersioning[0]);
        Minor = int.Parse(mainVersioning[1]);
        Patch = int.Parse(mainVersioning[2]);
    }

    public FPVersion(int major = 2, int minor = 0, int patch = 0, string subversion = "")
    {
        Major = major;
        Minor = minor;
        Patch = patch;
    }

    public bool MajorEquals(FPVersion other)
    {
        return Major == other.Major;
    }
    
    public bool MinorEquals(FPVersion other)
    {
        return Minor == other.Minor;
    }
    
    public bool PatchEquals(FPVersion other)
    {
        return Patch == other.Patch;
    }

    public override string ToString()
    {
        return $"{Major}.{Minor}.{Patch}";
    }

    public override bool Equals(object? obj)
    {
        var other = (FPVersion) obj!;
        return MajorEquals(other) && MinorEquals(other) && PatchEquals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Major, Minor, Patch);
    }
}