// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Abstractions;

namespace Karls.BetterSecretsTool.Vendor;

internal sealed class MsBuildProjectFinder {
    private readonly string _directory;
    private readonly IFileSystem _fileSystem;

    public MsBuildProjectFinder(string directory, IFileSystem? fileSystem = null) {
        ArgumentException.ThrowIfNullOrEmpty(directory);

        _directory = directory;
        _fileSystem = fileSystem ?? new FileSystem();
    }

    public string FindMsBuildProject(string? project) {
        var projectPath = project ?? _directory;

        if(!_fileSystem.Path.IsPathRooted(projectPath)) {
            projectPath = _fileSystem.Path.Combine(_directory, projectPath);
        }

        if(_fileSystem.Directory.Exists(projectPath)) {
            var projects = _fileSystem.Directory.EnumerateFileSystemEntries(projectPath, "*.*proj", SearchOption.TopDirectoryOnly)
                .Where(f => !".xproj".Equals(_fileSystem.Path.GetExtension(f), StringComparison.OrdinalIgnoreCase))
                .ToList();

            if(projects.Count > 1) {
                throw new FileNotFoundException("Multiple projects found. Please specify a project file.");
            }

            if(projects.Count == 0) {
                throw new FileNotFoundException("No projects found.");
            }

            return projects[0];
        }

        if(!_fileSystem.File.Exists(projectPath)) {
            throw new FileNotFoundException("Project file not found.");
        }

        return projectPath;
    }
}
