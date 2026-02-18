using Karls.BetterSecretsTool.Prompts;
using Spectre.Console.Testing;

namespace Karls.BetterSecretsTool.Tests.Prompts;

public class EditableTextPromptTests : IDisposable {
    private readonly TestConsole _console;

    public EditableTextPromptTests() {
        _console = new TestConsole();
        _console.Interactive();
    }

    public void Dispose() {
        _console.Dispose();
    }

    [Fact]
    public void Show_WhenTextEnteredAndEnterPressed_ReturnsText() {
        // Arrange
        var prompt = new EditableTextPrompt("Enter value:");

        _console.Input.PushText("hello");
        _console.Input.PushKey(ConsoleKey.Enter);

        // Act
        var result = prompt.Show(_console);

        // Assert
        result.ShouldBe("hello");
    }

    [Fact]
    public void Show_WhenEnterPressedImmediately_ReturnsEmptyString() {
        // Arrange
        var prompt = new EditableTextPrompt("Enter value:");

        _console.Input.PushKey(ConsoleKey.Enter);

        // Act
        var result = prompt.Show(_console);

        // Assert
        result.ShouldBe(string.Empty);
    }

    [Fact]
    public void Show_WithDefaultValue_ReturnsDefaultWhenEnterPressed() {
        // Arrange
        var prompt = new EditableTextPrompt("Enter value:", "default");

        _console.Input.PushKey(ConsoleKey.Enter);

        // Act
        var result = prompt.Show(_console);

        // Assert
        result.ShouldBe("default");
    }

    [Fact]
    public void Show_WithDefaultValueAndNewText_ReturnsNewText() {
        // Arrange
        var prompt = new EditableTextPrompt("Enter value:", "old");

        // Clear the default value using backspace and type new text
        _console.Input.PushKey(ConsoleKey.Backspace);
        _console.Input.PushKey(ConsoleKey.Backspace);
        _console.Input.PushKey(ConsoleKey.Backspace);
        _console.Input.PushText("new");
        _console.Input.PushKey(ConsoleKey.Enter);

        // Act
        var result = prompt.Show(_console);

        // Assert
        result.ShouldBe("new");
    }

    [Fact]
    public void Show_LeftArrowAndInsert_InsertsAtCorrectPosition() {
        // Arrange
        var prompt = new EditableTextPrompt("Enter value:");

        // Type "ac", move left, insert "b" -> "abc"
        _console.Input.PushText("ac");
        _console.Input.PushKey(ConsoleKey.LeftArrow);
        _console.Input.PushText("b");
        _console.Input.PushKey(ConsoleKey.Enter);

        // Act
        var result = prompt.Show(_console);

        // Assert
        result.ShouldBe("abc");
    }

    [Fact]
    public void Show_RightArrowAfterLeftArrow_MovesCorrectly() {
        // Arrange
        var prompt = new EditableTextPrompt("Enter value:");

        // Type "abc", move left twice, right once, insert "X" -> "abXc"
        _console.Input.PushText("abc");
        _console.Input.PushKey(ConsoleKey.LeftArrow);
        _console.Input.PushKey(ConsoleKey.LeftArrow);
        _console.Input.PushKey(ConsoleKey.RightArrow);
        _console.Input.PushText("X");
        _console.Input.PushKey(ConsoleKey.Enter);

        // Act
        var result = prompt.Show(_console);

        // Assert
        result.ShouldBe("abXc");
    }

    [Fact]
    public void Show_HomeKey_MovesCursorToStart() {
        // Arrange
        var prompt = new EditableTextPrompt("Enter value:");

        // Type "abc", press Home, insert "X" -> "Xabc"
        _console.Input.PushText("abc");
        _console.Input.PushKey(ConsoleKey.Home);
        _console.Input.PushText("X");
        _console.Input.PushKey(ConsoleKey.Enter);

        // Act
        var result = prompt.Show(_console);

        // Assert
        result.ShouldBe("Xabc");
    }

    [Fact]
    public void Show_EndKey_MovesCursorToEnd() {
        // Arrange
        var prompt = new EditableTextPrompt("Enter value:");

        // Type "abc", press Home, press End, insert "X" -> "abcX"
        _console.Input.PushText("abc");
        _console.Input.PushKey(ConsoleKey.Home);
        _console.Input.PushKey(ConsoleKey.End);
        _console.Input.PushText("X");
        _console.Input.PushKey(ConsoleKey.Enter);

        // Act
        var result = prompt.Show(_console);

        // Assert
        result.ShouldBe("abcX");
    }

    [Fact]
    public void Show_BackspaceAtStart_DoesNothing() {
        // Arrange
        var prompt = new EditableTextPrompt("Enter value:");

        // Type "abc", go to start, try backspace (should do nothing), insert "X" -> "Xabc"
        _console.Input.PushText("abc");
        _console.Input.PushKey(ConsoleKey.Home);
        _console.Input.PushKey(ConsoleKey.Backspace);
        _console.Input.PushText("X");
        _console.Input.PushKey(ConsoleKey.Enter);

        // Act
        var result = prompt.Show(_console);

        // Assert
        result.ShouldBe("Xabc");
    }

