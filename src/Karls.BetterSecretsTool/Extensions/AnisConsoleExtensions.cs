using Karls.BetterSecretsTool.Prompts;
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

        /// <summary>
        /// Prompts the user for input with full cursor navigation support.
        /// Allows using arrow keys, Home/End, and editing at any position.
        /// Press Escape to cancel.
        /// </summary>
        /// <param name="prompt">The prompt markup to display.</param>
        /// <param name="defaultValue">Optional default value to pre-populate the input.</param>
        /// <returns>The user's input string, or null if cancelled with Escape.</returns>
        public string? EditablePrompt(string prompt, string? defaultValue = null) {
            return console.Prompt(new EditableTextPrompt(prompt, defaultValue));
        }
    }
}
