namespace Slicito.Proclaimer;

/// <summary>
/// Attribute values for Proclaimer schema.
/// </summary>
public static class ProclaimerAttributeValues
{
    /// <summary>
    /// Element type kind values (for Kind attribute).
    /// </summary>
    public static class Kind
    {
        // Endpoints
        public const string EndpointController = "endpoint.controller";
        public const string EndpointMinimalApi = "endpoint.minimal_api";
        public const string ControllerAction = "controller.action";
        public const string ControllerResponse = "controller.response";
        
        // Communication
        public const string HttpClient = "http.client";
        
        // CQRS/MediatR
        public const string CqrsRequest = "cqrs.request";
        public const string CqrsHandler = "cqrs.handler";
        public const string MediatrSend = "mediatr.send";
        public const string MediatrPublish = "mediatr.publish";
        public const string NotificationHandler = "notification.handler";
        
        // Data access
        public const string EfDbContext = "ef.db_context";
        public const string EfEntity = "ef.entity";
        public const string DbTable = "db.table";
        public const string Repository = "app.repository";
        
        // Messaging
        public const string MessagePublisher = "message.publisher";
        public const string MessageContract = "message.contract";
        
        // Application
        public const string AppService = "app.service";
        public const string BackgroundService = "app.background_service";
        
        // Mapping
        public const string MappingProfile = "mapping.automapper.profile";
        public const string MappingMap = "mapping.automapper.map";
        
        // Configuration & Validation
        public const string Validator = "validator";
        public const string Validation = "validation";
        public const string ConfigValue = "config.value";
        public const string Options = "config.options";
        public const string OptionsPoco = "config.options_poco";
        
        // Caching
        public const string CacheOperation = "cache.operation";
    }
    
    /// <summary>
    /// Link type kind values (for Kind attribute on links).
    /// </summary>
    public static class LinkKind
    {
        public const string Calls = "calls";
        public const string SendsRequest = "sends_request";
        public const string HandledBy = "handled_by";
        public const string ProcessedBy = "processed_by";
        public const string MapsTo = "maps_to";
        public const string UsesClient = "uses_client";
        public const string UsesService = "uses_service";
        public const string UsesStorage = "uses_storage";
        public const string UsesCache = "uses_cache";
        public const string UsesOptions = "uses_options";
        public const string UsesConfiguration = "uses_configuration";
        public const string ReadsFrom = "reads_from";
        public const string Queries = "queries";
        public const string Publishes = "publishes";
        public const string PublishesNotification = "publishes_notification";
        public const string PublishesDomainEvent = "publishes_domain_event";
        public const string RequestProcessorDispatch = "requestprocessor.dispatch";
        public const string GeneratedFrom = "generated_from";
        public const string Implements = "implemented_by";
        public const string InvokesDomain = "invokes_domain";
        public const string Logs = "logs";
        public const string Validates = "validates";
        public const string Validation = "validation";
        public const string ServiceLocated = "service_located";
        public const string Returns = "returns";
        public const string CastsTo = "casts_to";
        public const string ConvertsTo = "converts_to";
        public const string Manages = "manages";
    }
}
