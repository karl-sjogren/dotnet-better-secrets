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
}
