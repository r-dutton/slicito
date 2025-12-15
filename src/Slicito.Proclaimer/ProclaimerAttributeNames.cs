namespace Slicito.Proclaimer;

/// <summary>
/// Attribute names for the canonical Proclaimer slice schema.
/// </summary>
public static class ProclaimerAttributeNames
{
    // Core identification
    public const string Kind = "Kind";
    public const string ServiceName = "ServiceName";

    // Endpoint & HTTP
    public const string HttpMethod = "HttpMethod";
    public const string Route = "Route";
    public const string UrlTemplate = "UrlTemplate";
    public const string TargetService = "TargetService";

    // Data access
    public const string RepositoryName = "RepositoryName";
    public const string DatabaseName = "DatabaseName";

    // Messaging
    public const string QueueName = "QueueName";
    public const string TopicName = "TopicName";
    public const string MessageType = "MessageType";

    // Analysis metadata
    public const string Provenance = "Provenance";
    public const string Confidence = "Confidence";
    public const string External = "External";
}
