using System.Text;
using Spectre.Console;

namespace Karls.BetterSecretsTool.Prompts;

/// <summary>
/// A text prompt that supports full cursor navigation with arrow keys,
/// Home/End, and editing at any position within the text.
/// Returns null if the user presses Escape to cancel.
/// </summary>
public class EditableTextPrompt : IPrompt<string?> {
    private readonly string _promptMarkup;
    private readonly string? _defaultValue;

    public EditableTextPrompt(string promptMarkup, string? defaultValue = null) {
        _promptMarkup = promptMarkup;
        _defaultValue = defaultValue;
    }

    public string? Show(IAnsiConsole console) {
        var buffer = new StringBuilder(_defaultValue ?? string.Empty);
        var cursorPosition = buffer.Length;

        // Hide the terminal cursor to avoid double cursor display
        console.Cursor.Show(false);

        try {
            // Initial render
            Render(console, buffer, cursorPosition);

            while(true) {
                var keyInfo = console.Input.ReadKey(intercept: true);
                if(keyInfo is null) {
                    continue;
                }

                var key = keyInfo.Value;

                switch(key.Key) {
                    case ConsoleKey.Enter:
                        // Move to new line before returning
                        console.WriteLine();
                        return buffer.ToString();

                    case ConsoleKey.Escape:
                        // Cancel and return null
                        console.WriteLine();
                        return null;

                    case ConsoleKey.LeftArrow:
                        if(cursorPosition > 0) {
                            cursorPosition--;
                        }

                        break;

                    case ConsoleKey.RightArrow:
                        if(cursorPosition < buffer.Length) {
                            cursorPosition++;
                        }

                        break;

                    case ConsoleKey.Home:
                        cursorPosition = 0;
                        break;

                    case ConsoleKey.End:
                        cursorPosition = buffer.Length;
                        break;

                    case ConsoleKey.Delete:
                        if(cursorPosition < buffer.Length) {
                            buffer.Remove(cursorPosition, 1);
                        }

                        break;

                    case ConsoleKey.Backspace:
                        if(cursorPosition > 0) {
                            buffer.Remove(cursorPosition - 1, 1);
                            cursorPosition--;
                        }

                        break;

                    default:
                        // Handle Ctrl+U to clear the line
                        if(key.Key == ConsoleKey.U && key.Modifiers.HasFlag(ConsoleModifiers.Control)) {
                            buffer.Clear();
                            cursorPosition = 0;
                            break;
                        }

                        // Insert printable characters
                        if(!char.IsControl(key.KeyChar)) {
                            buffer.Insert(cursorPosition, key.KeyChar);
                            cursorPosition++;
                        }

                        break;
                }

                Render(console, buffer, cursorPosition);
            }
        } finally {
            // Always restore the terminal cursor
            console.Cursor.Show(true);
        }
    }

    public Task<string?> ShowAsync(IAnsiConsole console, CancellationToken cancellationToken) {
        return Task.FromResult(Show(console));
    }

    private void Render(IAnsiConsole console, StringBuilder buffer, int cursorPosition) {
        var text = buffer.ToString();
        var beforeCursor = text[..cursorPosition];
        var cursorChar = cursorPosition < text.Length ? text[cursorPosition].ToString() : " ";
        var afterCursor = cursorPosition < text.Length - 1 ? text[(cursorPosition + 1)..] : string.Empty;

        // Calculate the width needed to clear the line
        // Use a reasonable max width for clearing
        var clearWidth = console.Profile.Width > 0 ? console.Profile.Width : 120;

        // Move to start of line and clear it
        console.Write("\r" + new string(' ', clearWidth) + "\r");

        // Render the prompt and current input with cursor indicator
        console.Markup(_promptMarkup);
        console.Write(" ");
        console.Write(Markup.Escape(beforeCursor));
        console.Markup($"[invert]{Markup.Escape(cursorChar)}[/]");
        console.Write(Markup.Escape(afterCursor));
    }
}
