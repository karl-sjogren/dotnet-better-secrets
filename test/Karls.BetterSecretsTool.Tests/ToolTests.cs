using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Karls.BetterSecretsTool.Contracts;
using Spectre.Console.Testing;

namespace Karls.BetterSecretsTool.Tests;

public class ToolTests {
    private readonly TestConsole _console;
    private readonly IFileSystem _fileSystem;
    private readonly IMsBuildProjectFinder _projectFinder;
    private readonly IProjectIdResolver _projectIdResolver;
    private readonly ISecretsStoreFactory _secretsStoreFactory;

    public ToolTests() {
        _console = new TestConsole();
        _console.Interactive();

        _fileSystem = new MockFileSystem();

        _projectFinder = A.Dummy<IMsBuildProjectFinder>();

        _projectIdResolver = A.Dummy<IProjectIdResolver>();

        _secretsStoreFactory = A.Dummy<ISecretsStoreFactory>();
    }

    [Fact]
    public void MainLoop_WhenEnterIsPressed_Exits() {
        // Arrange
        var tool = new Tool(_console, _fileSystem, _projectFinder, _projectIdResolver, _secretsStoreFactory);

        _console.Input.PushText("\n");

        var secretsStore = A.Dummy<ISecretsStore>();

        // Act & Assert
        Should.NotThrow(() => tool.MainLoop(null, secretsStore));
    }
}
