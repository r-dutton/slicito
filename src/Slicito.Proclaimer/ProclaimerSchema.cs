using System.Collections.Immutable;
using Slicito.Abstractions;

namespace Slicito.Proclaimer;

/// <summary>
/// Extension methods for building the canonical Proclaimer schema.
/// </summary>
public static class ProclaimerSchema
{
    /// <summary>
    /// Registers Proclaimer element types, link types, and attributes into a slice schema.
    /// </summary>
    public static SliceSchema AddProclaimerSchema(ProclaimerTypes types)
    {
        var elementTypes = ImmutableArray.Create(
            types.Service,
            types.Endpoint,
            types.HttpClient,
            types.Repository,
            types.Database,
            types.Queue,
            types.Topic,
            types.BackgroundService);

        var linkTypes = ImmutableDictionary.CreateBuilder<LinkType, ImmutableArray<LinkElementTypes>>();

        var anyElement = elementTypes.Aggregate((a, b) => a | b);

        // Service ownership
        linkTypes[types.BelongsToService] = ImmutableArray.Create(
            new LinkElementTypes(types.Endpoint | types.HttpClient | types.Repository | types.Database | types.Queue | types.Topic | types.BackgroundService, types.Service));

        // Calls & HTTP
        linkTypes[types.Calls] = ImmutableArray.Create(new LinkElementTypes(anyElement, anyElement));
        linkTypes[types.SendsHttpRequest] = ImmutableArray.Create(new LinkElementTypes(anyElement, types.HttpClient));

        // Data access
        linkTypes[types.WritesTo] = ImmutableArray.Create(
            new LinkElementTypes(types.Repository | types.Endpoint | types.BackgroundService, types.Database));
        linkTypes[types.ReadsFrom] = ImmutableArray.Create(
            new LinkElementTypes(types.Repository | types.Endpoint | types.BackgroundService, types.Database));

        // Messaging
        linkTypes[types.PublishesTo] = ImmutableArray.Create(
            new LinkElementTypes(types.Endpoint | types.BackgroundService, types.Queue | types.Topic));
        linkTypes[types.ConsumesFrom] = ImmutableArray.Create(
            new LinkElementTypes(types.Endpoint | types.BackgroundService, types.Queue | types.Topic));

        var elementAttributes = ImmutableDictionary.CreateBuilder<ElementType, ImmutableArray<string>>();

        elementAttributes[types.Service] = ImmutableArray.Create(
            ProclaimerAttributeNames.ServiceName,
            ProclaimerAttributeNames.Provenance,
            ProclaimerAttributeNames.External);

        elementAttributes[types.Endpoint] = ImmutableArray.Create(
            ProclaimerAttributeNames.ServiceName,
            ProclaimerAttributeNames.HttpMethod,
            ProclaimerAttributeNames.Route,
            CommonAttributeNames.CodeLocation,
            ProclaimerAttributeNames.Provenance,
            ProclaimerAttributeNames.Confidence);

        elementAttributes[types.HttpClient] = ImmutableArray.Create(
            ProclaimerAttributeNames.ServiceName,
            ProclaimerAttributeNames.HttpMethod,
            ProclaimerAttributeNames.UrlTemplate,
            ProclaimerAttributeNames.TargetService,
            ProclaimerAttributeNames.Provenance,
            ProclaimerAttributeNames.Confidence,
            ProclaimerAttributeNames.External);

        elementAttributes[types.Repository] = ImmutableArray.Create(
            ProclaimerAttributeNames.ServiceName,
            ProclaimerAttributeNames.RepositoryName,
            ProclaimerAttributeNames.Provenance);

        elementAttributes[types.Database] = ImmutableArray.Create(
            ProclaimerAttributeNames.DatabaseName,
            ProclaimerAttributeNames.Provenance);

        elementAttributes[types.Queue] = ImmutableArray.Create(
            ProclaimerAttributeNames.QueueName,
            ProclaimerAttributeNames.MessageType,
            ProclaimerAttributeNames.Provenance);

        elementAttributes[types.Topic] = ImmutableArray.Create(
            ProclaimerAttributeNames.TopicName,
            ProclaimerAttributeNames.MessageType,
            ProclaimerAttributeNames.Provenance);

        elementAttributes[types.BackgroundService] = ImmutableArray.Create(
            ProclaimerAttributeNames.ServiceName,
            CommonAttributeNames.CodeLocation,
            ProclaimerAttributeNames.Provenance,
            ProclaimerAttributeNames.Confidence);

        var rootElementTypes = ImmutableArray.Create(
            types.Endpoint,
            types.BackgroundService);

        return new SliceSchema(
            elementTypes,
            linkTypes.ToImmutable(),
            elementAttributes.ToImmutable(),
            rootElementTypes,
            HierarchyLinkType: null);
    }
}
