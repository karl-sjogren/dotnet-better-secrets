using System.IO.Abstractions;
using System.Xml;
using Karls.BetterSecretsTool.Contracts;

namespace Karls.BetterSecretsTool;

internal sealed class MsBuildProjectFinder : IMsBuildProjectFinder {
    private readonly IFileSystem _fileSystem;

    public MsBuildProjectFinder(IFileSystem? fileSystem = null) {
        _fileSystem = fileSystem ?? new FileSystem();
    }

    public MsBuildProject[] FindMsBuildProjects(string baseDirectory) {
        if(!_fileSystem.Directory.Exists(baseDirectory)) {
            return [];
        }

        var projects = _fileSystem.Directory.EnumerateFileSystemEntries(baseDirectory, "*.*proj", SearchOption.AllDirectories)
            .Where(f => !".xproj".Equals(_fileSystem.Path.GetExtension(f), StringComparison.OrdinalIgnoreCase))
            .ToList();

        if(projects.Count == 0) {
            return [];
        }

        return projects.Select(projectFilePath => ParseProjectFile(projectFilePath, baseDirectory))
            .OfType<MsBuildProject>()
            .ToArray();
    }

    internal MsBuildProject? ParseProjectFile(string projectFilePath, string baseDirectory) {
        string? sdk = null;

        try {
            using var stream = _fileSystem.File.OpenRead(projectFilePath);
            var xmlDoc = new XmlDocument();
            xmlDoc.Load(stream);

            var projectNode = xmlDoc.SelectSingleNode("/Project");
            if(projectNode?.Attributes != null) {
                var sdkAttribute = projectNode.Attributes["Sdk"];
                if(sdkAttribute != null) {
                    sdk = sdkAttribute.Value;
                }
            }
        } catch(XmlException) {
            return null;
        }

        var parentDirectory = _fileSystem.Path.GetDirectoryName(projectFilePath);

        // Ensure none have a trailing separator for accurate comparison
        parentDirectory = parentDirectory?.TrimEnd(_fileSystem.Path.DirectorySeparatorChar, _fileSystem.Path.AltDirectorySeparatorChar);
        baseDirectory = baseDirectory.TrimEnd(_fileSystem.Path.DirectorySeparatorChar, _fileSystem.Path.AltDirectorySeparatorChar);

        var atRoot = parentDirectory == baseDirectory;

        if(sdk == null) {
            return null;
        }

        return new MsBuildProject(projectFilePath, sdk, atRoot);
    }
}

internal sealed record MsBuildProject(string Path, string Sdk, bool AtRoot) {
    public bool IsWebSdk => Sdk.StartsWith("Microsoft.NET.Sdk.Web", StringComparison.OrdinalIgnoreCase);
}
