using System.Text;
using Bicep.LocalDeploy.DocGenerator.Services;
using NUnit.Framework;

namespace Bicep.LocalDeploy.DocGenerator.Tests;

[TestFixture]
public class DocGeneratorTests
{
    private static string ReadFile(string path) => File.ReadAllText(path, Encoding.UTF8);

    private static async Task<string> GenerateMarkdownAsync(string csSource, string h1Contains)
    {
        var outDir = Directory.CreateTempSubdirectory("docgen-out-");
        var srcDir = Directory.CreateTempSubdirectory("docgen-src-");
        try
        {
            var srcFile = Path.Combine(srcDir.FullName, "Model.cs");
            await File.WriteAllTextAsync(srcFile, csSource, Encoding.UTF8);

            var options = new GenerationOptions
            {
                SourceDirectories = [new DirectoryInfo(srcDir.FullName)],
                FilePatterns = ["*.cs"],
                OutputDirectory = new DirectoryInfo(outDir.FullName),
                Verbose = false,
                Force = true,
            };

            await DocumentationGenerator.GenerateAsync(options);

            var files = Directory.GetFiles(outDir.FullName, "*.md");
            Assert.That(files, Is.Not.Empty, "No markdown files were generated");
            foreach (var f in files)
            {
                var content = ReadFile(f);
                if (content.Contains("# " + h1Contains, StringComparison.Ordinal))
                {
                    return content;
                }
            }
            return ReadFile(files.First());
        }
        finally
        {
            try
            {
                srcDir.Delete(true);
            }
            catch
            { /* ignore */
            }
            try
            {
                outDir.Delete(true);
            }
            catch
            { /* ignore */
            }
        }
    }

    [Test]
    public async Task DirectoryModelGeneratesMarkdownWithExpectedSections()
    {
        var cs = """
using Bicep.LocalDeploy;
using Bicep.Local.Extension.Types.Attributes;

namespace Demo.Generated;

public enum Color
{
    Red,
    Green,
    Blue
}

[BicepFrontMatter("title", "Demo Widget")]
[BicepFrontMatter("description", "A demo resource used for tests")]
[BicepDocHeading("Widget", "Represents a demo widget resource.")]
[BicepDocExample(
    "Create widget",
    "Creates a widget",
    @"resource widget 'demo/widget@2025-01-01' = {
  name: 'example'
}",
    "bicep")]
[BicepDocCustom("Notes", "This is a custom notes section.")]
[ResourceType("Demo.Widget")]
public class Widget : WidgetBase
{
    [TypeProperty("The name of the widget", ObjectTypePropertyFlags.Required | ObjectTypePropertyFlags.Identifier)]
    public string Name { get; set; }

    [TypeProperty("The description of the widget", ObjectTypePropertyFlags.None)]
    public string? Description { get; set; }

    [TypeProperty("The color of the widget", ObjectTypePropertyFlags.None)]
    public Color? Color { get; set; }

    [TypeProperty("The computed size of the widget", ObjectTypePropertyFlags.ReadOnly)]
    public int? Size { get; set; }
}

public class WidgetBase
{
    [TypeProperty("The resource group path", ObjectTypePropertyFlags.Required | ObjectTypePropertyFlags.Identifier)]
    public string Path { get; set; }
}
""";

        string md = await GenerateMarkdownAsync(cs, "Widget");
        var doc = MarkdownParsingExtensions.ParseMarkdown(md);
        var yaml = doc.FrontMatters().FirstOrDefault();
        Assert.That(yaml, Is.Not.Null, "YAML front matter missing");
        var headings = doc.Headings().ToList();
        Assert.That(headings.Count(h => h.Level == "#"), Is.EqualTo(1), "Exactly one H1 expected");
        Assert.That(md, Does.Contain("# Widget"), "H1 title missing or incorrect");
        var firstPara = doc.GetFirstParagraph();
        Assert.That(firstPara, Is.Not.Empty, "Description paragraph under H1 is missing");
        Assert.That(md, Does.Contain("## Example usage"), "Example usage section missing");
        Assert.That(md, Does.Contain("```bicep"), "Bicep code fence missing");
        Assert.That(
            md,
            Does.Contain("## Argument reference"),
            "Argument reference section missing"
        );
        Assert.That(md, Does.Contain("- `path`"), "Expected argument bullet for 'path'");
        Assert.That(md, Does.Contain("- `name`"), "Expected argument bullet for 'name'");
        Assert.That(md, Does.Contain("color"), "Expected argument bullet for enum 'color'");
        Assert.That(
            md,
            Does.Contain("Can be `Red`, `Green`, or `Blue`"),
            "Expected enum values list"
        );
        Assert.That(
            md,
            Does.Contain("## Attribute reference"),
            "Attribute reference section missing"
        );
        Assert.That(md, Does.Contain("- `size`"), "Expected output bullet for 'size'");
        Assert.That(md, Does.Contain("## Notes"), "Custom Notes section missing");
    }

