using Bicep.LocalDeploy.DocGenerator.Services;
using NUnit.Framework;

namespace Bicep.LocalDeploy.DocGenerator.Tests;

[TestFixture]
public class IgnoreFileTests
{
    [Test]
    public async Task IgnoreFileSingleFileNameShouldMatchFile()
    {
        // Arrange
        DirectoryInfo testDir = Directory.CreateTempSubdirectory("ignore-test-");
        try
        {
            string ignoreFile = Path.Combine(testDir.FullName, ".biceplocalgenignore");
            await File.WriteAllTextAsync(ignoreFile, "Widget.cs\n");

            // Act
            IgnoreFile ignore = await IgnoreFile.CreateAsync(testDir.FullName);

            // Assert - these should all be ignored
            Assert.That(
                ignore.IsIgnored("Widget.cs"),
                Is.True,
                "Direct filename should be ignored"
            );
            Assert.That(
                ignore.IsIgnored("Models/Widget.cs"),
                Is.True,
                "File in subdirectory should be ignored"
            );
            Assert.That(
                ignore.IsIgnored("C:/source/Models/Widget.cs"),
                Is.True,
                "Full path should be ignored"
            );
            Assert.That(
                ignore.IsIgnored("/path/to/Widget.cs"),
                Is.True,
                "Unix-style path should be ignored"
            );

            // These should NOT be ignored
            Assert.That(
                ignore.IsIgnored("WidgetBase.cs"),
                Is.False,
                "Different filename should not be ignored"
            );
            Assert.That(
                ignore.IsIgnored("Widget.txt"),
                Is.False,
                "Different extension should not be ignored"
            );
        }
        finally
        {
            testDir.Delete(true);
        }
    }

    [Test]
    public async Task IgnoreFileRelativePathShouldMatchCorrectly()
    {
        // Arrange
        DirectoryInfo testDir = Directory.CreateTempSubdirectory("ignore-test-");
        try
        {
            string ignoreFile = Path.Combine(testDir.FullName, ".biceplocalgenignore");
            await File.WriteAllTextAsync(ignoreFile, "Models/Widget.cs\nGenerated/*.cs\n");

            // Act
            IgnoreFile ignore = await IgnoreFile.CreateAsync(testDir.FullName);

            // Assert
            Assert.That(
                ignore.IsIgnored("Models/Widget.cs"),
                Is.True,
                "Relative path should be ignored"
            );
            Assert.That(
                ignore.IsIgnored("Generated/Test.cs"),
                Is.True,
                "Wildcard in subdirectory should work"
            );
            Assert.That(
                ignore.IsIgnored("Other/Widget.cs"),
                Is.False,
                "Wrong subdirectory should not be ignored"
            );
        }
        finally
        {
            testDir.Delete(true);
        }
    }

    [Test]
    public async Task IgnoreFileDirectoryPatternShouldMatchAllFilesInDirectory()
    {
        // Arrange
        DirectoryInfo testDir = Directory.CreateTempSubdirectory("ignore-test-");
        try
        {
            string ignoreFile = Path.Combine(testDir.FullName, ".biceplocalgenignore");
            await File.WriteAllTextAsync(ignoreFile, "Models/**\n");

            // Act
            IgnoreFile ignore = await IgnoreFile.CreateAsync(testDir.FullName);

            // Assert - All files under Models/ should be ignored
            Assert.That(
                ignore.IsIgnored("Models/Configuration.cs"),
                Is.True,
                "File directly in Models should be ignored"
            );
            Assert.That(
                ignore.IsIgnored("Models/SubFolder/Widget.cs"),
                Is.True,
                "File in Models subdirectory should be ignored"
            );
            Assert.That(
                ignore.IsIgnored("C:/source/bicep-local-docgen/Models/Configuration.cs"),
                Is.True,
                "Absolute path to Models file should be ignored"
            );
            Assert.That(
                ignore.IsIgnored("/full/path/Models/AnyFile.cs"),
                Is.True,
                "Any absolute path with Models should be ignored"
            );

            // These should NOT be ignored
            Assert.That(
                ignore.IsIgnored("src/Models.cs"),
                Is.False,
                "File named Models.cs should not be ignored"
            );
            Assert.That(
                ignore.IsIgnored("Other/Configuration.cs"),
                Is.False,
                "File in different directory should not be ignored"
            );
            Assert.That(
                ignore.IsIgnored("ModelsHelper.cs"),
                Is.False,
                "File with Models prefix should not be ignored"
            );
        }
        finally
        {
            testDir.Delete(true);
        }
    }

    [Test]
    public async Task IgnoreFileMultipleDirectoryPatternsShouldWork()
    {
        // Arrange
        DirectoryInfo testDir = Directory.CreateTempSubdirectory("ignore-test-");
        try
        {
            string ignoreFile = Path.Combine(testDir.FullName, ".biceplocalgenignore");
            await File.WriteAllTextAsync(ignoreFile, "Models/**\nTests/**\n*.tmp\n");

            // Act
            IgnoreFile ignore = await IgnoreFile.CreateAsync(testDir.FullName);

            // Assert - Multiple patterns should all work
            Assert.That(
                ignore.IsIgnored("Models/Configuration.cs"),
                Is.True,
                "Models directory should be ignored"
            );
            Assert.That(
                ignore.IsIgnored("Tests/UnitTest.cs"),
                Is.True,
                "Tests directory should be ignored"
            );
            Assert.That(ignore.IsIgnored("temp.tmp"), Is.True, "Temp files should be ignored");
            Assert.That(
                ignore.IsIgnored("src/Controllers/HomeController.cs"),
                Is.False,
                "Other files should not be ignored"
            );
        }
        finally
        {
            testDir.Delete(true);
        }
    }
}
