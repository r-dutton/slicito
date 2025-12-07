namespace Slicito.Proclaimer;

/// <summary>
/// Attribute names for Proclaimer schema.
/// </summary>
public static class ProclaimerAttributeNames
{
    // Core identification
    public const string Kind = "Kind";
    
    // HTTP-related
    public const string Verb = "Verb";
    public const string Route = "Route";
    public const string BaseUrl = "BaseUrl";
    
    // Configuration
    public const string ConfigKey = "ConfigKey";
    
    // HTTP client
    public const string ClientMethod = "ClientMethod";
    public const string External = "External";
    
    // Data access
    public const string Contract = "Contract";
    public const string Entity = "Entity";
    public const string Table = "Table";
    
    // Analysis metadata
    public const string Provenance = "Provenance";
    public const string Confidence = "Confidence";
}
