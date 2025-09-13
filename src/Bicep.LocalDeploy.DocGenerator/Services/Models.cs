namespace Bicep.LocalDeploy.DocGenerator.Services;

public sealed class AnalysisResult
{
    public List<TypeInfoModel> Types { get; } = new();
}

public sealed class TypeInfoModel
{
    public string Name { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string? ResourceTypeName { get; set; }
    public string SourceFile { get; set; } = string.Empty;
    public List<MemberInfoModel> Members { get; } = new();
    public List<string> BaseTypes { get; } = new();
    public Dictionary<string, string> FrontMatter { get; } = new();
    public string? Summary { get; set; }
    public List<ExampleModel> Examples { get; } = new();
    public List<CustomSectionModel> CustomSections { get; } = new();
}

public sealed class MemberInfoModel
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsRequired { get; set; }
    public bool IsReadOnly { get; set; }
    public bool IsIdentifier { get; set; }
    public bool IsEnum { get; set; }
    public List<string> EnumValues { get; set; } = new();
}

public sealed class ExampleModel
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Language { get; set; } = "bicep";
}

public sealed class CustomSectionModel
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty; // Markdown body
}
