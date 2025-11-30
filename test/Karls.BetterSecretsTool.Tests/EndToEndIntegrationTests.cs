using System.Diagnostics;
using System.IO.Abstractions;
using Karls.BetterSecretsTool.Vendor;
using Microsoft.Extensions.Configuration.UserSecrets;
using Spectre.Console.Testing;

namespace Karls.BetterSecretsTool.Tests;

/// <summary>
/// End-to-end integration tests that use real implementations of all components
/// except the console UI (which uses TestConsole).
/// </summary>
public class EndToEndIntegrationTests : IDisposable {
    private readonly string _testUserSecretsId;
    private readonly string _testProjectDirectory;
    private readonly string _testProjectFile;
    private readonly IFileSystem _fileSystem;
    private readonly string _secretsFilePath;

    public EndToEndIntegrationTests() {
        // Generate a unique user secrets ID for each test instance
        _testUserSecretsId = $"e2e-test-{Guid.NewGuid()}";
        _fileSystem = new FileSystem();
        _secretsFilePath = PathHelper.GetSecretsPathFromSecretsId(_testUserSecretsId);

        // Create a temporary test project directory
        _testProjectDirectory = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), $"BetterSecretsE2ETest-{Guid.NewGuid()}");
        _testProjectFile = _fileSystem.Path.Combine(_testProjectDirectory, "TestProject.csproj");

        _fileSystem.Directory.CreateDirectory(_testProjectDirectory);
        CreateTestProject();
    }

    public void Dispose() {
        // Clean up: remove the secrets file and directory created during tests
        try {
            var secretsDirectoryPath = _fileSystem.Path.GetDirectoryName(_secretsFilePath);
            if(secretsDirectoryPath is not null && _fileSystem.Directory.Exists(secretsDirectoryPath)) {
                _fileSystem.Directory.Delete(secretsDirectoryPath, recursive: true);
            }
        } catch {
            // Ignore cleanup errors
        }

        // Clean up: remove the test project directory
        try {
            if(_fileSystem.Directory.Exists(_testProjectDirectory)) {
                _fileSystem.Directory.Delete(_testProjectDirectory, recursive: true);
            }
        } catch {
            // Ignore cleanup errors
        }
    }

    private void CreateTestProject() {
        // Create a minimal .NET project file with UserSecretsId
        var projectContent = $@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <UserSecretsId>{_testUserSecretsId}</UserSecretsId>
  </PropertyGroup>
</Project>";
        _fileSystem.File.WriteAllText(_testProjectFile, projectContent);
    }

    [Fact]
    public void Run_WithProjectDirectory_AddSecret_SecretIsPersisted() {
        // Arrange
        var console = new TestConsole();
        console.Interactive();
        var tool = CreateRealTool(console);

        // Simulate: A (add secret), enter key name, enter value, then Q (quit)
        console.Input.PushText("A");
        console.Input.PushTextWithEnter("E2ETestKey");
        console.Input.PushTextWithEnter("E2ETestValue");
        console.Input.PushText("Q");

        // Act - use the project directory as argument
        tool.Run([_testProjectDirectory]);

        // Assert - Verify the secret was persisted using dotnet user-secrets list
        var listOutput = RunDotnetUserSecretsList();
        listOutput.ShouldContain("E2ETestKey");
        listOutput.ShouldContain("E2ETestValue");
    }

    [Fact]
    public void Run_WithUserSecretsIdArgument_AddSecret_SecretIsPersisted() {
        // Arrange
        var console = new TestConsole();
        console.Interactive();
        var tool = CreateRealTool(console);

        // Simulate: A (add secret), enter key name, enter value, then Q (quit)
        console.Input.PushText("A");
        console.Input.PushTextWithEnter("IdArgKey");
        console.Input.PushTextWithEnter("IdArgValue");
        console.Input.PushText("Q");

        // Act - pass the user secrets ID directly via argument
        tool.Run(["--id", _testUserSecretsId]);

        // Assert - Verify the secret was persisted using dotnet user-secrets list
        var listOutput = RunDotnetUserSecretsList();
        listOutput.ShouldContain("IdArgKey");
        listOutput.ShouldContain("IdArgValue");
    }

    [Fact]
    public void Run_WithProjectDirectory_EditSecret_SecretIsUpdated() {
        // Arrange - first set a secret using dotnet user-secrets
        SetSecretViaCli("EditTestKey", "OriginalValue");

        var console = new TestConsole();
        console.Interactive();
        var tool = CreateRealTool(console);

        // Simulate: E (edit secret), select the key (Enter to select first), enter new value, then Q (quit)
        console.Input.PushText("E");
        console.Input.PushKey(ConsoleKey.Enter); // Select first (only) item
        console.Input.PushTextWithEnter("UpdatedValue");
        console.Input.PushText("Q");

        // Act
        tool.Run([_testProjectDirectory]);

        // Assert - Verify the secret was updated using dotnet user-secrets list
        var listOutput = RunDotnetUserSecretsList();
        listOutput.ShouldContain("EditTestKey");
        listOutput.ShouldContain("UpdatedValue");
        listOutput.ShouldNotContain("OriginalValue");
    }

    [Fact]
    public void Run_WithProjectDirectory_RemoveSecret_SecretIsDeleted() {
        // Arrange - first set a secret using dotnet user-secrets
        SetSecretViaCli("RemoveTestKey", "SomeValue");

        var console = new TestConsole();
        console.Interactive();
        var tool = CreateRealTool(console);

        // Simulate: D (delete secret), select the key (Enter to select first), then Q (quit)
        console.Input.PushText("D");
        console.Input.PushKey(ConsoleKey.Enter); // Select first (only) item
        console.Input.PushText("Q");

        // Act
        tool.Run([_testProjectDirectory]);

        // Assert - Verify the secret was removed using dotnet user-secrets list
        var listOutput = RunDotnetUserSecretsList();
        listOutput.ShouldNotContain("RemoveTestKey");
        listOutput.ShouldNotContain("SomeValue");
    }

    [Fact]
    public void Run_WithProjectDirectory_FullWorkflow_SecretsAreManagedCorrectly() {
        // Arrange
        var console = new TestConsole();
        console.Interactive();
        var tool = CreateRealTool(console);

        // Step 1: Add a secret
        console.Input.PushText("A");
        console.Input.PushTextWithEnter("WorkflowKey");
        console.Input.PushTextWithEnter("InitialValue");

        // Step 2: Edit the secret (it will be the only one, so Enter selects it)
        console.Input.PushText("E");
        console.Input.PushKey(ConsoleKey.Enter);
        console.Input.PushTextWithEnter("ModifiedValue");

        // Step 3: Add another secret (sorted order: SecondKey, WorkflowKey)
        console.Input.PushText("A");
        console.Input.PushTextWithEnter("SecondKey");
        console.Input.PushTextWithEnter("SecondValue");

        // Step 4: Remove SecondKey (first in sorted list, so just press Enter)
        console.Input.PushText("D");
        console.Input.PushKey(ConsoleKey.Enter); // Select first item (SecondKey)

        // Step 5: Quit
        console.Input.PushText("Q");

        // Act
        tool.Run([_testProjectDirectory]);

        // Assert - Verify with dotnet user-secrets list
        var listOutput = RunDotnetUserSecretsList();
        listOutput.ShouldContain("WorkflowKey");
        listOutput.ShouldContain("ModifiedValue");
        listOutput.ShouldNotContain("SecondKey");
        listOutput.ShouldNotContain("InitialValue");
    }

    private Tool CreateRealTool(TestConsole console) {
        // Create real implementations of all dependencies
        var projectFinder = new MsBuildProjectFinder(_fileSystem);
        var projectIdResolver = new ProjectIdResolver(_fileSystem);
        var secretsStoreFactory = new SecretsStoreFactory(_fileSystem);

        return new Tool(console, _fileSystem, projectFinder, projectIdResolver, secretsStoreFactory);
    }

    private void SetSecretViaCli(string key, string value) {
        using var process = new Process();
        process.StartInfo.FileName = "dotnet";
        process.StartInfo.Arguments = $"user-secrets set \"{key}\" \"{value}\" --id {_testUserSecretsId}";
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;

        process.Start();
        process.WaitForExit();
    }

    private string RunDotnetUserSecretsList() {
        using var process = new Process();
        process.StartInfo.FileName = "dotnet";
        process.StartInfo.Arguments = $"user-secrets list --id {_testUserSecretsId}";
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;

        process.Start();
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        // Note: dotnet user-secrets list returns exit code 0 even when no secrets exist,
        // outputting "No secrets configured for this application."
        // We combine both streams as the error stream may contain relevant messages.
        return output + error;
    }
}
