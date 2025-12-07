using System.Collections.Immutable;
using Slicito.Abstractions;

namespace Slicito.Proclaimer;

/// <summary>
/// Extension methods for building Proclaimer schema.
/// </summary>
public static class ProclaimerSchema
{
    /// <summary>
    /// Registers Proclaimer element types, link types, and attributes into a slice schema.
    /// </summary>
    public static SliceSchema AddProclaimerSchema(ProclaimerTypes types)
    {
        var elementTypes = ImmutableArray.Create(
            // Endpoints
            types.EndpointController,
            types.EndpointMinimalApi,
            types.ControllerAction,
            types.ControllerResponse,
            
            // Communication
            types.HttpClient,
            
            // CQRS/MediatR
            types.CqrsRequest,
            types.CqrsHandler,
            types.MediatrSend,
            types.MediatrPublish,
            types.NotificationHandler,
            
            // Data access
            types.EfDbContext,
            types.EfEntity,
            types.DbTable,
            types.Repository,
            
            // Messaging
            types.MessagePublisher,
            types.MessageContract,
            
            // Application
            types.AppService,
            types.BackgroundService,
            
            // Mapping
            types.MappingProfile,
            types.MappingMap,
            
            // Configuration & Validation
            types.Validator,
            types.Validation,
            types.ConfigValue,
            types.Options,
            types.OptionsPoco,
            
            // Caching
            types.CacheOperation
        );
        
        var linkTypes = ImmutableDictionary.CreateBuilder<LinkType, ImmutableArray<LinkElementTypes>>();
        
        // For now, we'll allow all link types to connect any element types
        // This will be refined as we implement specific analyzers
        var anyElementType = elementTypes.Aggregate((a, b) => a | b);
        var anyLink = new LinkElementTypes(anyElementType, anyElementType);
        
        linkTypes[types.Calls] = ImmutableArray.Create(anyLink);
        linkTypes[types.SendsRequest] = ImmutableArray.Create(anyLink);
        linkTypes[types.HandledBy] = ImmutableArray.Create(anyLink);
        linkTypes[types.ProcessedBy] = ImmutableArray.Create(anyLink);
        linkTypes[types.MapsTo] = ImmutableArray.Create(anyLink);
        linkTypes[types.UsesClient] = ImmutableArray.Create(anyLink);
        linkTypes[types.UsesService] = ImmutableArray.Create(anyLink);
        linkTypes[types.UsesStorage] = ImmutableArray.Create(anyLink);
        linkTypes[types.UsesCache] = ImmutableArray.Create(anyLink);
        linkTypes[types.UsesOptions] = ImmutableArray.Create(anyLink);
        linkTypes[types.UsesConfiguration] = ImmutableArray.Create(anyLink);
        linkTypes[types.ReadsFrom] = ImmutableArray.Create(anyLink);
        linkTypes[types.Queries] = ImmutableArray.Create(anyLink);
        linkTypes[types.Publishes] = ImmutableArray.Create(anyLink);
        linkTypes[types.PublishesNotification] = ImmutableArray.Create(anyLink);
        linkTypes[types.PublishesDomainEvent] = ImmutableArray.Create(anyLink);
        linkTypes[types.RequestProcessorDispatch] = ImmutableArray.Create(anyLink);
        linkTypes[types.GeneratedFrom] = ImmutableArray.Create(anyLink);
        linkTypes[types.Implements] = ImmutableArray.Create(anyLink);
        linkTypes[types.InvokesDomain] = ImmutableArray.Create(anyLink);
        linkTypes[types.Logs] = ImmutableArray.Create(anyLink);
        linkTypes[types.Validates] = ImmutableArray.Create(anyLink);
        linkTypes[types.ValidationLink] = ImmutableArray.Create(anyLink);
        linkTypes[types.ServiceLocated] = ImmutableArray.Create(anyLink);
        linkTypes[types.Returns] = ImmutableArray.Create(anyLink);
        linkTypes[types.CastsTo] = ImmutableArray.Create(anyLink);
        linkTypes[types.ConvertsTo] = ImmutableArray.Create(anyLink);
        linkTypes[types.Manages] = ImmutableArray.Create(anyLink);
        
        var elementAttributes = ImmutableDictionary.CreateBuilder<ElementType, ImmutableArray<string>>();
        
        // Define attributes for different element types
        var httpAttributes = ImmutableArray.Create(
            ProclaimerAttributeNames.Verb,
            ProclaimerAttributeNames.Route,
            ProclaimerAttributeNames.BaseUrl
        );
        
        var dataAttributes = ImmutableArray.Create(
            ProclaimerAttributeNames.Entity,
            ProclaimerAttributeNames.Table
        );
        
        var configAttributes = ImmutableArray.Create(
            ProclaimerAttributeNames.ConfigKey
        );
        
        var commonAttributes = ImmutableArray.Create(
            ProclaimerAttributeNames.Provenance,
            ProclaimerAttributeNames.Confidence,
            ProclaimerAttributeNames.External
        );
        
        // Endpoints get HTTP attributes
        elementAttributes[types.EndpointController] = httpAttributes.AddRange(commonAttributes);
        elementAttributes[types.EndpointMinimalApi] = httpAttributes.AddRange(commonAttributes);
        elementAttributes[types.ControllerAction] = httpAttributes.AddRange(commonAttributes);
        
        // HTTP client
        elementAttributes[types.HttpClient] = httpAttributes.AddRange(commonAttributes)
            .Add(ProclaimerAttributeNames.ClientMethod);
        
        // Data access
        elementAttributes[types.EfDbContext] = dataAttributes.AddRange(commonAttributes);
        elementAttributes[types.EfEntity] = dataAttributes.AddRange(commonAttributes);
        elementAttributes[types.DbTable] = dataAttributes.AddRange(commonAttributes);
        elementAttributes[types.Repository] = dataAttributes.AddRange(commonAttributes);
        
        // Messaging
        elementAttributes[types.MessagePublisher] = commonAttributes.Add(ProclaimerAttributeNames.Contract);
        elementAttributes[types.MessageContract] = commonAttributes.Add(ProclaimerAttributeNames.Contract);
        
        // Configuration
        elementAttributes[types.ConfigValue] = configAttributes.AddRange(commonAttributes);
        elementAttributes[types.Options] = configAttributes.AddRange(commonAttributes);
        elementAttributes[types.OptionsPoco] = configAttributes.AddRange(commonAttributes);
        
        // All other types get common attributes
        foreach (var elementType in elementTypes)
        {
            if (!elementAttributes.ContainsKey(elementType))
            {
                elementAttributes[elementType] = commonAttributes;
            }
        }
        
        // Root element types (endpoints are the primary entry points)
        var rootElementTypes = ImmutableArray.Create(
            types.EndpointController,
            types.EndpointMinimalApi,
            types.BackgroundService
        );
        
        return new SliceSchema(
            elementTypes,
            linkTypes.ToImmutable(),
            elementAttributes.ToImmutable(),
            rootElementTypes,
            HierarchyLinkType: null // Proclaimer doesn't use hierarchical links like DotNet's Contains
        );
    }
}
