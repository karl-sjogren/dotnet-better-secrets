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

internal class Tool {
    private readonly IAnsiConsole _console;
    private readonly IFileSystem _fileSystem;
    private readonly IMsBuildProjectFinder _projectFinder;
    private readonly IProjectIdResolver _projectIdResolver;
    private readonly ISecretsStoreFactory _secretsStoreFactory;

    private readonly JsonSerializerOptions _jsonOptions = new() {
        WriteIndented = true
    };

    internal Tool(
            IAnsiConsole console,
            IFileSystem fileSystem,
            IMsBuildProjectFinder projectFinder,
            IProjectIdResolver projectIdResolver,
            ISecretsStoreFactory secretsStoreFactory) {
        _console = console;
        _fileSystem = fileSystem;
        _projectFinder = projectFinder;
        _projectIdResolver = projectIdResolver;
        _secretsStoreFactory = secretsStoreFactory;
    }

    public void Run(string[] args) {
        var commandLineParser = new CommandLineParser(_fileSystem);
        var options = commandLineParser.ParseArguments(args);

        if(options.ShowHelp) {
            RenderHelpMessage();
            return;
        }

        var id = options.UserSecretsId;
        string? keyVaultName = null;

        if(string.IsNullOrWhiteSpace(id)) {
            var directory = options.WorkingDirectory;

            if(!_fileSystem.Directory.Exists(directory)) {
                _console.MarkupLineInterpolated($"[red]Error:[/] The specified directory '[grey]{directory}[/]' does not exist.");
                return;
            }

            var result = ResolveUserSecretsId(directory, options.BuildConfiguration);
            if(result is null) {
                _console.MarkupLineInterpolated($"[red]Error:[/] Could not find a .NET project in the specified directory '[grey]{directory}[/]' to resolve the User Secrets ID from.");
                return;
            }

            id = result.UserSecretsId;
            keyVaultName = result.UserSecretsKeyVault;
        }

        if(string.IsNullOrWhiteSpace(id)) {
            _console.MarkupLine("[red]Error:[/] Could not determine User Secrets ID for the selected project.");
            return;
        }

        var secretStore = _secretsStoreFactory.Create(id);
        MainLoop(keyVaultName, secretStore);
    }

    internal void MainLoop(string? keyVaultName, ISecretsStore secretsStore) {
        while(true) {
            RenderTable(secretsStore);

            _console.WriteLine();
            _console.MarkupLine("[grey]Type [green]A[/] to add, [green]E[/] to edit or [green]D[/] to delete secrets. Type [green]S[/] to show single value or [green]J[/] to show all values as JSON.[/]");

            if(!string.IsNullOrWhiteSpace(keyVaultName)) {
                _console.MarkupLineInterpolated($"[grey]Type [green]K[/] to download secrets from key vault [yellow]{keyVaultName}[/][/].");
            }

            _console.MarkupLine("[grey]Press [green]Enter[/] to exit.[/]");

            var shouldExit = HandleInput(keyVaultName, secretsStore);
            if(shouldExit) {
                return;
            }
        }
    }

    /// <summary>
    /// Handles user input and performs the corresponding action.
    /// </summary>
    /// <returns>True if the input indicates the tool should exit.</returns>
    internal bool HandleInput(string? keyVaultName, ISecretsStore secretsStore) {
        var prompt = _console.Input.ReadKey(false).GetValueOrDefault().KeyChar.ToString();

        _console.ClearSafe();

        prompt = prompt.Trim().ToUpperInvariant();
        if(string.IsNullOrWhiteSpace(prompt) || prompt == "Q") {
            return true;
        }

        if(prompt == "A") {
            AddSecret(secretsStore);
        } else if(prompt == "E") {
            EditSecret(secretsStore);
        } else if(prompt == "D") {
            RemoveSecret(secretsStore);
        } else if(prompt == "S") {
            ShowSecret(secretsStore);
        } else if(prompt == "J") {
            ShowSecretJson(secretsStore);
        } else if(prompt == "K" && !string.IsNullOrWhiteSpace(keyVaultName)) {
            DownloadFromKeyVault(secretsStore, keyVaultName);
        }

        return false;
    }

