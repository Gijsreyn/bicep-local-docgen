using System;

namespace Bicep.LocalDeploy;

[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Struct,
    AllowMultiple = true,
    Inherited = true
)]
public sealed class BicepDocMetadataAttribute : Attribute
{
    public BicepDocMetadataAttribute(string key, string value)
    {
        Key = key;
        Value = value;
    }

    public string Key { get; }
    public string Value { get; }
}
