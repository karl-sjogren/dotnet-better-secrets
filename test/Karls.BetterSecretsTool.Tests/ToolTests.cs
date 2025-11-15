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

    [Fact]
    public void RenderHelpMessage_PrintsExpectedOutput() {
        // Arrange
        var tool = new Tool(_console, _fileSystem, _projectFinder, _projectIdResolver, _secretsStoreFactory);

        // Act
        tool.RenderHelpMessage();

        // Assert
        var output = _console.Output;
        output.ShouldStartWith("Karls Better Secrets Tool");
        output.ShouldContain("Usage: dotnet better-secrets <working-directory> [options]");
        output.ShouldContain("For more information, visit");
        output.TrimEnd().ShouldEndWith("https://github.com/karl-sjogren/dotnet-better-secrets.");
    }

    [Fact]
    public void RenderTable_WhenCalledWithEmptyStore_PrintsEmptyTableWithCorrectHeaders() {
        // Arrange
        var tool = new Tool(_console, _fileSystem, _projectFinder, _projectIdResolver, _secretsStoreFactory);

        var secretsStore = new InMemorySecretsStore();

        // Act
        tool.RenderTable(secretsStore);

        // Assert
        var lines = _console.Output.Split("\n", StringSplitOptions.RemoveEmptyEntries);
        lines.Length.ShouldBe(4); // Top border, header, bottom border, caption

        var headerLine = lines[1];
        headerLine.ShouldContain("│ Key");
        headerLine.ShouldContain("│ Value");
    }

    [Fact]
    public void RenderTable_WhenCalledWithSecrets_PrintsTableWithSecrets() {
        // Arrange
        var tool = new Tool(_console, _fileSystem, _projectFinder, _projectIdResolver, _secretsStoreFactory);

        var secretsStore = new InMemorySecretsStore();
        secretsStore.Set("ApiKey", "12345");
        secretsStore.Set("ConnectionString", "Server=myServer;Database=myDB;User Id=myUser;Password=myPass;");

        // Act
        tool.RenderTable(secretsStore);

        // Assert
        var lines = _console.Output.Split("\n", StringSplitOptions.RemoveEmptyEntries);
        lines.Length.ShouldBe(9); // Top border, header, separator, 1 secret, separator, 1 secret on two lines, bottom border, caption

        var secret1 = lines[3];
        secret1.ShouldContain("│ ApiKey");
        secret1.ShouldContain("│ 12345");

        var secret2 = lines[5];
        secret2.ShouldContain("│ ConnectionString");
        secret2.ShouldContain("│ Server=myServer;Database=myDB;User");

        var secret2Continued = lines[6];
        secret2Continued.ShouldContain("│ Id=myUser;Password=myPass;");
    }
}