    private void DownloadFromKeyVault(ISecretsStore secretStore, string keyVaultName) {
        _console.ClearSafe();
        _console.MarkupLineInterpolated($"[grey]Downloading secrets from key vault [yellow]{keyVaultName}[/][/].");

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
                        _console.ClearSafe();
                        _console.MarkupLineInterpolated($"[red]Error:[/] Could not access key vault [yellow]{keyVaultName}[/]: {rfe.Message}");
                    };
                    return;
                } catch(Exception ex) {
                    afterStatusAction = () => {
                        _console.ClearSafe();
                        _console.MarkupLineInterpolated($"[red]Error:[/] Could not access key vault [yellow]{keyVaultName}[/]: {ex.Message}");
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
                            _console.ClearSafe();
                            _console.MarkupLineInterpolated($"[red]Error:[/] Could not access value of secret [yellow]{secretName}[/]: {rfe.Message}");
                        };
                        return;
                    } catch(Exception ex) {
                        afterStatusAction = () => {
                            _console.ClearSafe();
                            _console.MarkupLineInterpolated($"[red]Error:[/] Could not access value of secret [yellow]{secretName}[/]: {ex.Message}");
                        };
                        return;
                    }
                }
            });

        secretStore.Save();
        if(afterStatusAction is not null) {
            afterStatusAction?.Invoke();
            _console.MarkupLine("[grey]Press any key to continue...[/]");
            _console.Input.ReadKey(true);
        }
    }

    private void ShowSecret(ISecretsStore secretStore) {
        var key = SelectKey(secretStore, "[grey]Select a secret to show:[/]");
        _console.MarkupLineInterpolated($"[grey]Value for [green]{key}[/][/]:");
        _console.MarkupLineInterpolated($"[yellow]{secretStore[key]}[/]");
        _console.MarkupLine("[grey]Press any key to continue...[/]");
        _console.Input.ReadKey(true);
    }

    private void ShowSecretJson(ISecretsStore secretStore) {
        var dict = secretStore.AsEnumerable().ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        var json = JsonSerializer.Serialize(dict, _jsonOptions);
        var jsonText = new JsonText(json);
        _console.Write(jsonText);
        _console.WriteLine();
        _console.WriteLine();
        _console.MarkupLine("[grey]Press any key to continue...[/]");
        _console.Input.ReadKey(true);
    }

    private void RemoveSecret(ISecretsStore secretStore) {
        var key = SelectKey(secretStore, "[grey]Select a secret to delete:[/]");
        secretStore.Remove(key);
        secretStore.Save();
    }

    private void EditSecret(ISecretsStore secretStore) {
        var key = SelectKey(secretStore, "[grey]Select a secret to edit:[/]");

        _console.MarkupLine($"[grey]Editing secret [green]{key}[/][/].");
        _console.MarkupLineInterpolated($"[grey]Current value: [yellow]{secretStore[key]}[/][/]");
        var newValue = _console.Ask<string>("[grey]Enter new value:[/]");
        secretStore.Set(key, newValue);
        secretStore.Save();
    }

    private void AddSecret(ISecretsStore secretStore) {
        var key = _console.Ask<string>("[grey]Enter secret [green]key[/][/]:");
        var value = _console.Ask<string>("[grey]Enter secret [yellow]value[/][/]:");
        secretStore.Set(key, value);
        secretStore.Save();
    }

    internal void RenderHelpMessage() {
        _console.MarkupLine("[bold]Karls Better Secrets Tool[/]");
        _console.MarkupLine("An easier way to manage your .NET User Secrets from the command line.");
        _console.WriteLine();
        _console.MarkupLine("Usage: [green]dotnet better-secrets[/] [yellow]<working-directory>[/] [grey][[options]][/]");
        _console.WriteLine();
        _console.MarkupLine("Arguments:");
        _console.MarkupLine("  [yellow]<working-directory>[/]  The working directory containing the .NET project to manage secrets for. If not specified, the current directory will be used.");
        _console.WriteLine();
        _console.MarkupLine("Options:");
        _console.MarkupLine("  [green]-i[/], [green]--id[/]             The User Secrets ID to use. If not specified, the ID will be resolved from the project in the working directory.");
        _console.MarkupLine("  [green]-c[/], [green]--configuration[/]  The build configuration to use when resolving the project. Defaults to 'Debug'.");
        _console.MarkupLine("  [green]-h[/], [green]--help[/]           Show this help message.");
        _console.WriteLine();
        _console.MarkupLine("Examples:");
        _console.MarkupLine("  [grey]dotnet better-secrets ./MyProject[/]           Manage secrets for the project in ./MyProject.");
        _console.MarkupLine("  [grey]dotnet better-secrets -i <user-secrets-id>[/]  Manage secrets for the specified User Secrets ID.");
        _console.WriteLine();
        _console.MarkupLine("For more information, visit [blue underline]https://github.com/karl-sjogren/dotnet-better-secrets[/].");
    }

    private string SelectKey(ISecretsStore secretStore, string title) {
        var selectionSize = Math.Max(5, Math.Min(15, _console.Profile.Height - 3));
        var selection =
            new SelectionPrompt<string>()
                .Title(title)
                .PageSize(selectionSize)
                .MoreChoicesText("[grey](Move up and down to reveal more secrets)[/]")
                .AddChoices([.. secretStore.AsSortedEnumerable().Select(kvp => kvp.Key)]);

        return _console.Prompt(selection);
    }

    internal void RenderTable(ISecretsStore secretStore) {
        var version = typeof(Program).Assembly.GetName().Version?.ToString(3) ?? "unknown version";

        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey37)
            .Caption($"[grey]Karls Better Secrets Tool {version}[/]")
            .ShowRowSeparators()
            .Expand();

        var longestKey = secretStore.AsEnumerable().Select(kvp => kvp.Key.Length).DefaultIfEmpty(0).Max();
        longestKey = Math.Max(longestKey, 3);

        _console.ClearSafe();

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

        _console.Write(table);
    }

    private ResolveResult? ResolveUserSecretsId(string workingDirectory, string? buildConfiguration) {
        MsBuildProject[] projects;
        try {
            projects = _projectFinder.FindMsBuildProjects(workingDirectory);

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

                _console.MarkupLineInterpolated($"[yellow]Warning:[/] Multiple .NET projects found in directory '[grey]{workingDirectory}[/]'. Using: '[grey]{projectFile}[/]'.");
            }

            return _projectIdResolver.Resolve(projectFile, buildConfiguration ?? "Debug");
        } catch(Exception) {
            return null;
        }
    }
}
