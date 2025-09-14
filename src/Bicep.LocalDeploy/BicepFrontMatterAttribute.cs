namespace Bicep.LocalDeploy
{
    /// <summary>
    /// Declares front matter metadata for generated documentation. Use keys like "title", "description", or custom values.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = true)]
    public sealed class BicepFrontMatterAttribute : Attribute
    {
        /// <param name="key">Front matter key, e.g. "title", "description", "category".</param>
        /// <param name="value">Front matter value.</param>
        public BicepFrontMatterAttribute(string key, string value)
        {
            Key = key;
            Value = value;
            BlockIndex = 1;
        }

        /// <summary>
        /// Creates a front matter entry for a specific block number (1-based).
        /// </summary>
        /// <param name="blockIndex">1-based block index. 1 is the first block.</param>
        /// <param name="key">Front matter key.</param>
        /// <param name="value">Front matter value.</param>
        public BicepFrontMatterAttribute(int blockIndex, string key, string value)
        {
            BlockIndex = blockIndex < 1 ? 1 : blockIndex;
            Key = key;
            Value = value;
        }

        /// <summary>1-based front matter block index. Defaults to 1.</summary>
        public int BlockIndex { get; }

        /// <summary>Front matter key.</summary>
        public string Key { get; }

        /// <summary>Front matter value.</summary>
        public string Value { get; }
    }
}
