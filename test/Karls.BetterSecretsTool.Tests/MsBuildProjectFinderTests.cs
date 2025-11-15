using System.IO.Abstractions.TestingHelpers;

using MUS = System.IO.Abstractions.TestingHelpers.MockUnixSupport;

namespace Karls.BetterSecretsTool.Tests;

public class MsBuildProjectFinderTests {
    [Fact]
    public void ParseProjectFile_WithSdkAttribute_ReturnsMsBuildProject() {
        // Arrange
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData> {
            { @"C:\Projects\MyApp\MyApp.csproj", new MockFileData("<Project Sdk=\"Microsoft.NET.Sdk\"></Project>") }
        });
        var finder = new MsBuildProjectFinder(fileSystem);

        // Act
        var project = finder.ParseProjectFile(@"C:\Projects\MyApp\MyApp.csproj", @"C:\Projects\MyApp");

        // Assert
        project.ShouldNotBeNull();
        project.Path.ShouldBe(@"C:\Projects\MyApp\MyApp.csproj");
        project.Sdk.ShouldBe("Microsoft.NET.Sdk");
    }

    [Fact]
    public void ParseProjectFile_WithoutSdkAttribute_ReturnsNull() {
        // Arrange
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData> {
            { @"C:\Projects\MyApp\MyApp.csproj", new MockFileData("<Project></Project>") }
        });
        var finder = new MsBuildProjectFinder(fileSystem);

        // Act
        var project = finder.ParseProjectFile(@"C:\Projects\MyApp\MyApp.csproj", @"C:\Projects\MyApp");

        // Assert
        project.ShouldBeNull();
    }

    [Fact]
    public void ParseProjectFile_WithInvalidXml_ReturnsNull() {
        // Arrange
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData> {
            { @"C:\Projects\MyApp\MyApp.csproj", new MockFileData("<Project><Invalid></Project>") }
        });
        var finder = new MsBuildProjectFinder(fileSystem);

        // Act
        var project = finder.ParseProjectFile(@"C:\Projects\MyApp\MyApp.csproj", @"C:\Projects\MyApp");

        // Assert
        project.ShouldBeNull();
    }

    [Fact]
    public void FindMsBuildProjects_WhenCalled_ReturnsAllProjectsDownTheDirectoryTree() {
        // Arrange
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData> {
            { MUS.Path(@"C:\Projects\MyApp\MyApp.csproj"), new MockFileData("<Project Sdk=\"Microsoft.NET.Sdk\"></Project>") },
            { MUS.Path(@"C:\Projects\MyApp\MyLib\MyLib.csproj"), new MockFileData("<Project Sdk=\"Microsoft.NET.Sdk\"></Project>") },
            { MUS.Path(@"C:\Projects\MyApp\MyWeb\MyWeb.fsproj"), new MockFileData("<Project Sdk=\"Microsoft.NET.Sdk.Web\"></Project>") },
            { MUS.Path(@"C:\Projects\MyApp\Readme.txt"), new MockFileData("This is a readme file.") }
        });
        var finder = new MsBuildProjectFinder(fileSystem);

        // Act
        var projects = finder.FindMsBuildProjects(MUS.Path(@"C:\Projects\MyApp\"));

        // Assert
        projects.Length.ShouldBe(3);

        var myAppProject = projects.FirstOrDefault(p => p.Path == MUS.Path(@"C:\Projects\MyApp\MyApp.csproj"));
        myAppProject.ShouldNotBeNull();
        myAppProject.Sdk.ShouldBe("Microsoft.NET.Sdk");
        myAppProject.AtRoot.ShouldBeTrue();
        myAppProject.IsWebSdk.ShouldBeFalse();

        var myLibProject = projects.FirstOrDefault(p => p.Path == MUS.Path(@"C:\Projects\MyApp\MyLib\MyLib.csproj"));
        myLibProject.ShouldNotBeNull();
        myLibProject.Sdk.ShouldBe("Microsoft.NET.Sdk");
        myLibProject.AtRoot.ShouldBeFalse();
        myLibProject.IsWebSdk.ShouldBeFalse();

        var myWebProject = projects.FirstOrDefault(p => p.Path == MUS.Path(@"C:\Projects\MyApp\MyWeb\MyWeb.fsproj"));
        myWebProject.ShouldNotBeNull();
        myWebProject.Sdk.ShouldBe("Microsoft.NET.Sdk.Web");
        myWebProject.AtRoot.ShouldBeFalse();
        myWebProject.IsWebSdk.ShouldBeTrue();
    }
}
