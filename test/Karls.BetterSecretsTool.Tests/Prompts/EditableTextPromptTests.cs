using Karls.BetterSecretsTool.Prompts;
using Spectre.Console.Testing;

namespace Karls.BetterSecretsTool.Tests.Prompts;

public class EditableTextPromptTests {
    [Fact]
    public void Show_WhenTextEnteredAndEnterPressed_ReturnsText() {
        // Arrange
        var console = new TestConsole();
        console.Interactive();
        var prompt = new EditableTextPrompt("Enter value:");

        console.Input.PushText("hello");
        console.Input.PushKey(ConsoleKey.Enter);

        // Act
        var result = prompt.Show(console);

        // Assert
        result.ShouldBe("hello");
    }

    [Fact]
    public void Show_WhenEnterPressedImmediately_ReturnsEmptyString() {
        // Arrange
        var console = new TestConsole();
        console.Interactive();
        var prompt = new EditableTextPrompt("Enter value:");

        console.Input.PushKey(ConsoleKey.Enter);

        // Act
        var result = prompt.Show(console);

        // Assert
        result.ShouldBe(string.Empty);
    }

    [Fact]
    public void Show_WithDefaultValue_ReturnsDefaultWhenEnterPressed() {
        // Arrange
        var console = new TestConsole();
        console.Interactive();
        var prompt = new EditableTextPrompt("Enter value:", "default");

        console.Input.PushKey(ConsoleKey.Enter);

        // Act
        var result = prompt.Show(console);

        // Assert
        result.ShouldBe("default");
    }

    [Fact]
    public void Show_WithDefaultValueAndNewText_ReturnsNewText() {
        // Arrange
        var console = new TestConsole();
        console.Interactive();
        var prompt = new EditableTextPrompt("Enter value:", "old");

        // Clear the default value using backspace and type new text
        console.Input.PushKey(ConsoleKey.Backspace);
        console.Input.PushKey(ConsoleKey.Backspace);
        console.Input.PushKey(ConsoleKey.Backspace);
        console.Input.PushText("new");
        console.Input.PushKey(ConsoleKey.Enter);

        // Act
        var result = prompt.Show(console);

        // Assert
        result.ShouldBe("new");
    }

    [Fact]
    public void Show_LeftArrowAndInsert_InsertsAtCorrectPosition() {
        // Arrange
        var console = new TestConsole();
        console.Interactive();
        var prompt = new EditableTextPrompt("Enter value:");

        // Type "ac", move left, insert "b" -> "abc"
        console.Input.PushText("ac");
        console.Input.PushKey(ConsoleKey.LeftArrow);
        console.Input.PushText("b");
        console.Input.PushKey(ConsoleKey.Enter);

        // Act
        var result = prompt.Show(console);

        // Assert
        result.ShouldBe("abc");
    }

    [Fact]
    public void Show_RightArrowAfterLeftArrow_MovesCorrectly() {
        // Arrange
        var console = new TestConsole();
        console.Interactive();
        var prompt = new EditableTextPrompt("Enter value:");

        // Type "abc", move left twice, right once, insert "X" -> "abXc"
        console.Input.PushText("abc");
        console.Input.PushKey(ConsoleKey.LeftArrow);
        console.Input.PushKey(ConsoleKey.LeftArrow);
        console.Input.PushKey(ConsoleKey.RightArrow);
        console.Input.PushText("X");
        console.Input.PushKey(ConsoleKey.Enter);

        // Act
        var result = prompt.Show(console);

        // Assert
        result.ShouldBe("abXc");
    }

    [Fact]
    public void Show_HomeKey_MovesCursorToStart() {
        // Arrange
        var console = new TestConsole();
        console.Interactive();
        var prompt = new EditableTextPrompt("Enter value:");

        // Type "abc", press Home, insert "X" -> "Xabc"
        console.Input.PushText("abc");
        console.Input.PushKey(ConsoleKey.Home);
        console.Input.PushText("X");
        console.Input.PushKey(ConsoleKey.Enter);

        // Act
        var result = prompt.Show(console);

        // Assert
        result.ShouldBe("Xabc");
    }

    [Fact]
    public void Show_EndKey_MovesCursorToEnd() {
        // Arrange
        var console = new TestConsole();
        console.Interactive();
        var prompt = new EditableTextPrompt("Enter value:");

        // Type "abc", press Home, press End, insert "X" -> "abcX"
        console.Input.PushText("abc");
        console.Input.PushKey(ConsoleKey.Home);
        console.Input.PushKey(ConsoleKey.End);
        console.Input.PushText("X");
        console.Input.PushKey(ConsoleKey.Enter);

        // Act
        var result = prompt.Show(console);

        // Assert
        result.ShouldBe("abcX");
    }

