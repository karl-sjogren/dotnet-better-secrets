using System.IO.Abstractions;
using Karls.BetterSecretsTool.Vendor;
using Spectre.Console;

namespace Karls.BetterSecretsTool;

[ExcludeFromCodeCoverage(Justification = "Contains only the tool entry point.")]
public static class Program {
    public static void Main(string[] args) {
        var fileSystem = new FileSystem();
        var console = AnsiConsole.Create(new());
        var projectFinder = new MsBuildProjectFinder(fileSystem);
        var projectIdResolver = new ProjectIdResolver(fileSystem);
        var secretsStoreFactory = new SecretsStoreFactory(fileSystem);

        var tool = new Tool(console, fileSystem, projectFinder, projectIdResolver, secretsStoreFactory);

        try {
            tool.Run(args);
        } catch(Exception ex) {
            console.MarkupLineInterpolated($"[red]Fatal Error:[/]");
            console.WriteException(ex, ExceptionFormats.ShortenEverything);
        }
    }
}
