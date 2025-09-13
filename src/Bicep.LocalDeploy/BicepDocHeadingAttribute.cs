using System;

namespace Bicep.LocalDeploy;

/// <summary>
/// Specifies the document heading (H1) and its description for a resource model.
/// </summary>
/// <remarks>
/// This attribute controls the first heading of the generated markdown and the paragraph under it.
/// Use <see cref="BicepFrontMatterAttribute"/> exclusively for YAML front matter data.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
public sealed class BicepDocHeadingAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of <see cref="BicepDocHeadingAttribute"/>.
    /// </summary>
    /// <param name="title">The H1 title to use in the generated markdown.</param>
    /// <param name="description">The paragraph shown under the H1.</param>
    public BicepDocHeadingAttribute(string title, string description)
    {
        Title = title;
        Description = description;
    }

    /// <summary>The H1 title.</summary>
    public string Title { get; }

    /// <summary>The description paragraph below the H1.</summary>
    public string Description { get; }
}
