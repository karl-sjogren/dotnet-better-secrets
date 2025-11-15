using System.IO.Abstractions;
using System.Text.Json;
using Azure;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Karls.BetterSecretsTool.Contracts;
using Karls.BetterSecretsTool.Extensions;
using Karls.BetterSecretsTool.Vendor;
using Spectre.Console;
using Spectre.Console.Json;

namespace Karls.BetterSecretsTool;

public static class Program {
    internal static IFileSystem FileSystem { get; set; } = new FileSystem();
    internal static IAnsiConsole Console { get; set; } = AnsiConsole.Create(new());
    internal static IMsBuildProjectFinder ProjectFinder { get; set; } = new MsBuildProjectFinder(FileSystem);
    internal static IProjectIdResolver ProjectIdResolver { get; set; } = new ProjectIdResolver(FileSystem);

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

            var result = ResolveUserSecretsId(directory, options.BuildConfiguration);
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
        MainLoop(keyVaultName, secretStore);
    }

    internal static void MainLoop(string? keyVaultName, ISecretsStore secretStore) {
        while(true) {
            RenderTable(secretStore);

            Console.WriteLine();
            Console.MarkupLine("[grey]Type [green]A[/] to add, [green]E[/] to edit or [green]D[/] to delete secrets. Type [green]S[/] to show single value or [green]J[/] to show all values as JSON.[/]");

            if(!string.IsNullOrWhiteSpace(keyVaultName)) {
                Console.MarkupLineInterpolated($"[grey]Type [green]K[/] to download secrets from key vault [yellow]{keyVaultName}[/][/].");
            }

            Console.MarkupLine("[grey]Press [green]Enter[/] to exit[/]");

            var prompt = Console.Input.ReadKey(false).GetValueOrDefault().KeyChar.ToString();

            prompt = prompt.Trim().ToUpperInvariant();
            if(string.IsNullOrWhiteSpace(prompt) || prompt == "Q") {
                Console.ClearSafe();
                return;
            }

            Console.ClearSafe();

            if(prompt == "A") {
                AddSecret(secretStore);
            } else if(prompt == "E") {
                EditSecret(secretStore);
            } else if(prompt == "D") {
                RemoveSecret(secretStore);
            } else if(prompt == "S") {
                ShowSecret(secretStore);
            } else if(prompt == "J") {
                ShowSecretJson(secretStore);
            } else if(prompt == "K" && !string.IsNullOrWhiteSpace(keyVaultName)) {
                DownloadFromKeyVault(secretStore, keyVaultName);
            }
        }
    }

    private static void DownloadFromKeyVault(ISecretsStore secretStore, string keyVaultName) {
        Console.ClearSafe();
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
                        Console.ClearSafe();
                        Console.MarkupLineInterpolated($"[red]Error:[/] Could not access key vault [yellow]{keyVaultName}[/]: {rfe.Message}");
                    };
                    return;
                } catch(Exception ex) {
                    afterStatusAction = () => {
                        Console.ClearSafe();
                        Console.MarkupLineInterpolated($"[red]Error:[/] Could not access key vault [yellow]{keyVaultName}[/]: {ex.Message}");
                    };
                    return;
                }

                foreach(var secretName in secrets.Select(s => s.Name)) {
                    try {
                        ctx.Status = $"Downloading secret [green]{secretName}[/]...";
                        var secret = client.GetSecret(secretName);
                        if(secret?.Value is not null) {
                            var fixedKey = secretName.Replace("--", ":", StringComparison.Ordinal);
                            secretStore.Set(fixedKey, secret.Value.Value);
                        }
                    } catch(RequestFailedException ex) when(ex.Status == 404) {
                        // A secret was deleted after we listed them. Ignore.
                    } catch(AggregateException ex) when(ex.InnerException is RequestFailedException rfe) {
                        afterStatusAction = () => {
                            Console.ClearSafe();
                            Console.MarkupLineInterpolated($"[red]Error:[/] Could not access value of secret [yellow]{secretName}[/]: {rfe.Message}");
                        };
                        return;
                    } catch(Exception ex) {
                        afterStatusAction = () => {
                            Console.ClearSafe();
                            Console.MarkupLineInterpolated($"[red]Error:[/] Could not access value of secret [yellow]{secretName}[/]: {ex.Message}");
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

    private static void ShowSecret(ISecretsStore secretStore) {
        var key = SelectKey(secretStore, "[grey]Select a secret to show:[/]");
        Console.MarkupLineInterpolated($"[grey]Value for [green]{key}[/][/]:");
        Console.MarkupLineInterpolated($"[yellow]{secretStore[key]}[/]");
        Console.MarkupLine("[grey]Press any key to continue...[/]");
        Console.Input.ReadKey(true);
    }

    private static void ShowSecretJson(ISecretsStore secretStore) {
        var dict = secretStore.AsEnumerable().ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        var json = JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true });
        var jsonText = new JsonText(json);
        Console.Write(jsonText);
        Console.WriteLine();
        Console.WriteLine();
        Console.MarkupLine("[grey]Press any key to continue...[/]");
        Console.Input.ReadKey(true);
    }

    private static void RemoveSecret(ISecretsStore secretStore) {
        var key = SelectKey(secretStore, "[grey]Select a secret to delete:[/]");
        secretStore.Remove(key);
        secretStore.Save();
    }

    private static void EditSecret(ISecretsStore secretStore) {
        var key = SelectKey(secretStore, "[grey]Select a secret to edit:[/]");

        Console.MarkupLine($"[grey]Editing secret [green]{key}[/][/].");
        Console.MarkupLineInterpolated($"[grey]Current value: [yellow]{secretStore[key]}[/][/]");
        var newValue = Console.Ask<string>("[grey]Enter new value:[/]");
        secretStore.Set(key, newValue);
        secretStore.Save();
    }

    private static void AddSecret(ISecretsStore secretStore) {
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
        Console.WriteLine();
        Console.MarkupLine("For more information, visit [blue underline]https://github.com/karl-sjogren/dotnet-better-secrets[/]");
    }

    private static string SelectKey(ISecretsStore secretStore, string title) {
        var selectionSize = Math.Max(5, Math.Min(15, Console.Profile.Height - 3));
        var selection =
            new SelectionPrompt<string>()
                .Title(title)
                .PageSize(selectionSize)
                .MoreChoicesText("[grey](Move up and down to reveal more secrets)[/]")
                .AddChoices([.. secretStore.AsSortedEnumerable().Select(kvp => kvp.Key)]);

        return Console.Prompt(selection);
    }

    internal static void RenderTable(ISecretsStore secretStore) {
        var version = typeof(Program).Assembly.GetName().Version?.ToString(3) ?? "unknown version";

        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey37)
            .Caption($"[grey]Karls Better Secrets Tool {version}[/]")
            .ShowRowSeparators()
            .Expand();

        var longestKey = secretStore.AsEnumerable().Select(kvp => kvp.Key.Length).DefaultIfEmpty(0).Max();
        longestKey = Math.Max(longestKey, 3);

        Console.ClearSafe();

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

    private static ResolveResult? ResolveUserSecretsId(string workingDirectory, string? buildConfiguration) {
        MsBuildProject[] projects;
        try {
            projects = ProjectFinder.FindMsBuildProjects(workingDirectory);

            if(projects.Length == 0) {
                return null;
            }

            var projectFile = projects[0].Path;
            if(projects.Length > 1) {
                projectFile = projects
                    .OrderBy(p => p.AtRoot ? 0 : 1)
                    .ThenBy(p => p.IsWebSdk ? 0 : 1)
                    .First()
                    .Path;

                Console.MarkupLineInterpolated($"[yellow]Warning:[/] Multiple .NET projects found in directory '[grey]{workingDirectory}[/]'. Using: '[grey]{projectFile}[/]'.");
            }

            return ProjectIdResolver.Resolve(projectFile, buildConfiguration ?? "Debug");
        } catch(Exception) {
            return null;
        }
    }

    private static CommandLineOptions ParseArguments(string[] args) {
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
            }
        }

        workingDirectory ??= FileSystem.Directory.GetCurrentDirectory();
        configuration ??= "Debug";

        return new CommandLineOptions(workingDirectory, userSecretsId, configuration, false);
    }
}

public record CommandLineOptions(string? WorkingDirectory, string? UserSecretsId, string? BuildConfiguration, bool ShowHelp);
