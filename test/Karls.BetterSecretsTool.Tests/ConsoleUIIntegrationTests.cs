using System.Diagnostics;
using System.IO.Abstractions;
using Karls.BetterSecretsTool.Contracts;
using Karls.BetterSecretsTool.Vendor;
using Microsoft.Extensions.Configuration.UserSecrets;
using Spectre.Console.Testing;

namespace Karls.BetterSecretsTool.Tests;

public class ConsoleUIIntegrationTests : IDisposable {
    private readonly string _testUserSecretsId;
    private readonly IFileSystem _fileSystem;
    private readonly string _secretsFilePath;

    public ConsoleUIIntegrationTests() {
        // Generate a unique user secrets ID for each test instance
        _testUserSecretsId = $"integration-test-{Guid.NewGuid()}";
        _fileSystem = new FileSystem();
        _secretsFilePath = PathHelper.GetSecretsPathFromSecretsId(_testUserSecretsId);
    }

    public void Dispose() {
        // Clean up: remove the secrets file and directory created during tests
        try {
            var directoryPath = _fileSystem.Path.GetDirectoryName(_secretsFilePath);
            if(directoryPath is not null && _fileSystem.Directory.Exists(directoryPath)) {
                _fileSystem.Directory.Delete(directoryPath, recursive: true);
            }
        } catch {
            // Ignore cleanup errors
        }
    }

    [Fact]
    public void AddSecret_WhenUserAddsSecret_SecretIsPersisted() {
        // Arrange
        var console = new TestConsole();
        console.Interactive();
        var tool = CreateTool(console);
        var secretsStore = CreateSecretsStore();

        // Simulate: A (add secret), enter key name, enter value, then Q (quit)
        console.Input.PushText("A");
        console.Input.PushTextWithEnter("TestKey");
        console.Input.PushTextWithEnter("TestValue");
        console.Input.PushText("Q");

        // Act
        tool.MainLoop(null, secretsStore);

        // Assert - Verify the secret was persisted by reading from the filesystem
        var verificationStore = CreateSecretsStore();
        verificationStore.ContainsKey("TestKey").ShouldBeTrue();
        verificationStore["TestKey"].ShouldBe("TestValue");

        // Also verify using dotnet user-secrets list
        var listOutput = RunDotnetUserSecretsList();
        listOutput.ShouldContain("TestKey");
        listOutput.ShouldContain("TestValue");
    }

    [Fact]
    public void EditSecret_WhenUserEditsSecret_SecretIsUpdated() {
        // Arrange
        var console = new TestConsole();
        console.Interactive();
        var tool = CreateTool(console);
        var secretsStore = CreateSecretsStore();

        // Pre-populate with an existing secret
        secretsStore.Set("ExistingKey", "OriginalValue");
        secretsStore.Save();

        // Simulate: E (edit secret), select the key (Enter to select first), clear existing value, enter new value, then Q (quit)
        console.Input.PushText("E");
        console.Input.PushKey(ConsoleKey.Enter); // Select first (only) item
        // Clear the existing value "OriginalValue" (13 chars) using Home + Delete
        console.Input.PushKey(ConsoleKey.Home);
        for(var i = 0; i < "OriginalValue".Length; i++) {
            console.Input.PushKey(ConsoleKey.Delete);
        }

        // Type each character individually using PushKey
        console.Input.PushKey(ConsoleKey.U);
        console.Input.PushText("pdatedValue");
        console.Input.PushKey(ConsoleKey.Enter);
        console.Input.PushText("Q");

        // Act
        tool.MainLoop(null, secretsStore);

        // Assert - Verify the secret was updated by reading from the filesystem
        var verificationStore = CreateSecretsStore();
        verificationStore.ContainsKey("ExistingKey").ShouldBeTrue();
        verificationStore["ExistingKey"].ShouldBe("UpdatedValue");

        // Also verify using dotnet user-secrets list
        var listOutput = RunDotnetUserSecretsList();
        listOutput.ShouldContain("ExistingKey");
        listOutput.ShouldContain("UpdatedValue");
        listOutput.ShouldNotContain("OriginalValue");
    }

