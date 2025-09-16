using System.Text;
using Bicep.LocalDeploy.DocGenerator.Services;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace Bicep.LocalDeploy.DocGenerator.Tests;

[TestFixture]
public class CheckCommandTests
{
    private static string ReadFile(string path) => File.ReadAllText(path, Encoding.UTF8);

    [Test]
    public async Task CheckCommandWithValidModelReturnsSuccess()
    {
        // Arrange
        DirectoryInfo srcDir = Directory.CreateTempSubdirectory("check-src-");
        try
        {
            string csSource = """
                using Bicep.LocalDeploy;

                namespace TestModels;

                [ResourceType("TestResource")]
                [BicepDocHeading("Test Resource", "A test resource for validation")]
                [BicepDocExample("Basic Usage", "Creates a test resource", "resource test 'TestResource' = {}")]
                [BicepFrontMatter("title", "Test Resource")]
                public class TestResource
                {
                    [TypeProperty("The name of the resource", TypePropertyFlags.Required | TypePropertyFlags.Identifier)]
                    public string Name { get; init; } = string.Empty;
                }
                """;

            string srcFile = Path.Combine(srcDir.FullName, "TestResource.cs");
            await File.WriteAllTextAsync(srcFile, csSource, Encoding.UTF8);

            ConsoleLogger logger = new(LogLevel.Information);
            CheckValidator validator = new(logger);

            CheckOptions options = new()
            {
                SourceDirectories = [new DirectoryInfo(srcDir.FullName)],
                FilePatterns = ["*.cs"],
                LogLevel = LogLevel.Information,
                IncludeExtended = false,
                Verbose = false,
            };

            // Act
            List<CheckResult> results = await validator.ValidateAsync(options);

            // Assert
            Assert.That(results, Is.Not.Null);
            Assert.That(results, Is.Empty, "Expected no validation errors for valid model");
        }
        finally
        {
            try
            {
                srcDir.Delete(true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Test]
    public async Task CheckCommandWithMissingHeadingReturnsErrors()
    {
        // Arrange
        DirectoryInfo srcDir = Directory.CreateTempSubdirectory("check-src-");
        try
        {
            string csSource = """
                using Bicep.LocalDeploy;

                namespace TestModels;

                [ResourceType("TestResource")]
                [BicepDocExample("Basic Usage", "Creates a test resource", "resource test 'TestResource' = {}")]
                [BicepFrontMatter("title", "Test Resource")]
                public class TestResource
                {
                    [TypeProperty("The name of the resource", TypePropertyFlags.Required | TypePropertyFlags.Identifier)]
                    public string Name { get; init; } = string.Empty;
                }
                """;

            string srcFile = Path.Combine(srcDir.FullName, "TestResource.cs");
            await File.WriteAllTextAsync(srcFile, csSource, Encoding.UTF8);

            ConsoleLogger logger = new(LogLevel.Information);
            CheckValidator validator = new(logger);

            CheckOptions options = new()
            {
                SourceDirectories = [new DirectoryInfo(srcDir.FullName)],
                FilePatterns = ["*.cs"],
                LogLevel = LogLevel.Information,
                IncludeExtended = false,
                Verbose = false,
            };

            // Act
            List<CheckResult> results = await validator.ValidateAsync(options);

            // Assert
            Assert.That(results, Is.Not.Null);
            Assert.That(results, Has.Count.EqualTo(1));
            Assert.That(results[0].FilePath, Does.EndWith("TestResource.cs"));
            Assert.That(
                results[0].Errors,
                Has.Some.Property("ExpectedAttribute")
                    .EqualTo("[BicepDocHeading(\"Title\", \"Description\")]")
            );
        }
        finally
        {
            try
            {
                srcDir.Delete(true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Test]
    public async Task CheckCommandWithMissingExampleReturnsErrors()
    {
        // Arrange
        DirectoryInfo srcDir = Directory.CreateTempSubdirectory("check-src-");
        try
        {
            string csSource = """
                using Bicep.LocalDeploy;

                namespace TestModels;

                [ResourceType("TestResource")]
                [BicepDocHeading("Test Resource", "A test resource for validation")]
                [BicepFrontMatter("title", "Test Resource")]
                public class TestResource
                {
                    [TypeProperty("The name of the resource", TypePropertyFlags.Required | TypePropertyFlags.Identifier)]
                    public string Name { get; init; } = string.Empty;
                }
                """;

            string srcFile = Path.Combine(srcDir.FullName, "TestResource.cs");
            await File.WriteAllTextAsync(srcFile, csSource, Encoding.UTF8);

            ConsoleLogger logger = new(LogLevel.Information);
            CheckValidator validator = new(logger);

            CheckOptions options = new()
            {
                SourceDirectories = [new DirectoryInfo(srcDir.FullName)],
                FilePatterns = ["*.cs"],
                LogLevel = LogLevel.Information,
                IncludeExtended = false,
                Verbose = false,
            };

            // Act
            List<CheckResult> results = await validator.ValidateAsync(options);

            // Assert
            Assert.That(results, Is.Not.Null);
            Assert.That(results, Has.Count.EqualTo(1));
            Assert.That(results[0].FilePath, Does.EndWith("TestResource.cs"));
            Assert.That(
                results[0].Errors,
                Has.Some.Property("ExpectedAttribute")
                    .EqualTo("[BicepDocExample(\"Title\", \"Description\", \"code\")]")
            );
        }
        finally
        {
            try
            {
                srcDir.Delete(true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Test]
    public async Task CheckCommandWithMissingFrontMatterDefaultValidationReturnsNoErrors()
    {
        // Arrange
        DirectoryInfo srcDir = Directory.CreateTempSubdirectory("check-src-");
        try
        {
            string csSource = """
                using Bicep.LocalDeploy;

                namespace TestModels;

                [ResourceType("TestResource")]
                [BicepDocHeading("Test Resource", "A test resource for validation")]
                [BicepDocExample("Basic Usage", "Creates a test resource", "resource test 'TestResource' = {}")]
                public class TestResource
                {
                    [TypeProperty("The name of the resource", TypePropertyFlags.Required | TypePropertyFlags.Identifier)]
                    public string Name { get; init; } = string.Empty;
                }
                """;

            string srcFile = Path.Combine(srcDir.FullName, "TestResource.cs");
            await File.WriteAllTextAsync(srcFile, csSource, Encoding.UTF8);

            ConsoleLogger logger = new(LogLevel.Information);
            CheckValidator validator = new(logger);

            CheckOptions options = new()
            {
                SourceDirectories = [new DirectoryInfo(srcDir.FullName)],
                FilePatterns = ["*.cs"],
                LogLevel = LogLevel.Information,
                IncludeExtended = false,
                Verbose = false,
            };

            // Act
            List<CheckResult> results = await validator.ValidateAsync(options);

            // Assert
            Assert.That(results, Is.Not.Null);
            Assert.That(
                results,
                Is.Empty,
                "Expected no validation errors when BicepFrontMatter is missing but IncludeExtended is false"
            );
        }
        finally
        {
            try
            {
                srcDir.Delete(true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Test]
    public async Task CheckCommandWithAllMissingAttributesReturnsMultipleErrors()
    {
        // Arrange
        DirectoryInfo srcDir = Directory.CreateTempSubdirectory("check-src-");
        try
        {
            string csSource = """
                using Bicep.LocalDeploy;

                namespace TestModels;

                [ResourceType("TestResource")]
                public class TestResource
                {
                    [TypeProperty("The name of the resource", TypePropertyFlags.Required | TypePropertyFlags.Identifier)]
                    public string Name { get; init; } = string.Empty;
                }
                """;

            string srcFile = Path.Combine(srcDir.FullName, "TestResource.cs");
            await File.WriteAllTextAsync(srcFile, csSource, Encoding.UTF8);

            ConsoleLogger logger = new(LogLevel.Information);
            CheckValidator validator = new(logger);

            CheckOptions options = new()
            {
                SourceDirectories = [new DirectoryInfo(srcDir.FullName)],
                FilePatterns = ["*.cs"],
                LogLevel = LogLevel.Information,
                IncludeExtended = false,
                Verbose = false,
            };

            // Act
            List<CheckResult> results = await validator.ValidateAsync(options);

            // Assert
            Assert.That(results, Is.Not.Null);
            Assert.That(results, Has.Count.EqualTo(1));
            Assert.That(results[0].FilePath, Does.EndWith("TestResource.cs"));
            Assert.That(results[0].Errors, Has.Count.EqualTo(2));
            Assert.That(
                results[0].Errors,
                Has.Some.Property("ExpectedAttribute")
                    .EqualTo("[BicepDocHeading(\"Title\", \"Description\")]")
            );
            Assert.That(
                results[0].Errors,
                Has.Some.Property("ExpectedAttribute")
                    .EqualTo("[BicepDocExample(\"Title\", \"Description\", \"code\")]")
            );
            Assert.That(
                results[0].Errors,
                Has.None.Property("ExpectedAttribute")
                    .EqualTo("[BicepFrontMatter(\"key\", \"value\")]"),
                "BicepFrontMatter should not be validated by default"
            );
        }
        finally
        {
            try
            {
                srcDir.Delete(true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Test]
    public async Task CheckCommandWithCustomAttributesWhenIncludeExtendedTrueValidatesCustom()
    {
        // Arrange
        DirectoryInfo srcDir = Directory.CreateTempSubdirectory("check-src-");
        try
        {
            string csSource = """
                using Bicep.LocalDeploy;

                namespace TestModels;

                [ResourceType("TestResource")]
                [BicepDocHeading("Test Resource", "A test resource for validation")]
                [BicepDocExample("Basic Usage", "Creates a test resource", "resource test 'TestResource' = {}")]
                public class TestResource
                {
                    [TypeProperty("The name of the resource", TypePropertyFlags.Required | TypePropertyFlags.Identifier)]
                    public string Name { get; init; } = string.Empty;
                }
                """;

            string srcFile = Path.Combine(srcDir.FullName, "TestResource.cs");
            await File.WriteAllTextAsync(srcFile, csSource, Encoding.UTF8);

            ConsoleLogger logger = new(LogLevel.Information);
            CheckValidator validator = new(logger);

            CheckOptions options = new()
            {
                SourceDirectories = [new DirectoryInfo(srcDir.FullName)],
                FilePatterns = ["*.cs"],
                LogLevel = LogLevel.Information,
                IncludeExtended = true,
                Verbose = false,
            };

            // Act
            List<CheckResult> results = await validator.ValidateAsync(options);

            // Assert
            Assert.That(results, Is.Not.Null);
            Assert.That(results, Has.Count.EqualTo(1));
            Assert.That(results[0].FilePath, Does.EndWith("TestResource.cs"));
            Assert.That(
                results[0].Errors,
                Has.Some.Property("ExpectedAttribute")
                    .EqualTo("[BicepFrontMatter(\"key\", \"value\")]")
            );
        }
        finally
        {
            try
            {
                srcDir.Delete(true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Test]
    public async Task CheckCommandWithCustomAttributesWhenIncludeExtendedFalseIgnoresCustom()
    {
        // Arrange
        DirectoryInfo srcDir = Directory.CreateTempSubdirectory("check-src-");
        try
        {
            string csSource = """
                using Bicep.LocalDeploy;

                namespace TestModels;

                [ResourceType("TestResource")]
                [BicepDocHeading("Test Resource", "A test resource for validation")]
                [BicepDocExample("Basic Usage", "Creates a test resource", "resource test 'TestResource' = {}")]
                [BicepDocCustom("custom", "value")]
                public class TestResource
                {
                    [TypeProperty("The name of the resource", TypePropertyFlags.Required | TypePropertyFlags.Identifier)]
                    public string Name { get; init; } = string.Empty;
                }
                """;

            string srcFile = Path.Combine(srcDir.FullName, "TestResource.cs");
            await File.WriteAllTextAsync(srcFile, csSource, Encoding.UTF8);

            ConsoleLogger logger = new(LogLevel.Information);
            CheckValidator validator = new(logger);

            CheckOptions options = new()
            {
                SourceDirectories = [new DirectoryInfo(srcDir.FullName)],
                FilePatterns = ["*.cs"],
                LogLevel = LogLevel.Information,
                IncludeExtended = false,
                Verbose = false,
            };

            // Act
            List<CheckResult> results = await validator.ValidateAsync(options);

            // Assert
            Assert.That(results, Is.Not.Null);
            Assert.That(
                results,
                Is.Empty,
                "Expected no validation errors when IncludeExtended is false"
            );
        }
        finally
        {
            try
            {
                srcDir.Delete(true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Test]
    public async Task CheckCommandWithNonResourceTypeClassIgnoresClass()
    {
        // Arrange
        DirectoryInfo srcDir = Directory.CreateTempSubdirectory("check-src-");
        try
        {
            string csSource = """
                using Bicep.LocalDeploy;

                namespace TestModels;

                public class RegularClass
                {
                    public string Name { get; init; } = string.Empty;
                }
                """;

            string srcFile = Path.Combine(srcDir.FullName, "RegularClass.cs");
            await File.WriteAllTextAsync(srcFile, csSource, Encoding.UTF8);

            ConsoleLogger logger = new(LogLevel.Information);
            CheckValidator validator = new(logger);

            CheckOptions options = new()
            {
                SourceDirectories = [new DirectoryInfo(srcDir.FullName)],
                FilePatterns = ["*.cs"],
                LogLevel = LogLevel.Information,
                IncludeExtended = false,
                Verbose = false,
            };

            // Act
            List<CheckResult> results = await validator.ValidateAsync(options);

            // Assert
            Assert.That(results, Is.Not.Null);
            Assert.That(
                results,
                Is.Empty,
                "Expected no validation errors for non-ResourceType class"
            );
        }
        finally
        {
            try
            {
                srcDir.Delete(true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Test]
    public async Task CheckCommandWithIgnoreFileIgnoresSpecifiedFiles()
    {
        // Arrange
        DirectoryInfo srcDir = Directory.CreateTempSubdirectory("check-src-");
        try
        {
            string csSource = """
                using Bicep.LocalDeploy;

                namespace TestModels;

                [ResourceType("TestResource")]
                public class TestResource
                {
                    [TypeProperty("The name of the resource", TypePropertyFlags.Required | TypePropertyFlags.Identifier)]
                    public string Name { get; init; } = string.Empty;
                }
                """;

            string ignoreContent = "**/TestResource.cs";

            string srcFile = Path.Combine(srcDir.FullName, "TestResource.cs");
            string ignoreFile = Path.Combine(srcDir.FullName, ".biceplocalgenignore");
            await File.WriteAllTextAsync(srcFile, csSource, Encoding.UTF8);
            await File.WriteAllTextAsync(ignoreFile, ignoreContent, Encoding.UTF8);

            ConsoleLogger logger = new(LogLevel.Information);
            CheckValidator validator = new(logger);

            CheckOptions options = new()
            {
                SourceDirectories = [new DirectoryInfo(srcDir.FullName)],
                FilePatterns = ["*.cs"],
                IgnorePath = ignoreFile,
                LogLevel = LogLevel.Information,
                IncludeExtended = false,
                Verbose = false,
            };

            // Act
            List<CheckResult> results = await validator.ValidateAsync(options);

            // Assert
            Assert.That(results, Is.Not.Null);
            Assert.That(results, Is.Empty, "Expected no validation errors when file is ignored");
        }
        finally
        {
            try
            {
                srcDir.Delete(true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Test]
    public async Task CheckCommandWithMultipleFilesValidatesAll()
    {
        // Arrange
        DirectoryInfo srcDir = Directory.CreateTempSubdirectory("check-src-");
        try
        {
            string validSource = """
                using Bicep.LocalDeploy;

                namespace TestModels;

                [ResourceType("ValidResource")]
                [BicepDocHeading("Valid Resource", "A valid resource")]
                [BicepDocExample("Basic Usage", "Creates a valid resource", "resource valid 'ValidResource' = {}")]
                public class ValidResource
                {
                    [TypeProperty("The name of the resource", TypePropertyFlags.Required | TypePropertyFlags.Identifier)]
                    public string Name { get; init; } = string.Empty;
                }
                """;

            string invalidSource = """
                using Bicep.LocalDeploy;

                namespace TestModels;

                [ResourceType("InvalidResource")]
                public class InvalidResource
                {
                    [TypeProperty("The name of the resource", TypePropertyFlags.Required | TypePropertyFlags.Identifier)]
                    public string Name { get; init; } = string.Empty;
                }
                """;

            string validFile = Path.Combine(srcDir.FullName, "ValidResource.cs");
            string invalidFile = Path.Combine(srcDir.FullName, "InvalidResource.cs");
            await File.WriteAllTextAsync(validFile, validSource, Encoding.UTF8);
            await File.WriteAllTextAsync(invalidFile, invalidSource, Encoding.UTF8);

            ConsoleLogger logger = new(LogLevel.Information);
            CheckValidator validator = new(logger);

            CheckOptions options = new()
            {
                SourceDirectories = [new DirectoryInfo(srcDir.FullName)],
                FilePatterns = ["*.cs"],
                LogLevel = LogLevel.Information,
                IncludeExtended = false,
                Verbose = false,
            };

            // Act
            List<CheckResult> results = await validator.ValidateAsync(options);

            // Assert
            Assert.That(results, Is.Not.Null);
            Assert.That(results, Has.Count.EqualTo(1));
            Assert.That(results[0].FilePath, Does.EndWith("InvalidResource.cs"));
            Assert.That(
                results[0].Errors,
                Has.Some.Property("ExpectedAttribute")
                    .EqualTo("[BicepDocHeading(\"Title\", \"Description\")]")
            );
            Assert.That(
                results[0].Errors,
                Has.Some.Property("ExpectedAttribute")
                    .EqualTo("[BicepDocExample(\"Title\", \"Description\", \"code\")]")
            );
        }
        finally
        {
            try
            {
                srcDir.Delete(true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}
