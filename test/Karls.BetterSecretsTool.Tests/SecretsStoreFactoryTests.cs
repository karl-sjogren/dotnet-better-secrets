using System.IO.Abstractions.TestingHelpers;
using Karls.BetterSecretsTool.Vendor;

namespace Karls.BetterSecretsTool.Tests;

public class SecretsStoreFactoryTests {
    [Fact]
    public void Create_WhenCalled_ReturnsSecretsStoreWithGivenUserSecretsId() {
        // Arrange
        var fileSystem = new MockFileSystem();
        var factory = new SecretsStoreFactory(fileSystem);
        var userSecretsId = "my-secrets-id";

        // Act
        var secretsStore = factory.Create(userSecretsId);

        // Assert
        secretsStore.ShouldBeOfType<SecretsStore>();
        ((SecretsStore)secretsStore).UserSecretsId.ShouldBe(userSecretsId);
    }
}