    [Fact]
    public void RemoveSecret_WhenUserRemovesSecret_SecretIsDeleted() {
        // Arrange
        var console = new TestConsole();
        console.Interactive();
        var tool = CreateTool(console);
        var secretsStore = CreateSecretsStore();

        // Pre-populate with an existing secret
        secretsStore.Set("SecretToRemove", "SomeValue");
        secretsStore.Save();

        // Simulate: D (delete secret), select the key (Enter to select first), confirm (y), then Q (quit)
        console.Input.PushText("D");
        console.Input.PushKey(ConsoleKey.Enter); // Select first (only) item
        console.Input.PushTextWithEnter("y"); // Confirm deletion
        console.Input.PushText("Q");

        // Act
        tool.MainLoop(null, secretsStore);

        // Assert - Verify the secret was removed by reading from the filesystem
        var verificationStore = CreateSecretsStore();
        verificationStore.ContainsKey("SecretToRemove").ShouldBeFalse();

        // Also verify using dotnet user-secrets list
        var listOutput = RunDotnetUserSecretsList();
        listOutput.ShouldNotContain("SecretToRemove");
        listOutput.ShouldNotContain("SomeValue");
    }

    [Fact]
    public void AddEditRemove_FullWorkflow_SecretsAreManagedCorrectly() {
        // Arrange
        var console = new TestConsole();
        console.Interactive();
        var tool = CreateTool(console);
        var secretsStore = CreateSecretsStore();

        // Step 1: Add a secret
        console.Input.PushText("A");
        console.Input.PushTextWithEnter("WorkflowKey");
        console.Input.PushTextWithEnter("InitialValue");

        // Step 2: Edit the secret (it will be the only one, so Enter selects it)
        console.Input.PushText("E");
        console.Input.PushKey(ConsoleKey.Enter);
        // Clear the existing value "InitialValue" (12 chars) using Home + Delete
        console.Input.PushKey(ConsoleKey.Home);
        for(var i = 0; i < "InitialValue".Length; i++) {
            console.Input.PushKey(ConsoleKey.Delete);
        }

        console.Input.PushKey(ConsoleKey.M);
        console.Input.PushText("odifiedValue");
        console.Input.PushKey(ConsoleKey.Enter);

        // Step 3: Add another secret (sorted order: SecondKey, WorkflowKey)
        console.Input.PushText("A");
        console.Input.PushTextWithEnter("SecondKey");
        console.Input.PushTextWithEnter("SecondValue");

        // Step 4: Remove SecondKey (first in sorted list, so just press Enter)
        console.Input.PushText("D");
        console.Input.PushKey(ConsoleKey.Enter); // Select first item (SecondKey)
        console.Input.PushTextWithEnter("y"); // Confirm deletion

        // Step 5: Quit
        console.Input.PushText("Q");

        // Act
        tool.MainLoop(null, secretsStore);

        // Assert
        var verificationStore = CreateSecretsStore();

        // WorkflowKey should be present with ModifiedValue
        verificationStore.ContainsKey("WorkflowKey").ShouldBeTrue();
        verificationStore["WorkflowKey"].ShouldBe("ModifiedValue");

        // SecondKey should be removed
        verificationStore.ContainsKey("SecondKey").ShouldBeFalse();

        // Verify with dotnet user-secrets list
        var listOutput = RunDotnetUserSecretsList();
        listOutput.ShouldContain("WorkflowKey");
        listOutput.ShouldContain("ModifiedValue");
        listOutput.ShouldNotContain("SecondKey");
        listOutput.ShouldNotContain("InitialValue");
    }

    private Tool CreateTool(TestConsole console) {
        var projectFinder = A.Dummy<IMsBuildProjectFinder>();
        var projectIdResolver = A.Dummy<IProjectIdResolver>();
        var secretsStoreFactory = A.Dummy<ISecretsStoreFactory>();

        return new Tool(console, _fileSystem, projectFinder, projectIdResolver, secretsStoreFactory);
    }

    private SecretsStore CreateSecretsStore() {
        return new SecretsStore(_testUserSecretsId, _fileSystem);
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