    [Fact]
    public void Show_BackspaceInMiddle_RemovesCorrectCharacter() {
        // Arrange
        var prompt = new EditableTextPrompt("Enter value:");

        // Type "abc", move left, backspace -> "ac"
        _console.Input.PushText("abc");
        _console.Input.PushKey(ConsoleKey.LeftArrow);
        _console.Input.PushKey(ConsoleKey.Backspace);
        _console.Input.PushKey(ConsoleKey.Enter);

        // Act
        var result = prompt.Show(_console);

        // Assert
        result.ShouldBe("ac");
    }

    [Fact]
    public void Show_DeleteInMiddle_RemovesCorrectCharacter() {
        // Arrange
        var prompt = new EditableTextPrompt("Enter value:");

        // Type "abc", move left twice, delete -> "ac"
        _console.Input.PushText("abc");
        _console.Input.PushKey(ConsoleKey.LeftArrow);
        _console.Input.PushKey(ConsoleKey.LeftArrow);
        _console.Input.PushKey(ConsoleKey.Delete);
        _console.Input.PushKey(ConsoleKey.Enter);

        // Act
        var result = prompt.Show(_console);

        // Assert
        result.ShouldBe("ac");
    }

    [Fact]
    public void Show_DeleteAtEnd_DoesNothing() {
        // Arrange
        var prompt = new EditableTextPrompt("Enter value:");

        // Type "abc", delete at end (should do nothing)
        _console.Input.PushText("abc");
        _console.Input.PushKey(ConsoleKey.Delete);
        _console.Input.PushKey(ConsoleKey.Enter);

        // Act
        var result = prompt.Show(_console);

        // Assert
        result.ShouldBe("abc");
    }

    [Fact]
    public void Show_LeftArrowAtStart_DoesNothing() {
        // Arrange
        var prompt = new EditableTextPrompt("Enter value:");

        // Type "abc", go to start, left arrow (should do nothing), insert "X" -> "Xabc"
        _console.Input.PushText("abc");
        _console.Input.PushKey(ConsoleKey.Home);
        _console.Input.PushKey(ConsoleKey.LeftArrow);
        _console.Input.PushText("X");
        _console.Input.PushKey(ConsoleKey.Enter);

        // Act
        var result = prompt.Show(_console);

        // Assert
        result.ShouldBe("Xabc");
    }

    [Fact]
    public void Show_RightArrowAtEnd_DoesNothing() {
        // Arrange
        var prompt = new EditableTextPrompt("Enter value:");

        // Type "abc", right arrow at end (should do nothing), insert "X" -> "abcX"
        _console.Input.PushText("abc");
        _console.Input.PushKey(ConsoleKey.RightArrow);
        _console.Input.PushText("X");
        _console.Input.PushKey(ConsoleKey.Enter);

        // Act
        var result = prompt.Show(_console);

        // Assert
        result.ShouldBe("abcX");
    }

