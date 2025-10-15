using System.IO.Abstractions;
using Azure;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Karls.BetterSecretsTool.Vendor;
using Spectre.Console;

namespace Karls.BetterSecretsTool;

public static class Program {
    internal static IFileSystem FileSystem { get; set; } = new FileSystem();
    internal static IAnsiConsole Console { get; set; } = AnsiConsole.Create(new());

    public static void Main(string[] args) {
        var options = ParseArguments(args);

        if(options.ShowHelp) {
            RenderHelpMessage();
            return;
        }

        var id = options.UserSecretsId;
        string? keyVaultName = null;

        if(string.IsNullOrWhiteSpace(id)) {
            var directory = options.WorkingDirectory;

            if(!FileSystem.Directory.Exists(directory)) {
                Console.MarkupLineInterpolated($"[red]Error:[/] The specified directory '[grey]{directory}[/]' does not exist.");
                return;
            }

            var result = ResolveId(directory, options.BuildConfiguration);
            if(result is null) {
                Console.MarkupLineInterpolated($"[red]Error:[/] Could not find a .NET project in the specified directory '[grey]{directory}[/]' to resolve the User Secrets ID from.");
                return;
            }

            id = result.UserSecretsId;
            keyVaultName = result.UserSecretsKeyVault;
        }

        if(string.IsNullOrWhiteSpace(id)) {
            Console.MarkupLine("[red]Error:[/] Could not determine User Secrets ID for the selected project.");
            return;
        }

        var secretStore = new SecretsStore(id, FileSystem);

        while(true) {
            RenderTable(secretStore);

            Console.WriteLine();
            Console.MarkupLine("[grey]Type [green]A[/] to add, [green]E[/] to edit or [green]D[/] to delete secrets. Type [green]S[/] to show single value.[/]");

            if(!string.IsNullOrWhiteSpace(keyVaultName)) {
                Console.MarkupLineInterpolated($"[grey]Type [green]K[/] to download secrets from key vault [yellow]{keyVaultName}[/][/].");
            }

            Console.MarkupLine("[grey]Press [green]Enter[/] to exit[/]");

            var prompt = Console.Input.ReadKey(false).GetValueOrDefault().KeyChar.ToString();

            prompt = prompt.Trim().ToUpperInvariant();
            if(string.IsNullOrWhiteSpace(prompt) || prompt == "Q") {
                return;
            }

            try {
                Console.Clear();
            } catch {
                // Can fail in some environments, e.g. when piping output to a file or running from VSCode
            }

            if(prompt == "A") {
                AddSecret(secretStore);
            } else if(prompt == "E") {
                EditSecret(secretStore);
            } else if(prompt == "D") {
                RemoveSecret(secretStore);
            } else if(prompt == "S") {
                ShowSecret(secretStore);
            } else if(prompt == "K" && !string.IsNullOrWhiteSpace(keyVaultName)) {
                DownloadFromKeyVault(secretStore, keyVaultName);
            }
        }
    }

    private static void DownloadFromKeyVault(SecretsStore secretStore, string keyVaultName) {
        Console.Clear();
        Console.MarkupLineInterpolated($"[grey]Downloading secrets from key vault [yellow]{keyVaultName}[/][/].");

        var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions {
            ExcludeInteractiveBrowserCredential = false
        });

        Action? afterStatusAction = null;

        AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .Start("Authenticating with Azure...", ctx => {
                var client = new SecretClient(new Uri($"https://{keyVaultName}.vault.azure.net/"), credential);

                ctx.Status = "Downloading secret keys...";
                List<SecretProperties> secrets;

                try {
                    secrets = client.GetPropertiesOfSecrets().ToList();
                } catch(AggregateException ex) when(ex.InnerException is RequestFailedException rfe) {
                    afterStatusAction = () => {
                        Console.Clear();
                        Console.MarkupLineInterpolated($"[red]Error:[/] Could not access key vault [yellow]{keyVaultName}[/]: {rfe.Message}");
                    };
                    return;
                } catch(Exception ex) {
                    afterStatusAction = () => {
                        Console.Clear();
                        Console.MarkupLineInterpolated($"[red]Error:[/] Could not access key vault [yellow]{keyVaultName}[/]: {ex.Message}");
                    };
                    return;
                }

                foreach(var secretProperties in secrets) {
                    try {
                        ctx.Status = $"Downloading secret [green]{secretProperties.Name}[/]...";
                        var secret = client.GetSecret(secretProperties.Name);
                        if(secret?.Value is not null) {
                            var fixedKey = secretProperties.Name.Replace("--", ":", StringComparison.Ordinal);
                            secretStore.Set(fixedKey, secret.Value.Value);
                        }
                    } catch(RequestFailedException ex) when(ex.Status == 404) {
                        // A secret was deleted after we listed them. Ignore.
                    } catch(AggregateException ex) when(ex.InnerException is RequestFailedException rfe) {
                        afterStatusAction = () => {
                            Console.Clear();
                            Console.MarkupLineInterpolated($"[red]Error:[/] Could not access value of secret [yellow]{secretProperties.Name}[/]: {rfe.Message}");
                        };
                        return;
                    } catch(Exception ex) {
                        afterStatusAction = () => {
                            Console.Clear();
                            Console.MarkupLineInterpolated($"[red]Error:[/] Could not access value of secret [yellow]{secretProperties.Name}[/]: {ex.Message}");
                        };
                        return;
                    }
                }
            });

