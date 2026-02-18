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
    private int _previousLineCount = 1;

    public EditableTextPrompt(string promptMarkup, string? defaultValue = null) {
        _promptMarkup = promptMarkup;
        _defaultValue = defaultValue;
    }

    public string? Show(IAnsiConsole console) {
        return ShowAsync(console, CancellationToken.None).GetAwaiter().GetResult();
    }

    public async Task<string?> ShowAsync(IAnsiConsole console, CancellationToken cancellationToken) {
        var buffer = new StringBuilder(_defaultValue ?? string.Empty);
        var cursorPosition = buffer.Length;

        // Hide the terminal cursor to avoid double cursor display
        console.Cursor.Show(false);

        try {
            // Initial render
            Render(console, buffer, cursorPosition);

            while(!cancellationToken.IsCancellationRequested) {
                var keyInfo = await console.Input.ReadKeyAsync(intercept: true, cancellationToken).ConfigureAwait(false);
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

            // If we exit the loop due to cancellation, return null
            return null;
        } finally {
            // Always restore the terminal cursor
            console.Cursor.Show(true);
        }
    }

    private void Render(IAnsiConsole console, StringBuilder buffer, int cursorPosition) {
        var text = buffer.ToString();
        var beforeCursor = text[..cursorPosition];
        var cursorChar = cursorPosition < text.Length ? text[cursorPosition].ToString() : " ";
        var afterCursor = cursorPosition < text.Length - 1 ? text[(cursorPosition + 1)..] : string.Empty;

        // Calculate the width needed to clear the line
        // Use a reasonable max width for clearing
        var consoleWidth = console.Profile.Width > 0 ? console.Profile.Width : 120;
        var consoleHeight = console.Profile.Height > 0 ? console.Profile.Height : 25;

        // Calculate how many lines the NEW render will occupy BEFORE we start clearing
        // This ensures we clear enough lines
        var promptText = Markup.Remove(_promptMarkup);
        var newDisplayText = beforeCursor + cursorChar + afterCursor;
        var newTotalLength = promptText.Length + 1 + newDisplayText.Length;
        var newLineCount = newTotalLength > 0
            ? (int)Math.Ceiling((double)newTotalLength / consoleWidth)
            : 1;

        // Clear the maximum of previous and new line counts to ensure we clear everything
        var linesToClear = Math.Max(_previousLineCount, newLineCount);
        linesToClear = Math.Min(linesToClear, consoleHeight - 1);

        // Move cursor up to the first line of the previous render (if multi-line)
        var linesToMoveUp = linesToClear - 1;
        if(linesToMoveUp > 0) {
            for(var i = 0; i < linesToMoveUp; i++) {
                console.Cursor.MoveUp();
            }
        }

        // Clear all lines from top to bottom
        for(var i = 0; i < linesToClear; i++) {
            // Move to start of line using ANSI escape sequence through console.Write
            // This allows Spectre.Console to parse and track the cursor position
            // \x1b[1G moves cursor to column 1 (1-indexed, so column 0 in 0-indexed)
            console.Write("\x1b[1G");

            // Clear to end of line using ANSI escape sequence
            // \x1b[K clears from cursor to end of line without risk of wrapping
            console.Write("\x1b[K");

            // Move to next line if not the last line
            if(i < linesToClear - 1) {
                console.Cursor.MoveDown();
            }
        }

        // Move cursor back up to the first line to start rendering
        if(linesToClear > 1) {
            for(var i = 0; i < linesToClear - 1; i++) {
                console.Cursor.MoveUp();
            }
        }

        // Move to start of line using ANSI escape sequence
        console.Write("\x1b[1G");

        // Render the prompt and current input with cursor indicator
        console.Markup(_promptMarkup);
        console.Write(" ");
        console.Write(Markup.Escape(beforeCursor));
        console.Markup($"[invert]{Markup.Escape(cursorChar)}[/]");
        console.Write(Markup.Escape(afterCursor));

        // Store the line count for next render
        _previousLineCount = newLineCount;
    }
}
