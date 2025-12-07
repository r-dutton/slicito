using Slicito.Abstractions;
using Slicito.Abstractions.Facts;
using Slicito.ProgramAnalysis;

namespace Slicito.Proclaimer;

/// <summary>
/// Provides strongly-typed access to Proclaimer element and link types.
/// </summary>
public class ProclaimerTypes : IProgramTypes
{
    private readonly ITypeSystem _typeSystem;
    
    public ProclaimerTypes(ITypeSystem typeSystem)
    {
        _typeSystem = typeSystem;
        
        // Initialize element types
        EndpointController = GetElementType(ProclaimerAttributeValues.Kind.EndpointController);
        EndpointMinimalApi = GetElementType(ProclaimerAttributeValues.Kind.EndpointMinimalApi);
        ControllerAction = GetElementType(ProclaimerAttributeValues.Kind.ControllerAction);
        ControllerResponse = GetElementType(ProclaimerAttributeValues.Kind.ControllerResponse);
        
        HttpClient = GetElementType(ProclaimerAttributeValues.Kind.HttpClient);
        
        CqrsRequest = GetElementType(ProclaimerAttributeValues.Kind.CqrsRequest);
        CqrsHandler = GetElementType(ProclaimerAttributeValues.Kind.CqrsHandler);
        MediatrSend = GetElementType(ProclaimerAttributeValues.Kind.MediatrSend);
        MediatrPublish = GetElementType(ProclaimerAttributeValues.Kind.MediatrPublish);
        NotificationHandler = GetElementType(ProclaimerAttributeValues.Kind.NotificationHandler);
        
        EfDbContext = GetElementType(ProclaimerAttributeValues.Kind.EfDbContext);
        EfEntity = GetElementType(ProclaimerAttributeValues.Kind.EfEntity);
        DbTable = GetElementType(ProclaimerAttributeValues.Kind.DbTable);
        Repository = GetElementType(ProclaimerAttributeValues.Kind.Repository);
        
        MessagePublisher = GetElementType(ProclaimerAttributeValues.Kind.MessagePublisher);
        MessageContract = GetElementType(ProclaimerAttributeValues.Kind.MessageContract);
        
        AppService = GetElementType(ProclaimerAttributeValues.Kind.AppService);
        BackgroundService = GetElementType(ProclaimerAttributeValues.Kind.BackgroundService);
        
        MappingProfile = GetElementType(ProclaimerAttributeValues.Kind.MappingProfile);
        MappingMap = GetElementType(ProclaimerAttributeValues.Kind.MappingMap);
        
        Validator = GetElementType(ProclaimerAttributeValues.Kind.Validator);
        Validation = GetElementType(ProclaimerAttributeValues.Kind.Validation);
        ConfigValue = GetElementType(ProclaimerAttributeValues.Kind.ConfigValue);
        Options = GetElementType(ProclaimerAttributeValues.Kind.Options);
        OptionsPoco = GetElementType(ProclaimerAttributeValues.Kind.OptionsPoco);
        
        CacheOperation = GetElementType(ProclaimerAttributeValues.Kind.CacheOperation);
        
        // Initialize link types
        Contains = _typeSystem.GetLinkType([(CommonAttributeNames.Kind, CommonAttributeValues.Kind.Contains)]);
        Calls = GetLinkType(ProclaimerAttributeValues.LinkKind.Calls);
        SendsRequest = GetLinkType(ProclaimerAttributeValues.LinkKind.SendsRequest);
        HandledBy = GetLinkType(ProclaimerAttributeValues.LinkKind.HandledBy);
        ProcessedBy = GetLinkType(ProclaimerAttributeValues.LinkKind.ProcessedBy);
        MapsTo = GetLinkType(ProclaimerAttributeValues.LinkKind.MapsTo);
        UsesClient = GetLinkType(ProclaimerAttributeValues.LinkKind.UsesClient);
        UsesService = GetLinkType(ProclaimerAttributeValues.LinkKind.UsesService);
        UsesStorage = GetLinkType(ProclaimerAttributeValues.LinkKind.UsesStorage);
        UsesCache = GetLinkType(ProclaimerAttributeValues.LinkKind.UsesCache);
        UsesOptions = GetLinkType(ProclaimerAttributeValues.LinkKind.UsesOptions);
        UsesConfiguration = GetLinkType(ProclaimerAttributeValues.LinkKind.UsesConfiguration);
        ReadsFrom = GetLinkType(ProclaimerAttributeValues.LinkKind.ReadsFrom);
        Queries = GetLinkType(ProclaimerAttributeValues.LinkKind.Queries);
        Publishes = GetLinkType(ProclaimerAttributeValues.LinkKind.Publishes);
        PublishesNotification = GetLinkType(ProclaimerAttributeValues.LinkKind.PublishesNotification);
        PublishesDomainEvent = GetLinkType(ProclaimerAttributeValues.LinkKind.PublishesDomainEvent);
        RequestProcessorDispatch = GetLinkType(ProclaimerAttributeValues.LinkKind.RequestProcessorDispatch);
        GeneratedFrom = GetLinkType(ProclaimerAttributeValues.LinkKind.GeneratedFrom);
        Implements = GetLinkType(ProclaimerAttributeValues.LinkKind.Implements);
        InvokesDomain = GetLinkType(ProclaimerAttributeValues.LinkKind.InvokesDomain);
        Logs = GetLinkType(ProclaimerAttributeValues.LinkKind.Logs);
        Validates = GetLinkType(ProclaimerAttributeValues.LinkKind.Validates);
        ValidationLink = GetLinkType(ProclaimerAttributeValues.LinkKind.Validation);
        ServiceLocated = GetLinkType(ProclaimerAttributeValues.LinkKind.ServiceLocated);
        Returns = GetLinkType(ProclaimerAttributeValues.LinkKind.Returns);
        CastsTo = GetLinkType(ProclaimerAttributeValues.LinkKind.CastsTo);
        ConvertsTo = GetLinkType(ProclaimerAttributeValues.LinkKind.ConvertsTo);
        Manages = GetLinkType(ProclaimerAttributeValues.LinkKind.Manages);
    }
    
