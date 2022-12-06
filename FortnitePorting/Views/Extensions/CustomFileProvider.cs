// TEMPORARILY STEALING FROM GMATRIX TIL MERCURY COMMONS NUGET WORKS BECAUSE IM TOO LAZY TO SUBMODULE AN ENTIRE REPO FOR 1 FILE PLS DON'T SUE

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CUE4Parse.FileProvider.Vfs;
using CUE4Parse.UE4.IO;
using CUE4Parse.UE4.IO.Objects;
using CUE4Parse.UE4.Pak;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using CUE4Parse.Utils;
using Ionic.Zip;

namespace FortnitePorting.Views.Extensions;

/// <summary>
/// This class is used as a base type for all other custom file provider implementations
/// It includes constructors and methods from both <see cref="CUE4Parse.FileProvider.StreamedFileProvider"/> and <see cref="CUE4Parse.FileProvider.DefaultFileProvider"/>
/// </summary>
public abstract class CustomFileProvider : AbstractVfsFileProvider
{
    private DirectoryInfo _workingDirectory;
    private readonly SearchOption _searchOption;
    private readonly List<DirectoryInfo> _extraDirectories = new();

    public CustomFileProvider(bool isCaseInsensitive = false, VersionContainer versions = null) : base(isCaseInsensitive, versions) { }

    public CustomFileProvider(string directory, SearchOption searchOption, bool isCaseInsensitive = false, VersionContainer versions = null)
        : this(new DirectoryInfo(directory), searchOption, isCaseInsensitive, versions) { }

    public CustomFileProvider(DirectoryInfo directory, SearchOption searchOption, bool isCaseInsensitive = false, VersionContainer versions = null)
        : base(isCaseInsensitive, versions)
    {
        _workingDirectory = directory;
        _searchOption = searchOption;
    }

    public CustomFileProvider(DirectoryInfo mainDirectory, List<DirectoryInfo> extraDirectories, SearchOption searchOption, bool isCaseInsensitive = false, VersionContainer versions = null)
        : base(isCaseInsensitive, versions)
    {
        _workingDirectory = mainDirectory;
        _extraDirectories = extraDirectories;
        _searchOption = searchOption;
    }

    /// <summary>
    /// Initialize all local files in the directory
    /// </summary>
    /// <exception cref="ArgumentException">Caused by having a not valid working directory</exception>
    public void InitializeLocal()
    {
        if (!_workingDirectory.Exists) throw new ArgumentException("Given directory must exist", nameof(_workingDirectory));

        IterateFiles(_workingDirectory, _searchOption);
        foreach (var dir in _extraDirectories)
        {
            IterateFiles(dir, _searchOption);
        }
    }

    /// <summary>
    /// Used to initialize individual local files, and all streamed files.
    /// </summary>
    /// <param name="file">Name of file</param>
    /// <param name="stream">Stream containing either a single pak file, or utoc/ucas combination</param>
    /// <param name="openContainerStreamFunc">Used to initialize another file with a utoc</param>
    public void Initialize(string file = "", Stream[] stream = null, Func<string, FArchive> openContainerStreamFunc = null)
    {
        var ext = file.SubstringAfterLast('.');
        if (string.IsNullOrEmpty(ext) || stream == null) return;

        if (ext.Equals("pak", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var reader = new PakFileReader(file, stream[0], Versions) { IsConcurrent = true, CustomEncryption = CustomEncryption };
                if (reader.IsEncrypted && !_requiredKeys.ContainsKey(reader.Info.EncryptionKeyGuid))
                {
                    _requiredKeys[reader.Info.EncryptionKeyGuid] = null;
                }
                _unloadedVfs[reader] = null;
            }
            catch (Exception e)
            {
                Log.Warning("{Exception}", e.ToString());
            }
        }
        else if (ext.Equals("utoc", StringComparison.OrdinalIgnoreCase))
        {
            openContainerStreamFunc ??= it => new FStreamArchive(it, stream[1], Versions);

            try
            {
                var reader = new IoStoreReader(file, stream[0], openContainerStreamFunc, EIoStoreTocReadOptions.ReadDirectoryIndex, Versions) { IsConcurrent = true, CustomEncryption = CustomEncryption };
                if (reader.IsEncrypted && !_requiredKeys.ContainsKey(reader.Info.EncryptionKeyGuid))
                {
                    _requiredKeys[reader.Info.EncryptionKeyGuid] = null;
                }
                _unloadedVfs[reader] = null;
            }
            catch (Exception e)
            {
                Log.Warning("{Exception}", e.ToString());
            }
        }
    }

    /// <summary>
    /// Registers a local file into the provider
    /// </summary>
    /// <param name="file">Info for a file</param>
    protected void RegisterFile(FileInfo file)
    {
        var ext = file.FullName.SubstringAfterLast('.');
        if (ext.Equals("pak", StringComparison.OrdinalIgnoreCase))
        {
            Initialize(file.FullName, new Stream[] { file.OpenRead() });
        }
        else if (ext.Equals("utoc", StringComparison.OrdinalIgnoreCase))
        {
            Initialize(file.FullName, new Stream[] { file.OpenRead() }, it => new FStreamArchive(it, File.OpenRead(it), Versions));
        }
        else if (ext.Equals("apk", StringComparison.OrdinalIgnoreCase))
        {
            var zipfile = new ZipFile(file.FullName);
            MemoryStream pngstream = new();
            foreach (var entry in zipfile.Entries)
            {
                if (!entry.FileName.EndsWith("main.obb.png", StringComparison.OrdinalIgnoreCase)) continue;
                entry.Extract(pngstream);
                pngstream.Seek(0, SeekOrigin.Begin);

                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                var container = ZipFile.Read(pngstream);

                foreach (var fileentry in container.Entries)
                {
                    var streams = new Stream[2];
                    if (fileentry.FileName.EndsWith(".pak"))
                    {
                        try
                        {
                            streams[0] = new MemoryStream();
                            fileentry.Extract(streams[0]);
                            streams[0].Seek(0, SeekOrigin.Begin);
                        }
                        catch (Exception e)
                        {
                            Log.Warning("{Exception}", e.ToString());
                        }
                    }
                    else if (fileentry.FileName.EndsWith(".utoc"))
                    {
                        try
                        {
                            streams[0] = new MemoryStream();
                            fileentry.Extract(streams[0]);
                            streams[0].Seek(0, SeekOrigin.Begin);

                            foreach (var ucas in container.Entries) // look for ucas file
                            {
                                if (ucas.FileName.Equals(fileentry.FileName.SubstringBeforeLast('.') + ".ucas"))
                                {
                                    streams[1] = new MemoryStream();
                                    ucas.Extract(streams[1]);
                                    streams[1].Seek(0, SeekOrigin.Begin);
                                    break;
                                }
                            }
                            if (streams[1] is not { }) continue; // ucas file not found
                        }
                        catch (Exception e)
                        {
                            Log.Warning("{Exception}", e.ToString());
                        }
                    }
                    else
                    {
                        continue;
                    }

                    Initialize(fileentry.FileName, streams);
                }
            }
        }
    }

    /// <summary>
    /// Iterate through all files in a directory to load into the provider
    /// </summary>
    /// <param name="directory">Directory to files</param>
    /// <param name="option">File search options</param>
    private void IterateFiles(DirectoryInfo directory, SearchOption option)
    {
        if (!directory.Exists) return;

        foreach (var file in directory.EnumerateFiles("*.*", option))
        {
            var ext = file.Extension.SubstringAfter('.');
            if (!file.Exists || string.IsNullOrEmpty(ext)) continue;
            RegisterFile(file);
        }
    }
}