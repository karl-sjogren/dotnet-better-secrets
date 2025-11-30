using System.IO.Abstractions;
using Karls.BetterSecretsTool.Contracts;
using Karls.BetterSecretsTool.Vendor;

namespace Karls.BetterSecretsTool;

internal class SecretsStoreFactory : ISecretsStoreFactory {
    private readonly IFileSystem _fileSystem;

    public SecretsStoreFactory(IFileSystem fileSystem) {
        _fileSystem = fileSystem;
    }

    public ISecretsStore Create(string userSecretsId) {
        return new SecretsStore(userSecretsId, _fileSystem);
    }
}
