using System;

namespace Bicep.LocalDeploy;

[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Struct,
    AllowMultiple = true,
    Inherited = true
)]
public sealed class BicepDocCustomAttribute : Attribute
{
    public BicepDocCustomAttribute(string title, string description, string body)
    {
        Title = title;
        Description = description;
        Body = body;
    }

    public string Title { get; }
    public string Description { get; }
    public string Body { get; }
}
