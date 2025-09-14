namespace Bicep.LocalDeploy
{
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
    public sealed class BicepDocCustomAttribute(string title, string description) : Attribute
    {
        /// <summary>Section title.</summary>
        public string Title { get; } = title;

        /// <summary>Section description paragraph.</summary>
        public string Description { get; } = description;
    }
}