    [Fact]
    public void Show_BackspaceAtStart_DoesNothing() {
        // Arrange
        var console = new TestConsole();
        console.Interactive();
        var prompt = new EditableTextPrompt("Enter value:");

        // Type "abc", go to start, try backspace (should do nothing), insert "X" -> "Xabc"
        console.Input.PushText("abc");
        console.Input.PushKey(ConsoleKey.Home);
        console.Input.PushKey(ConsoleKey.Backspace);
        console.Input.PushText("X");
        console.Input.PushKey(ConsoleKey.Enter);

        // Act
        var result = prompt.Show(console);

        // Assert
        result.ShouldBe("Xabc");
    }

    [Fact]
    public void Show_BackspaceInMiddle_RemovesCorrectCharacter() {
        // Arrange
        var console = new TestConsole();
        console.Interactive();
        var prompt = new EditableTextPrompt("Enter value:");

        // Type "abc", move left, backspace -> "ac"
        console.Input.PushText("abc");
        console.Input.PushKey(ConsoleKey.LeftArrow);
        console.Input.PushKey(ConsoleKey.Backspace);
        console.Input.PushKey(ConsoleKey.Enter);

        // Act
        var result = prompt.Show(console);

        // Assert
        result.ShouldBe("ac");
    }

    [Fact]
    public void Show_DeleteInMiddle_RemovesCorrectCharacter() {
        // Arrange
        var console = new TestConsole();
        console.Interactive();
        var prompt = new EditableTextPrompt("Enter value:");

        // Type "abc", move left twice, delete -> "ac"
        console.Input.PushText("abc");
        console.Input.PushKey(ConsoleKey.LeftArrow);
        console.Input.PushKey(ConsoleKey.LeftArrow);
        console.Input.PushKey(ConsoleKey.Delete);
        console.Input.PushKey(ConsoleKey.Enter);

        // Act
        var result = prompt.Show(console);

        // Assert
        result.ShouldBe("ac");
    }

    [Fact]
    public void Show_DeleteAtEnd_DoesNothing() {
        // Arrange
        var console = new TestConsole();
        console.Interactive();
        var prompt = new EditableTextPrompt("Enter value:");

        // Type "abc", delete at end (should do nothing)
        console.Input.PushText("abc");
        console.Input.PushKey(ConsoleKey.Delete);
        console.Input.PushKey(ConsoleKey.Enter);

        // Act
        var result = prompt.Show(console);

        // Assert
        result.ShouldBe("abc");
    }

    [Fact]
    public void Show_LeftArrowAtStart_DoesNothing() {
        // Arrange
        var console = new TestConsole();
        console.Interactive();
        var prompt = new EditableTextPrompt("Enter value:");

        // Type "abc", go to start, left arrow (should do nothing), insert "X" -> "Xabc"
        console.Input.PushText("abc");
        console.Input.PushKey(ConsoleKey.Home);
        console.Input.PushKey(ConsoleKey.LeftArrow);
        console.Input.PushText("X");
        console.Input.PushKey(ConsoleKey.Enter);

        // Act
        var result = prompt.Show(console);

        // Assert
        result.ShouldBe("Xabc");
    }

    [Fact]
    public void Show_RightArrowAtEnd_DoesNothing() {
        // Arrange
        var console = new TestConsole();
        console.Interactive();
        var prompt = new EditableTextPrompt("Enter value:");

        // Type "abc", right arrow at end (should do nothing), insert "X" -> "abcX"
        console.Input.PushText("abc");
        console.Input.PushKey(ConsoleKey.RightArrow);
        console.Input.PushText("X");
        console.Input.PushKey(ConsoleKey.Enter);

        // Act
        var result = prompt.Show(console);

        // Assert
        result.ShouldBe("abcX");
    }

    [Fact]
    public void Show_WhenEscapePressed_ReturnsNull() {
        // Arrange
        var console = new TestConsole();
        console.Interactive();
        var prompt = new EditableTextPrompt("Enter value:");

        console.Input.PushText("some text");
        console.Input.PushKey(ConsoleKey.Escape);

        // Act
        var result = prompt.Show(console);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void Show_WithDefaultValueWhenEscapePressed_ReturnsNull() {
        // Arrange
        var console = new TestConsole();
        console.Interactive();
        var prompt = new EditableTextPrompt("Enter value:", "default");

        console.Input.PushKey(ConsoleKey.Escape);

        // Act
        var result = prompt.Show(console);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void Show_ComplexEditing_ProducesCorrectResult() {
        // Arrange
        var console = new TestConsole();
        console.Interactive();
        var prompt = new EditableTextPrompt("Enter value:");

        // Type "hello world", delete "world", type "there" -> "hello there"
        console.Input.PushText("hello world");
        // Delete "world" (5 chars + space = 6 backspaces)
        for(var i = 0; i < 6; i++) {
            console.Input.PushKey(ConsoleKey.Backspace);
        }

        console.Input.PushText(" there");
        console.Input.PushKey(ConsoleKey.Enter);

        // Act
        var result = prompt.Show(console);

        // Assert
        result.ShouldBe("hello there");
    }
}
