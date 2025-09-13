using System;

namespace Bicep.LocalDeploy;

[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Struct,
    AllowMultiple = true,
    Inherited = true
)]
public sealed class BicepDocExampleAttribute : Attribute
{
    public BicepDocExampleAttribute(string title, string description, string code)
    {
        Title = title;
        Description = description;
        Code = code;
        Language = "bicep";
    }

    public BicepDocExampleAttribute(string title, string description, string code, string language)
    {
        Title = title;
        Description = description;
        Code = code;
        Language = language;
    }

    public string Title { get; }
    public string Description { get; }
    public string Code { get; }
    public string Language { get; }
}
