namespace FortnitePorting.Shared.Extensions;

public static class FileSystemExtensions
{
    public static bool TryDeleteFile(string path)
    {
        try
        {
            File.Delete(path);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static void Copy(string sourceDirectory, string targetDirectory)
    {
        Copy(new DirectoryInfo(sourceDirectory), new DirectoryInfo(targetDirectory));
    }

    public static void Copy(DirectoryInfo source, DirectoryInfo target)
    {
        Directory.CreateDirectory(target.FullName);

        foreach (var file in source.GetFiles())
        {
            file.CopyTo(Path.Combine(target.FullName, file.Name), true);
        }

        foreach (var subDirectory in source.GetDirectories())
        {
            Copy(subDirectory, target.CreateSubdirectory(subDirectory.Name));
        }
    }
}