        secretStore.Save();
        if(afterStatusAction is not null) {
            afterStatusAction?.Invoke();
            Console.MarkupLine("[grey]Press any key to continue...[/]");
            Console.Input.ReadKey(true);
        }
    }

    private static void ShowSecret(SecretsStore secretStore) {
        var key = SelectKey(secretStore, "[grey]Select a secret to show:[/]");
        Console.MarkupLineInterpolated($"[grey]Value for [green]{key}[/][/]:");
        Console.MarkupLineInterpolated($"[yellow]{secretStore[key]}[/]");
        Console.MarkupLine("[grey]Press any key to continue...[/]");
        Console.Input.ReadKey(true);
    }

    private static void RemoveSecret(SecretsStore secretStore) {
        var key = SelectKey(secretStore, "[grey]Select a secret to delete:[/]");
        secretStore.Remove(key);
        secretStore.Save();
    }

    private static void EditSecret(SecretsStore secretStore) {
        var key = SelectKey(secretStore, "[grey]Select a secret to edit:[/]");

        Console.MarkupLine($"[grey]Editing secret [green]{key}[/][/].");
        Console.MarkupLineInterpolated($"[grey]Current value: [yellow]{secretStore[key]}[/][/]");
        var newValue = Console.Ask<string>("[grey]Enter new value:[/]");
        secretStore.Set(key, newValue);
        secretStore.Save();
    }

    private static void AddSecret(SecretsStore secretStore) {
        var key = Console.Ask<string>("[grey]Enter secret [green]key[/][/]:");
        var value = Console.Ask<string>("[grey]Enter secret [yellow]value[/][/]:");
        secretStore.Set(key, value);
        secretStore.Save();
    }

    private static void RenderHelpMessage() {
        Console.MarkupLine("[bold]Karls Better Secrets Tool[/]");
        Console.MarkupLine("An easier way to manage your .NET User Secrets from the command line.");
        Console.WriteLine();
        Console.MarkupLine("Usage: [green]better-secrets[/] [yellow]<working-directory>[/] [grey][[options]][/]");
        Console.WriteLine();
        Console.MarkupLine("Arguments:");
        Console.MarkupLine("  [yellow]<working-directory>[/]  The working directory containing the .NET project to manage secrets for. If not specified, the current directory will be used.");
        Console.WriteLine();
        Console.MarkupLine("Options:");
        Console.MarkupLine("  [green]-i[/], [green]--id[/]             The User Secrets ID to use. If not specified, the ID will be resolved from the project in the working directory.");
        Console.MarkupLine("  [green]-c[/], [green]--configuration[/]  The build configuration to use when resolving the project. Defaults to 'Debug'.");
        Console.MarkupLine("  [green]-h[/], [green]--help[/]           Show this help message.");
        Console.WriteLine();
        Console.MarkupLine("Examples:");
        Console.MarkupLine("  [grey]better-secrets -d ./MyProject[/]        Manage secrets for the project in ./MyProject");
        Console.MarkupLine("  [grey]better-secrets -i <user-secrets-id>[/]  Manage secrets for the specified User Secrets ID");
    }

    private static string SelectKey(SecretsStore secretStore, string title) {
        var selection =
            new SelectionPrompt<string>()
                .Title(title)
                .PageSize(10)
                .MoreChoicesText("[grey](Move up and down to reveal more secrets)[/]")
                .AddChoices([.. secretStore.AsSortedEnumerable().Select(kvp => kvp.Key)]);

        return Console.Prompt(selection);
    }

    private static void RenderTable(SecretsStore secretStore) {
        var table = new Table()
                    .Border(TableBorder.Rounded)
                    .BorderColor(Color.Grey37)
                    .Caption("[grey]Karls Better Secrets Tool[/]")
                    .ShowRowSeparators()
                    .Expand();

        var longestKey = secretStore.AsEnumerable().Select(kvp => kvp.Key.Length).DefaultIfEmpty(0).Max();

        try {
            Console.Clear();
        } catch {
            // Can fail in some environments, e.g. when piping output to a file or running from VSCode
        }

        var keyColumn = new TableColumn(new Markup("[bold green]Key[/]"))
            .NoWrap()
            .Width(longestKey + 2);

        var valueColumn = new TableColumn(new Markup("[bold yellow]Value[/]"))
            .NoWrap()
            .Width(AnsiConsole.Console.Profile.Width - keyColumn.Width - 4);

        table.AddColumn(keyColumn);
        table.AddColumn(valueColumn);

        foreach(var secret in secretStore.AsSortedEnumerable()) {
            var keyMarkup = new Markup($"[green]{Markup.Escape(secret.Key)}[/]");

            var valueMarkup = new Markup($"[yellow]{Markup.Escape(secret.Value)}[/]");

            table.AddRow(keyMarkup, valueMarkup);
        }

        Console.Write(table);
    }

    private static ResolveResult? ResolveId(string workingDirectory, string? buildConfiguration) {
        var resolver = new ProjectIdResolver(FileSystem);

        var finder = new MsBuildProjectFinder(workingDirectory, FileSystem);
        string projectFile;
        try {
            projectFile = finder.FindMsBuildProject("");
        } catch(Exception) {
            return null;
        }

        return resolver.Resolve(projectFile, buildConfiguration ?? "Debug");
    }

    private static CommandLineOptions ParseArguments(string[] args) {
        string? workingDirectory = null;
        string? userSecretsId = null;
        string? configuration = null;

        for(var i = 0; i < args.Length; i++) {
            var arg = args[i];
            if(!arg.StartsWith("-", StringComparison.Ordinal)) {
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
            }
        }

        workingDirectory ??= FileSystem.Directory.GetCurrentDirectory();
        configuration ??= "Debug";

        return new CommandLineOptions(workingDirectory, userSecretsId, configuration, false);
    }
}

public record CommandLineOptions(string? WorkingDirectory, string? UserSecretsId, string? BuildConfiguration, bool ShowHelp);
