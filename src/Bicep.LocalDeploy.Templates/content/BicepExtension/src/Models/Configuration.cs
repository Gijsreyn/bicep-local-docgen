using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Types.Attributes;

namespace MyExtension.Models;

public class Configuration
{
    [TypeProperty("The base URL for the API endpoint.", ObjectTypePropertyFlags.Required)]
    public required string BaseUrl { get; set; }
}