    [Test]
    public async Task SingleRequiredPropertyGeneratesOnlyBasicExampleNoAdvanced()
    {
        var cs = """
using Bicep.LocalDeploy;
using Bicep.Local.Extension.Types.Attributes;

namespace Edge.SingleRequired;

[BicepDocHeading("SingleReq", "Model with a single required property.")]
[ResourceType("Edge.SingleReq")]
public class SingleReq
{
    [TypeProperty("The name.", ObjectTypePropertyFlags.Required | ObjectTypePropertyFlags.Identifier)]
    public string Name { get; set; }
}
""";

        string md = await GenerateMarkdownAsync(cs, "SingleReq");
        Assert.That(md, Does.Contain("# SingleReq"));
        Assert.That(md, Does.Contain("## Example usage"));
        Assert.That(md, Does.Contain("### Basic Edge.SingleReq"));
        Assert.That(md, Does.Not.Contain("### Advanced"));
        Assert.That(md, Does.Contain("- `name` - (Required)"));
        Assert.That(md, Does.Contain("## Argument reference"));
    }

    [Test]
    public async Task NoOutputsOmitsAttributeReferenceSection()
    {
        var cs = """
using Bicep.LocalDeploy;
using Bicep.Local.Extension.Types.Attributes;

namespace Edge.NoOutputs;

[BicepDocHeading("NoOutputs", "Model with arguments only.")]
[ResourceType("Edge.NoOutputs")]
public class NoOutputs
{
    [TypeProperty("The id.", ObjectTypePropertyFlags.Required | ObjectTypePropertyFlags.Identifier)]
    public string Id { get; set; }

    [TypeProperty("Optional comment.", ObjectTypePropertyFlags.None)]
    public string? Comment { get; set; }
}
""";

        string md = await GenerateMarkdownAsync(cs, "NoOutputs");
        Assert.That(md, Does.Contain("# NoOutputs"));
        Assert.That(md, Does.Contain("## Argument reference"));
        Assert.That(md, Does.Not.Contain("## Attribute reference"));
    }

    [Test]
    public async Task HeadingFallsBackToFrontMatterTitleWhenNoHeadingAttribute()
    {
        var cs = """
using Bicep.LocalDeploy;
using Bicep.Local.Extension.Types.Attributes;

namespace Edge.FrontTitle;

[BicepFrontMatter(1, "title", "Front Title")]
[ResourceType("Edge.FrontTitle")]
public class FrontTitle
{
    [TypeProperty("Required val.", ObjectTypePropertyFlags.Required | ObjectTypePropertyFlags.Identifier)]
    public string Val { get; set; }
}
""";

        string md = await GenerateMarkdownAsync(cs, "Front Title");
        Assert.That(md, Does.Contain("# Front Title"));
    }

    [Test]
    public async Task HeadingFallsBackToResourceTypeNameWhenNoHeadingAndNoFrontMatter()
    {
        var cs = """
using Bicep.LocalDeploy;
using Bicep.Local.Extension.Types.Attributes;

namespace Edge.Fallback;

[ResourceType("Edge.Fallback")]
public class Fallback
{
    [TypeProperty("Required val.", ObjectTypePropertyFlags.Required | ObjectTypePropertyFlags.Identifier)]
    public string Val { get; set; }
}
""";

        string md = await GenerateMarkdownAsync(cs, "Edge.Fallback");
        Assert.That(md, Does.Contain("# Edge.Fallback"));
    }

    [Test]
    public async Task EnumSingleValueShowsSingularSuffix()
    {
        var cs = """
using Bicep.LocalDeploy;
using Bicep.Local.Extension.Types.Attributes;

namespace Edge.EnumOne;

public enum Only
{
    One
}

[BicepDocHeading("EnumOne", "Enum with a single value.")]
[ResourceType("Edge.EnumOne")]
public class EnumOne
{
    [TypeProperty("Pick one.", ObjectTypePropertyFlags.None)]
    public Only? Choice { get; set; }
}
""";

        string md = await GenerateMarkdownAsync(cs, "EnumOne");
        Assert.That(md, Does.Contain("Can be `One`"));
    }

