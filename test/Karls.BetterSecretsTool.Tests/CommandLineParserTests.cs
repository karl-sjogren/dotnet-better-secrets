using System.IO.Abstractions.TestingHelpers;

namespace Karls.BetterSecretsTool.Tests;

public class CommandLineParserTests {
    [Fact]
    public void ParseArguments_WhenNoArguments_ReturnsExpectedOptions() {
        // Arrange
        var fileSystem = new MockFileSystem();
        var parser = new CommandLineParser(fileSystem);

        // Act
        var result = parser.ParseArguments([]);

        // Assert
        result.ShouldBe(new CommandLineOptions(
            WorkingDirectory: fileSystem.Directory.GetCurrentDirectory(),
            UserSecretsId: null,
            BuildConfiguration: "Debug",
            ShowHelp: false));
    }

    [Fact]
    public void ParseArguments_WhenShortFormArgumentsProvided_ReturnsExpectedOptions() {
        // Arrange
        var fileSystem = new MockFileSystem();
        var parser = new CommandLineParser(fileSystem);
        var args = new[] {
            "/path/to/project",
            "-i", "my-secrets-id",
            "-c", "Release"
        };

        // Act
        var result = parser.ParseArguments(args);

        // Assert
        result.ShouldBe(new CommandLineOptions(
            WorkingDirectory: "/path/to/project",
            UserSecretsId: "my-secrets-id",
            BuildConfiguration: "Release",
            ShowHelp: false));
    }

    [Fact]
    public void ParseArguments_WhenLongFormArgumentsProvided_ReturnsExpectedOptions() {
        // Arrange
        var fileSystem = new MockFileSystem();
        var parser = new CommandLineParser(fileSystem);
        var args = new[] {
            "/path/to/project",
            "--id", "my-secrets-id",
            "--configuration", "Release"
        };

        // Act
        var result = parser.ParseArguments(args);

        // Assert
        result.ShouldBe(new CommandLineOptions(
            WorkingDirectory: "/path/to/project",
            UserSecretsId: "my-secrets-id",
            BuildConfiguration: "Release",
            ShowHelp: false));
    }

    [Fact]
    public void ParseArguments_WhenCalledWithUnknownDashedArguments_IgnoresUnknownArguments() {
        // Arrange
        var fileSystem = new MockFileSystem();
        var parser = new CommandLineParser(fileSystem);
        var args = new[] {
            "/path/to/project",
            "--id", "my-secrets-id",
            "--configuration", "Release",
            "--zebra", "some-value",
            "-z", "another-value"
        };

        // Act
        var result = parser.ParseArguments(args);

        // Assert
        result.ShouldBe(new CommandLineOptions(
            WorkingDirectory: "/path/to/project",
            UserSecretsId: "my-secrets-id",
            BuildConfiguration: "Release",
            ShowHelp: false));
    }

    [Fact]
    public void ParseArguments_WhenCalledWithUnknownDashedArgumentsWithoutValues_IgnoresUnknownArguments() {
        // Arrange
        var fileSystem = new MockFileSystem();
        var parser = new CommandLineParser(fileSystem);
        var args = new[] {
            "/path/to/project",
            "--id", "my-secrets-id",
            "--zebra", "--configuration", "Release",
            "-z", "another-value"
        };

        // Act
        var result = parser.ParseArguments(args);

        // Assert
        result.ShouldBe(new CommandLineOptions(
            WorkingDirectory: "/path/to/project",
            UserSecretsId: "my-secrets-id",
            BuildConfiguration: "Release",
            ShowHelp: false));
    }

    [Theory]
    [InlineData("-h")]
    [InlineData("--help")]
    public void ParseArguments_WhenHelpArgumentProvided_IgnoresRestOfArguments(string helpFlag) {
        // Arrange
        var fileSystem = new MockFileSystem();
        var parser = new CommandLineParser(fileSystem);
        var args = new[] {
            "/path/to/project",
            "--id", "my-secrets-id",
            "--configuration", "Release",
            helpFlag
        };

        // Act
        var result = parser.ParseArguments(args);

        // Assert
        result.ShouldBe(new CommandLineOptions(
            WorkingDirectory: null,
            UserSecretsId: null,
            BuildConfiguration: null,
            ShowHelp: true));
    }
}
