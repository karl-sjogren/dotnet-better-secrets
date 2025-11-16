using System.IO.Abstractions;
using Karls.BetterSecretsTool.Contracts;

namespace Karls.BetterSecretsTool;

internal class CommandLineParser : ICommandLineParser {
    private readonly IFileSystem _fileSystem;

    public CommandLineParser(IFileSystem fileSystem) {
        _fileSystem = fileSystem;
    }

    public CommandLineOptions ParseArguments(string[] args) {
        string? workingDirectory = null;
        string? userSecretsId = null;
        string? configuration = null;

        for(var i = 0; i < args.Length; i++) {
            var arg = args[i];
            if(!arg.StartsWith('-')) {
                workingDirectory = args[i];
            } else if(arg == "-i" || arg == "--id") {
                if(i + 1 < args.Length) {
                    userSecretsId = args[i + 1];
                    i++;
                }
            } else if(arg == "-c" || arg == "--configuration") {
                if(i + 1 < args.Length) {
                    configuration = args[i + 1];
                    i++;
                }
            } else if(arg == "-h" || arg == "--help") {
                return new CommandLineOptions(null, null, null, true);
            } else {
                // Skip the next argument if it does not start with '-'
                if(i + 1 < args.Length && !args[i + 1].StartsWith('-')) {
                    i++;
                }
            }
        }

        workingDirectory ??= _fileSystem.Directory.GetCurrentDirectory();
        configuration ??= "Debug";

        return new CommandLineOptions(workingDirectory, userSecretsId, configuration, false);
    }
}

public record CommandLineOptions(string? WorkingDirectory, string? UserSecretsId, string? BuildConfiguration, bool ShowHelp);
