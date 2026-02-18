using System.Text;
using Spectre.Console;

namespace Karls.BetterSecretsTool.Prompts;

/// <summary>
/// A text prompt that supports full cursor navigation with arrow keys,
/// Home/End, and editing at any position within the text.
/// Returns null if the user presses Escape to cancel.
/// Uses horizontal scrolling when text exceeds available width.
/// </summary>
public class EditableTextPrompt : IPrompt<string?> {
    private readonly string _promptMarkup;
    private readonly string? _defaultValue;

    // Scroll indicators
    private const char _leftIndicator = '◀';
    private const char _rightIndicator = '▶';

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
            Render(console, buffer.ToString(), cursorPosition);

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

                Render(console, buffer.ToString(), cursorPosition);
            }

            // If we exit the loop due to cancellation, return null
            return null;
        } finally {
            // Always restore the terminal cursor
            console.Cursor.Show(true);
        }
    }

    private void Render(IAnsiConsole console, string text, int cursorPosition) {
        var consoleWidth = console.Profile.Width > 0 ? console.Profile.Width : 120;

        // Calculate the rendered prompt length (without markup)
        var promptLength = Markup.Remove(_promptMarkup).Length;

        // Available width for text display: console width - prompt - space - 1 char safety margin
        // We need at least 1 char for the cursor indicator
        var availableWidth = consoleWidth - promptLength - 1 - 1;

        // Reserve space for scroll indicators if needed
        // We'll calculate this dynamically based on whether we need to scroll
        var textDisplayLength = text.Length + 1; // +1 for cursor indicator (space at end when cursor is at end)

        // Clear the current line and return to start
        console.Write("\r" + new string(' ', consoleWidth - 1) + "\r");

        // Render the prompt
        console.Markup(_promptMarkup);
        console.Write(" ");

        if(textDisplayLength <= availableWidth) {
            // Text fits - render normally without scrolling
            RenderTextWithCursor(console, text, cursorPosition, 0, text.Length);
        } else {
            // Text doesn't fit - need horizontal scrolling
            // Reserve 1 char on each side for potential scroll indicators
            var windowWidth = availableWidth - 2; // -2 for potential ◀ and ▶

            if(windowWidth < 1) {
                // Console is too narrow, just show what we can
                windowWidth = availableWidth;
            }

            // Calculate window position to keep cursor roughly centered
            var (windowStart, windowEnd, showLeftIndicator, showRightIndicator) =
                CalculateWindow(text.Length, cursorPosition, windowWidth);

            // Render left scroll indicator if needed
            if(showLeftIndicator) {
                console.Write(_leftIndicator.ToString());
            } else if(availableWidth > windowWidth) {
                // Add space to maintain alignment when no indicator
                console.Write(" ");
            }

            // Render the visible portion of text with cursor
            RenderTextWithCursor(console, text, cursorPosition, windowStart, windowEnd);

            // Render right scroll indicator if needed
            if(showRightIndicator) {
                console.Write(_rightIndicator.ToString());
            }
        }
    }

    private static (int windowStart, int windowEnd, bool showLeftIndicator, bool showRightIndicator) CalculateWindow(
        int textLength,
        int cursorPosition,
        int windowWidth) {
        // The visible content needs to include the cursor indicator
        // If cursor is at the end, we need +1 for the space that shows the cursor
        var contentLength = textLength + 1;

        // Try to center the cursor in the window
        var idealStart = cursorPosition - (windowWidth / 2);

        // Clamp to valid range
        var windowStart = Math.Max(0, idealStart);
        var windowEnd = windowStart + windowWidth;

        // If window extends past the end, shift it back
        if(windowEnd > contentLength) {
            windowEnd = contentLength;
            windowStart = Math.Max(0, windowEnd - windowWidth);
        }

        // Determine if we need scroll indicators
        var showLeftIndicator = windowStart > 0;
        var showRightIndicator = windowEnd < contentLength;

        return (windowStart, Math.Min(windowEnd, textLength), showLeftIndicator, showRightIndicator);
    }

    private static void RenderTextWithCursor(
        IAnsiConsole console,
        string text,
        int cursorPosition,
        int windowStart,
        int windowEnd) {
        // Determine what parts of the text to show
        var visibleText = text[windowStart..windowEnd];
        var cursorPosInWindow = cursorPosition - windowStart;

        // Is the cursor within the visible window?
        if(cursorPosInWindow >= 0 && cursorPosInWindow <= visibleText.Length) {
            // Text before cursor (within window)
            if(cursorPosInWindow > 0) {
                console.Write(Markup.Escape(visibleText[..cursorPosInWindow]));
            }

            // Cursor character (inverted)
            var cursorChar = cursorPosInWindow < visibleText.Length
                ? visibleText[cursorPosInWindow].ToString()
                : " ";
            console.Markup($"[invert]{Markup.Escape(cursorChar)}[/]");

            // Text after cursor (within window)
            if(cursorPosInWindow < visibleText.Length - 1) {
                console.Write(Markup.Escape(visibleText[(cursorPosInWindow + 1)..]));
            }
        } else {
            // Cursor is outside visible window (shouldn't happen with proper window calculation)
            console.Write(Markup.Escape(visibleText));
        }
    }
}