    [Fact]
    public void Show_WhenEscapePressed_ReturnsNull() {
        // Arrange
        var prompt = new EditableTextPrompt("Enter value:");

        _console.Input.PushText("some text");
        _console.Input.PushKey(ConsoleKey.Escape);

        // Act
        var result = prompt.Show(_console);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void Show_WithDefaultValueWhenEscapePressed_ReturnsNull() {
        // Arrange
        var prompt = new EditableTextPrompt("Enter value:", "default");

        _console.Input.PushKey(ConsoleKey.Escape);

        // Act
        var result = prompt.Show(_console);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void Show_CtrlU_ClearsEntireLine() {
        // Arrange
        var prompt = new EditableTextPrompt("Enter value:");

        // Type "hello world", press Ctrl+U to clear, type "new text"
        _console.Input.PushText("hello world");
        _console.Input.PushKey(new ConsoleKeyInfo('\u0015', ConsoleKey.U, shift: false, alt: false, control: true));
        _console.Input.PushText("new text");
        _console.Input.PushKey(ConsoleKey.Enter);

        // Act
        var result = prompt.Show(_console);

        // Assert
        result.ShouldBe("new text");
    }

    [Fact]
    public void Show_CtrlU_WithDefaultValue_ClearsEntireLine() {
        // Arrange
        var prompt = new EditableTextPrompt("Enter value:", "default value");

        // Press Ctrl+U to clear the default, type "replaced"
        _console.Input.PushKey(new ConsoleKeyInfo('\u0015', ConsoleKey.U, shift: false, alt: false, control: true));
        _console.Input.PushText("replaced");
        _console.Input.PushKey(ConsoleKey.Enter);

        // Act
        var result = prompt.Show(_console);

        // Assert
        result.ShouldBe("replaced");
    }

    [Fact]
    public void Show_ComplexEditing_ProducesCorrectResult() {
        // Arrange
        var prompt = new EditableTextPrompt("Enter value:");

        // Type "hello world", delete "world", type "there" -> "hello there"
        _console.Input.PushText("hello world");
        // Delete "world" (5 chars + space = 6 backspaces)
        for(var i = 0; i < 6; i++) {
            _console.Input.PushKey(ConsoleKey.Backspace);
        }

        _console.Input.PushText(" there");
        _console.Input.PushKey(ConsoleKey.Enter);

        // Act
        var result = prompt.Show(_console);

        // Assert
        result.ShouldBe("hello there");
    }

    [Fact]
    public void Show_WithLongText_RendersCorrectly() {
        // Arrange
        // Set a narrow console width to force horizontal scrolling
        _console.Profile.Width = 40;
        var prompt = new EditableTextPrompt("Enter value:");

        // Type text that exceeds console width (prompt "Enter value:" = 12 chars + space + text)
        // With width 40, this will trigger horizontal scrolling
        var longText = new string('a', 50);
        _console.Input.PushText(longText);
        _console.Input.PushKey(ConsoleKey.Enter);

        // Act
        var result = prompt.Show(_console);

        // Assert
        result.ShouldBe(longText);
    }

    [Fact]
    public void Show_WhenTextExpandsToRequireScrolling_HandlesCorrectly() {
        // Arrange
        _console.Profile.Width = 40;
        var prompt = new EditableTextPrompt("Enter value:");

        // Start with short text, then add more to trigger horizontal scrolling
        _console.Input.PushText("short");
        // Add more text to exceed the available width
        _console.Input.PushText(new string('x', 40));
        _console.Input.PushKey(ConsoleKey.Enter);

        // Act
        var result = prompt.Show(_console);

        // Assert
        result.ShouldBe("short" + new string('x', 40));
    }

    [Fact]
    public void Show_WhenTextShrinksFromScrolling_ReturnsCorrectValue() {
        // Arrange
        _console.Profile.Width = 40;
        var prompt = new EditableTextPrompt("Enter value:");

        // Type long text that requires scrolling
        var longText = new string('a', 50);
        _console.Input.PushText(longText);

        // Delete most of it using Ctrl+U to clear, then type short text
        _console.Input.PushKey(new ConsoleKeyInfo('\u0015', ConsoleKey.U, shift: false, alt: false, control: true));
        _console.Input.PushText("short");
        _console.Input.PushKey(ConsoleKey.Enter);

        // Act
        var result = prompt.Show(_console);

        // Assert
        result.ShouldBe("short");
    }

    [Fact]
    public void Show_WithTextExactlyAtConsoleWidth_HandlesEdgeCase() {
        // Arrange
        _console.Profile.Width = 40;
        var prompt = new EditableTextPrompt("Enter value:");

        // "Enter value: " is 13 chars, so we have about 26 chars left
        // This should be right at the edge before scrolling kicks in
        var exactText = new string('a', 26);
        _console.Input.PushText(exactText);
        _console.Input.PushKey(ConsoleKey.Enter);

        // Act
        var result = prompt.Show(_console);

        // Assert
        result.ShouldBe(exactText);
    }

    [Fact]
    public void Show_WithLongText_CursorNavigationWorksCorrectly() {
        // Arrange
        _console.Profile.Width = 40;
        var prompt = new EditableTextPrompt("Enter value:");

        // Type long text, navigate to the middle, insert a character
        var longText = new string('a', 50);
        _console.Input.PushText(longText);

        // Move cursor to beginning
        _console.Input.PushKey(ConsoleKey.Home);

        // Move 25 positions to the right (middle of the text)
        for(var i = 0; i < 25; i++) {
            _console.Input.PushKey(ConsoleKey.RightArrow);
        }

        // Insert 'X' in the middle
        _console.Input.PushText("X");
        _console.Input.PushKey(ConsoleKey.Enter);

        // Act
        var result = prompt.Show(_console);

        // Assert - X should be at position 25
        result.ShouldBe(new string('a', 25) + "X" + new string('a', 25));
    }

    [Fact]
    public void Show_WithLongText_BackspaceInMiddleWorksCorrectly() {
        // Arrange
        _console.Profile.Width = 40;
        var prompt = new EditableTextPrompt("Enter value:");

        // Type long text with a marker in the middle
        var longText = new string('a', 25) + "XY" + new string('a', 25);
        _console.Input.PushText(longText);

        // Move cursor to beginning
        _console.Input.PushKey(ConsoleKey.Home);

        // Move to position 27 (after 'Y')
        for(var i = 0; i < 27; i++) {
            _console.Input.PushKey(ConsoleKey.RightArrow);
        }

        // Backspace twice to delete 'Y' and 'X'
        _console.Input.PushKey(ConsoleKey.Backspace);
        _console.Input.PushKey(ConsoleKey.Backspace);
        _console.Input.PushKey(ConsoleKey.Enter);

        // Act
        var result = prompt.Show(_console);

        // Assert - XY should be removed
        result.ShouldBe(new string('a', 50));
    }
}