    // Element types - Endpoints
    public ElementType EndpointController { get; }
    public ElementType EndpointMinimalApi { get; }
    public ElementType ControllerAction { get; }
    public ElementType ControllerResponse { get; }
    
    // Element types - Communication
    public ElementType HttpClient { get; }
    
    // Element types - CQRS/MediatR
    public ElementType CqrsRequest { get; }
    public ElementType CqrsHandler { get; }
    public ElementType MediatrSend { get; }
    public ElementType MediatrPublish { get; }
    public ElementType NotificationHandler { get; }
    
    // Element types - Data access
    public ElementType EfDbContext { get; }
    public ElementType EfEntity { get; }
    public ElementType DbTable { get; }
    public ElementType Repository { get; }
    
    // Element types - Messaging
    public ElementType MessagePublisher { get; }
    public ElementType MessageContract { get; }
    
    // Element types - Application
    public ElementType AppService { get; }
    public ElementType BackgroundService { get; }
    
    // Element types - Mapping
    public ElementType MappingProfile { get; }
    public ElementType MappingMap { get; }
    
    // Element types - Configuration & Validation
    public ElementType Validator { get; }
    public ElementType Validation { get; }
    public ElementType ConfigValue { get; }
    public ElementType Options { get; }
    public ElementType OptionsPoco { get; }
    
    // Element types - Caching
    public ElementType CacheOperation { get; }
    
    // Link types
    public LinkType Contains { get; } // For compatibility with IProgramTypes
    public LinkType Calls { get; }
    public LinkType SendsRequest { get; }
    public LinkType HandledBy { get; }
    public LinkType ProcessedBy { get; }
    public LinkType MapsTo { get; }
    public LinkType UsesClient { get; }
    public LinkType UsesService { get; }
    public LinkType UsesStorage { get; }
    public LinkType UsesCache { get; }
    public LinkType UsesOptions { get; }
    public LinkType UsesConfiguration { get; }
    public LinkType ReadsFrom { get; }
    public LinkType Queries { get; }
    public LinkType Publishes { get; }
    public LinkType PublishesNotification { get; }
    public LinkType PublishesDomainEvent { get; }
    public LinkType RequestProcessorDispatch { get; }
    public LinkType GeneratedFrom { get; }
    public LinkType Implements { get; }
    public LinkType InvokesDomain { get; }
    public LinkType Logs { get; }
    public LinkType Validates { get; }
    public LinkType ValidationLink { get; }
    public LinkType ServiceLocated { get; }
    public LinkType Returns { get; }
    public LinkType CastsTo { get; }
    public LinkType ConvertsTo { get; }
    public LinkType Manages { get; }
    
    // IProgramTypes implementation (for compatibility with Slicito's program analysis)
    ElementType IProgramTypes.Procedure => ControllerAction | CqrsHandler | NotificationHandler;
    ElementType IProgramTypes.NestedProcedures => _typeSystem.GetElementType([("__none__", "__none__")]); // Not used in Proclaimer
    ElementType IProgramTypes.Operation => _typeSystem.GetElementType([("__none__", "__none__")]); // Not used in Proclaimer
    ElementType IProgramTypes.Call => _typeSystem.GetElementType([("__none__", "__none__")]); // Not used in Proclaimer
    
    bool IProgramTypes.HasName(ElementType elementType) => true;
    
    public bool HasCodeLocation(ElementType elementType) => true;
    
    private ElementType GetElementType(string kind) =>
        _typeSystem.GetElementType([(ProclaimerAttributeNames.Kind, kind)]);
    
    private LinkType GetLinkType(string kind) =>
        _typeSystem.GetLinkType([(ProclaimerAttributeNames.Kind, kind)]);
}
