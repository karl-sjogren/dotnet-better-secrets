// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO.Abstractions;
using System.Text;

namespace Karls.BetterSecretsTool.Vendor;

/// <summary>
/// This API supports infrastructure and is not intended to be used
/// directly from your code. This API may change or be removed in future releases.
/// </summary>
internal sealed class ProjectIdResolver {
    private const string _defaultConfig = "Debug";
    private readonly string? _targetsFile;
    private readonly string _workingDirectory;
    private readonly IFileSystem _fileSystem;

    public ProjectIdResolver(string workingDirectory, IFileSystem? fileSystem = null) {
        _workingDirectory = workingDirectory;
        _fileSystem = fileSystem ?? new FileSystem();
        _targetsFile = FindTargetsFile();
    }

    public string? Resolve(string project, string configuration) {
        var finder = new MsBuildProjectFinder(_workingDirectory, _fileSystem);
        string projectFile;
        try {
            projectFile = finder.FindMsBuildProject(project);
        } catch(Exception) {
            //_reporter.Error(ex.Message);
            return null;
        }

        configuration = !string.IsNullOrEmpty(configuration)
            ? configuration
            : _defaultConfig;

        var outputFile = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), _fileSystem.Path.GetRandomFileName());
        try {
            var psi = new ProcessStartInfo {
                FileName = DotNetMuxer.MuxerPathOrDefault(),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                ArgumentList =
                    {
                        "msbuild",
                        projectFile,
                        "/nologo",
                        "/t:_ExtractUserSecretsMetadata", // defined in SecretManager.targets
                        "/p:_UserSecretsMetadataFile=" + outputFile,
                        "/p:Configuration=" + configuration,
                        "/p:CustomAfterMicrosoftCommonTargets=" + _targetsFile,
                        "/p:CustomAfterMicrosoftCommonCrossTargetingTargets=" + _targetsFile,
                        "-verbosity:detailed",
                    }
            };

            using var process = new Process() {
                StartInfo = psi,
            };

            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();
            process.OutputDataReceived += (_, d) => {
                if(!string.IsNullOrEmpty(d.Data)) {
                    outputBuilder.AppendLine(d.Data);
                }
            };
            process.ErrorDataReceived += (_, d) => {
                if(!string.IsNullOrEmpty(d.Data)) {
                    errorBuilder.AppendLine(d.Data);
                }
            };
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();

            if(process.ExitCode != 0) {
                //_reporter.Error($"Exit code: {process.ExitCode}");
                //_reporter.Error(SecretsHelpersResources.FormatError_ProjectFailedToLoad(projectFile));
                return null;
            }

            if(!_fileSystem.File.Exists(outputFile)) {
                //_reporter.Error(SecretsHelpersResources.FormatError_ProjectMissingId(projectFile));
                return null;
            }

            var id = _fileSystem.File.ReadAllText(outputFile)?.Trim();
            if(string.IsNullOrEmpty(id)) {
                //_reporter.Error(SecretsHelpersResources.FormatError_ProjectMissingId(projectFile));
            }

            return id;
        } finally {
            TryDelete(outputFile);
        }
    }

    private string? FindTargetsFile() {
        var assemblyDir = _fileSystem.Path.GetDirectoryName(typeof(ProjectIdResolver).Assembly.Location);
        var searchPaths = new[]
        {
                _fileSystem.Path.Combine(AppContext.BaseDirectory, "assets"),
                _fileSystem.Path.Combine(assemblyDir!, "assets"),
                AppContext.BaseDirectory,
                assemblyDir
            };

        var targetPath = searchPaths.Select(p => _fileSystem.Path.Combine(p!, "SecretManager.targets")).FirstOrDefault(_fileSystem.File.Exists);
        if(targetPath == null) {
            //_reporter.Error("Fatal error: could not find SecretManager.targets");
            return null;
        }

        return targetPath;
    }

    private void TryDelete(string file) {
        try {
            if(_fileSystem.File.Exists(file)) {
                _fileSystem.File.Delete(file);
            }
        } catch {
            // whatever
        }
    }
}
