using Spectre.Console;

namespace Karls.BetterSecretsTool.Extensions;

public static class AnsiConsoleExtensions {
    extension(IAnsiConsole console) {
        public void ClearSafe() {
            try {
                console.Clear();
            } catch {
                // Can fail in some environments, e.g. when piping output to a file or running from VSCode
            }
        }
    }
}
