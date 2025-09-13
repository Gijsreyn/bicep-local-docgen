using System;

namespace Bicep.LocalDeploy;

/// <summary>
/// Defines a custom documentation section for a resource type.
/// Only a section <see cref="Title"/> and <see cref="Description"/> are supported.
/// </summary>
/// <remarks>
/// This attribute can be applied multiple times to the same type to render multiple custom sections.
/// </remarks>
[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Struct,
    AllowMultiple = true,
    Inherited = true
)]
public sealed class BicepDocCustomAttribute : Attribute
{
    /// <summary>
    /// Creates a new custom documentation section.
    /// </summary>
    /// <param name="title">The section title. Rendered as an H2 in the generated markdown.</param>
    /// <param name="description">An optional description paragraph below the title.</param>
    public BicepDocCustomAttribute(string title, string description)
    {
        Title = title;
        Description = description;
    }

    /// <summary>Section title.</summary>
    public string Title { get; }

    /// <summary>Section description paragraph.</summary>
    public string Description { get; }
}
