// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO.Abstractions;
using System.Text;
using Karls.BetterSecretsTool.Contracts;

namespace Karls.BetterSecretsTool.Vendor;

/// <summary>
/// This API supports infrastructure and is not intended to be used
/// directly from your code. This API may change or be removed in future releases.
/// </summary>
internal sealed class ProjectIdResolver : IProjectIdResolver {
    private const string _defaultConfig = "Debug";
    private readonly string? _targetsFile;
    private readonly IFileSystem _fileSystem;

    public ProjectIdResolver(IFileSystem? fileSystem = null) {
        _fileSystem = fileSystem ?? new FileSystem();
        _targetsFile = FindTargetsFile();
    }

    public ResolveResult Resolve(string projectFile, string configuration) {
        configuration = !string.IsNullOrEmpty(configuration)
            ? configuration
            : _defaultConfig;

        var secretIdOutputFile = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), _fileSystem.Path.GetRandomFileName());
        var secretKeyVaultOutputFile = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), _fileSystem.Path.GetRandomFileName());
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
                        "/p:_UserSecretsMetadataFile=" + secretIdOutputFile,
                        "/p:_UserSecretsKeyVaultMetadataFile=" + secretKeyVaultOutputFile,
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
                throw new InvalidOperationException($"Could not load the MSBuild project '{_fileSystem.Path.GetFileName(projectFile)}'");
            }

            if(!_fileSystem.File.Exists(secretIdOutputFile)) {
                throw new InvalidOperationException($"Could not find the global property 'UserSecretsId' in MSBuild project '{_fileSystem.Path.GetFileName(projectFile)}'.");
            }

            var userSecretsId = _fileSystem.File.ReadAllText(secretIdOutputFile)?.Trim();
            if(string.IsNullOrEmpty(userSecretsId)) {
                throw new InvalidOperationException($"Could not find the global property 'UserSecretsId' in MSBuild project '{_fileSystem.Path.GetFileName(projectFile)}'.");
            }

            var userSecretsKeyVault = string.Empty;
            if(_fileSystem.File.Exists(secretKeyVaultOutputFile)) {
                userSecretsKeyVault = _fileSystem.File.ReadAllText(secretKeyVaultOutputFile)?.Trim() ?? string.Empty;
            }

            return new ResolveResult(userSecretsId, userSecretsKeyVault);
        } finally {
            TryDelete(secretIdOutputFile);
            TryDelete(secretKeyVaultOutputFile);
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
            throw new InvalidOperationException("Fatal error: could not find SecretManager.targets");
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
