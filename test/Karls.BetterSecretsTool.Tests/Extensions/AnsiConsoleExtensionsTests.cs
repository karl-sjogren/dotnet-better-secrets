using Karls.BetterSecretsTool.Extensions;
using Spectre.Console;

namespace Karls.BetterSecretsTool.Tests.Extensions;

public class AnsiConsoleExtensionsTests {
    [Fact]
    public void ClearSafe_WhenConsoleClearsSuccessfully_DoesNotThrow() {
        // Arrange
        var console = A.Fake<IAnsiConsole>();

        // Act & Assert
        Should.NotThrow(console.ClearSafe);
    }

    [Fact]
    public void ClearSafe_WhenConsoleThrows_DoesNotThrow() {
        // Arrange
        var console = A.Fake<IAnsiConsole>();
        A.CallTo(() => console.Clear(true)).Throws<Exception>();

        // Act & Assert
        Should.NotThrow(console.ClearSafe);
    }
}