    [Test]
    public async Task NestedTypeExpandsNestedMembers()
    {
        var cs = """
using Bicep.LocalDeploy;
using Bicep.Local.Extension.Types.Attributes;

namespace Edge.Nested;

public class Child
{
    [TypeProperty("Child prop.", ObjectTypePropertyFlags.None)]
    public string Info { get; set; }
}

[BicepDocHeading("Nested", "Parent with nested child type.")]
[ResourceType("Edge.Nested")]
public class Parent
{
    [TypeProperty("Nested child.", ObjectTypePropertyFlags.None)]
    public Child? Child { get; set; }
}
""";

        string md = await GenerateMarkdownAsync(cs, "Nested");
        Assert.That(md, Does.Contain("- `child` - (Optional) Nested child:"));
        Assert.That(md, Does.Contain("  - `info` - (Optional) Child prop."));
    }

    [Test]
    public async Task MultiBlockFrontMatterWritesTwoBlocksInOrderAndSortsKeys()
    {
        var cs = """
using Bicep.LocalDeploy;
using Bicep.Local.Extension.Types.Attributes;

namespace Edge.FrontMatter;

[BicepFrontMatter(2, "zeta", "block2-zeta")]
[BicepFrontMatter(2, "alpha", "block2-alpha")]
[BicepFrontMatter(1, "title", "Block1Title")]
[BicepFrontMatter(1, "another", "block1-another")]
[BicepDocHeading("FM", "Has multiple front matter blocks.")]
[ResourceType("Edge.FrontMatter")]
public class FrontMatter
{
    [TypeProperty("Required.", ObjectTypePropertyFlags.Required | ObjectTypePropertyFlags.Identifier)]
    public string Name { get; set; }
}
""";

        string md = await GenerateMarkdownAsync(cs, "FM");
    // Verify order using raw content: block1 (another, title) appears before block2 (alpha, zeta)
        int idxBlock1Another = md.IndexOf("another: \"block1-another\"", StringComparison.Ordinal);
        int idxBlock1Title = md.IndexOf("title: \"Block1Title\"", StringComparison.Ordinal);
        int idxBlock2Alpha = md.IndexOf("alpha: \"block2-alpha\"", StringComparison.Ordinal);
        int idxBlock2Zeta = md.IndexOf("zeta: \"block2-zeta\"", StringComparison.Ordinal);
        Assert.That(idxBlock1Another, Is.GreaterThanOrEqualTo(0));
        Assert.That(idxBlock1Title, Is.GreaterThan(idxBlock1Another));
        Assert.That(idxBlock2Alpha, Is.GreaterThan(idxBlock1Title));
        Assert.That(idxBlock2Zeta, Is.GreaterThan(idxBlock2Alpha));
    }

    [Test]
    public async Task HeadingAttributeOverridesFrontMatterTitle()
    {
        var cs = """
using Bicep.LocalDeploy;
using Bicep.Local.Extension.Types.Attributes;

namespace Edge.HeadingPrecedence;

[BicepFrontMatter("title", "Ignored Title")]
[BicepDocHeading("Real Title", "Heading wins over front matter title.")]
[ResourceType("Edge.HeadingPrecedence")]
public class HP
{
    [TypeProperty("Id.", ObjectTypePropertyFlags.Required | ObjectTypePropertyFlags.Identifier)]
    public string Id { get; set; }
}
""";

        string md = await GenerateMarkdownAsync(cs, "Real Title");
        Assert.That(md, Does.Contain("# Real Title"));
        Assert.That(md, Does.Not.Contain("# Ignored Title"));
    }

    [Test]
    public async Task EnumMultiValuesFormatsGrammarWithCommasAndOr()
    {
        var cs = """
using Bicep.LocalDeploy;
using Bicep.Local.Extension.Types.Attributes;

namespace Edge.EnumMany;

public enum Many { A, B, C }

[BicepDocHeading("EnumMany", "Enum with multiple values.")]
[ResourceType("Edge.EnumMany")]
public class EnumMany
{
    [TypeProperty("Pick.", ObjectTypePropertyFlags.None)]
    public Many? Choice { get; set; }
}
""";

        string md = await GenerateMarkdownAsync(cs, "EnumMany");
        Assert.That(md, Does.Contain("(Can be `A`, `B`, or `C`)"));
    }
}
