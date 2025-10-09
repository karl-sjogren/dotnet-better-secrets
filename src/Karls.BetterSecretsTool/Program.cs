using System.IO.Abstractions;
using Karls.BetterSecretsTool.Vendor;
using Spectre.Console;

namespace Karls.BetterSecretsTool;

public static class Program {
    public static void Main(string[] args) {
        var fileSystem = new FileSystem();
        var directory = args.Length > 0 ? args[0] : fileSystem.Directory.GetCurrentDirectory();
        var id = ResolveId(directory, fileSystem);

        var secretStore = new SecretsStore(id!, fileSystem);

        while(true) {
            RenderTable(secretStore);

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[grey]Type [green]A[/] to add, [green]E[/] to edit or [green]D[/] to delete secrets. Type [green]S[/] to show single value.[/]");
            AnsiConsole.MarkupLine("[grey]Press [green]Enter[/] to exit[/]");

            var prompt = AnsiConsole.Console.Input.ReadKey(false).GetValueOrDefault().KeyChar.ToString();

            prompt = prompt.Trim().ToUpperInvariant();
            if(string.IsNullOrWhiteSpace(prompt) || prompt == "Q") {
                return;
            }

            try {
                AnsiConsole.Clear();
            } catch {
                // Can fail in some environments, e.g. when piping output to a file or running from VSCode
            }

            if(prompt == "A") {
                var key = AnsiConsole.Ask<string>("[grey]Enter secret [green]key[/][/]:");
                var value = AnsiConsole.Ask<string>("[grey]Enter secret [yellow]value[/][/]:");
                secretStore.Set(key, value);
            } else if(prompt == "E") {
                var key = SelectKey(secretStore, "[grey]Select a secret to edit:[/]");

                AnsiConsole.MarkupLine($"[grey]Editing secret [green]{key}[/][/].");
                AnsiConsole.MarkupLineInterpolated($"[grey]Current value: [yellow]{secretStore[key]}[/][/]");
                var newValue = AnsiConsole.Ask<string>("[grey]Enter new value:[/]");
                secretStore.Set(key, newValue);
            } else if(prompt == "D") {
                var key = SelectKey(secretStore, "[grey]Select a secret to delete:[/]");
                secretStore.Remove(key);
            } else if(prompt == "S") {
                var key = SelectKey(secretStore, "[grey]Select a secret to show:[/]");
                AnsiConsole.MarkupLineInterpolated($"[grey]Value for [green]{key}[/]:");
                AnsiConsole.MarkupLineInterpolated($"[yellow]{secretStore[key]}[/]");
                AnsiConsole.MarkupLine("[grey]Press any key to continue...[/]");
                AnsiConsole.Console.Input.ReadKey(true);
            }
        }
    }

    private static string SelectKey(SecretsStore secretStore, string title) {
        var selection =
            new SelectionPrompt<string>()
                .Title(title)
                .PageSize(10)
                .MoreChoicesText("[grey](Move up and down to reveal more secrets)[/]")
                .AddChoices([.. secretStore.AsSortedEnumerable().Select(kvp => kvp.Key)]);

        return AnsiConsole.Prompt(selection);
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
            AnsiConsole.Clear();
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

        AnsiConsole.Write(table);
    }

    private static string? ResolveId(string workingDirectory, IFileSystem? fileSystem = null) {
        var resolver = new ProjectIdResolver(workingDirectory, fileSystem);
        return resolver.Resolve("", "Debug");
    }
}
