namespace Slicito.Proclaimer;

/// <summary>
/// Attribute values for the canonical Proclaimer schema.
/// </summary>
public static class ProclaimerAttributeValues
{
    /// <summary>
    /// Element type kind values (for Kind attribute).
    /// </summary>
    public static class Kind
    {
        public const string Service = "proclaimer.service";
        public const string Endpoint = "proclaimer.endpoint";
        public const string HttpClient = "proclaimer.http_client";
        public const string Repository = "proclaimer.repository";
        public const string Database = "proclaimer.database";
        public const string Queue = "proclaimer.queue";
        public const string Topic = "proclaimer.topic";
        public const string BackgroundService = "proclaimer.background_service";
    }

    /// <summary>
    /// Link type kind values (for Kind attribute on links).
    /// </summary>
    public static class LinkKind
    {
        public const string BelongsToService = "proclaimer.belongs_to_service";
        public const string Calls = "proclaimer.calls";
        public const string SendsHttpRequest = "proclaimer.sends_http_request";
        public const string WritesTo = "proclaimer.writes_to";
        public const string ReadsFrom = "proclaimer.reads_from";
        public const string PublishesTo = "proclaimer.publishes_to";
        public const string ConsumesFrom = "proclaimer.consumes_from";
    }
}
