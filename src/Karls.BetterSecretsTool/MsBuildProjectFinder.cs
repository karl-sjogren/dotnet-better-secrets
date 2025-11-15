using System.IO.Abstractions;
using System.Xml;

namespace Karls.BetterSecretsTool;

internal sealed class MsBuildProjectFinder {
    private readonly string _baseDirectory;
    private readonly IFileSystem _fileSystem;

    public MsBuildProjectFinder(string baseDirectory, IFileSystem? fileSystem = null) {
        ArgumentException.ThrowIfNullOrEmpty(baseDirectory);

        _baseDirectory = baseDirectory;
        _fileSystem = fileSystem ?? new FileSystem();
    }

    public MsBuildProject[] FindMsBuildProjects() {
        if(!_fileSystem.Directory.Exists(_baseDirectory)) {
            return [];
        }

        var projects = _fileSystem.Directory.EnumerateFileSystemEntries(_baseDirectory, "*.*proj", SearchOption.AllDirectories)
            .Where(f => !".xproj".Equals(_fileSystem.Path.GetExtension(f), StringComparison.OrdinalIgnoreCase))
            .ToList();

        if(projects.Count == 0) {
            return [];
        }

        return projects.Select(ParseProjectFile)
            .OfType<MsBuildProject>()
            .ToArray();
    }

    private MsBuildProject? ParseProjectFile(string projectFilePath) {
        string? sdk = null;

        var xmlDoc = new XmlDocument();
        xmlDoc.Load(projectFilePath);

        var projectNode = xmlDoc.SelectSingleNode("/Project");
        if(projectNode?.Attributes != null) {
            var sdkAttribute = projectNode.Attributes["Sdk"];
            if(sdkAttribute != null) {
                sdk = sdkAttribute.Value;
            }
        }

        var parentDirectory = _fileSystem.Path.GetDirectoryName(projectFilePath);
        var atRoot = string.Equals(parentDirectory, _baseDirectory, StringComparison.OrdinalIgnoreCase);

        if(sdk == null) {
            return null;
        }

        return new MsBuildProject(projectFilePath, sdk, atRoot);
    }
}

internal sealed record MsBuildProject(string Path, string Sdk, bool AtRoot) {
    public bool IsWebSdk => Sdk.StartsWith("Microsoft.NET.Sdk.Web", StringComparison.OrdinalIgnoreCase);
}
