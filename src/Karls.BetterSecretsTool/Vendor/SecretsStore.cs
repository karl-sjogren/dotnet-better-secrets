// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Abstractions;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Karls.BetterSecretsTool.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.UserSecrets;

namespace Karls.BetterSecretsTool.Vendor;

/// <summary>
/// This API supports infrastructure and is not intended to be used
/// directly from your code. This API may change or be removed in future releases.
/// </summary>
public class SecretsStore : ISecretStore {
    private readonly IDictionary<string, string> _secrets;
    private readonly IFileSystem _fileSystem;
    private readonly JsonSerializerOptions _jsonSerializerOptions = new() { WriteIndented = true };

    public SecretsStore(string userSecretsId, IFileSystem? fileSystem = null) {
        ArgumentNullException.ThrowIfNull(userSecretsId);
        _fileSystem = fileSystem ?? new FileSystem();

        SecretsFilePath = PathHelper.GetSecretsPathFromSecretsId(userSecretsId);

        // workaround bug in configuration
        var secretDir = _fileSystem.Path.GetDirectoryName(SecretsFilePath);
        _fileSystem.Directory.CreateDirectory(secretDir!);

        _secrets = Load(userSecretsId);
    }

    public string this[string key] {
        get {
            return _secrets[key];
        }
    }

    public int Count => _secrets.Count;

    // For testing.
    internal string SecretsFilePath { get; }

    public bool ContainsKey(string key) => _secrets.ContainsKey(key);

    public IEnumerable<KeyValuePair<string, string>> AsEnumerable() => _secrets;

    public IEnumerable<KeyValuePair<string, string>> AsSortedEnumerable() => _secrets.OrderBy(kvp => kvp.Key);

    public void Clear() => _secrets.Clear();

    public void Set(string key, string value) => _secrets[key] = value;

    public void Remove(string key) {
        _secrets.Remove(key);
    }

    public virtual void Save() {
        _fileSystem.Directory.CreateDirectory(_fileSystem.Path.GetDirectoryName(SecretsFilePath)!);

        var contents = new Dictionary<string, string>();
        if(_secrets is not null) {
            foreach(var secret in _secrets.AsEnumerable()) {
                contents[secret.Key] = secret.Value;
            }
        }

        // Create a temp file with the correct Unix file mode before moving it to the expected _filePath.
        if(!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            var tempFilename = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), _fileSystem.Path.GetRandomFileName());
            _fileSystem.File.Create(tempFilename).Dispose();
            _fileSystem.File.Move(tempFilename, SecretsFilePath, overwrite: true);
        }

        var jsonContent = JsonSerializer.Serialize(contents, _jsonSerializerOptions);
        _fileSystem.File.WriteAllText(SecretsFilePath, jsonContent, Encoding.UTF8);
    }

    protected virtual IDictionary<string, string> Load(string userSecretsId) {
        return new ConfigurationBuilder()
            .AddJsonFile(SecretsFilePath, optional: true)
            .Build()
            .AsEnumerable()
            .Where(i => i.Value != null)
            .Cast<KeyValuePair<string, string>>()
            .ToDictionary(i => i.Key, i => i.Value, StringComparer.OrdinalIgnoreCase);
    }
}
