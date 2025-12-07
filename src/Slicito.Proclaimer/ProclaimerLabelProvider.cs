using System.Collections.Immutable;
using Slicito.Abstractions;
using Slicito.Abstractions.Facts;

namespace Slicito.Proclaimer;

/// <summary>
/// Provides human-readable labels for Proclaimer elements and links.
/// </summary>
public class ProclaimerLabelProvider
{
    private readonly ISlice _slice;
    private readonly ProclaimerTypes _types;
    
    public ProclaimerLabelProvider(ISlice slice, ProclaimerTypes types)
    {
        _slice = slice;
        _types = types;
    }
    
    /// <summary>
    /// Gets a label for an element.
    /// </summary>
    public async Task<string> GetElementLabelAsync(ElementId elementId)
    {
        var elementType = _slice.GetElementType(elementId);
        
        // Get attributes for labeling
        var verb = await TryGetAttributeAsync(elementId, ProclaimerAttributeNames.Verb);
        var route = await TryGetAttributeAsync(elementId, ProclaimerAttributeNames.Route);
        var entity = await TryGetAttributeAsync(elementId, ProclaimerAttributeNames.Entity);
        var table = await TryGetAttributeAsync(elementId, ProclaimerAttributeNames.Table);
        var contract = await TryGetAttributeAsync(elementId, ProclaimerAttributeNames.Contract);
        
        // Create label based on element type
        if (elementType.Value.IsSubsetOfOrEquals(_types.EndpointController.Value))
        {
            return FormatEndpointLabel(verb, route);
        }
        if (elementType.Value.IsSubsetOfOrEquals(_types.HttpClient.Value))
        {
            return FormatHttpClientLabel(verb, route);
        }
        if (elementType.Value.IsSubsetOfOrEquals(_types.Repository.Value))
        {
            return $"Repository: {entity ?? table ?? "Unknown"}";
        }
        if (elementType.Value.IsSubsetOfOrEquals(_types.EfDbContext.Value))
        {
            return $"DbContext: {entity ?? "Unknown"}";
        }
        if (elementType.Value.IsSubsetOfOrEquals(_types.DbTable.Value))
        {
            return $"Table: {table ?? "Unknown"}";
        }
        if (elementType.Value.IsSubsetOfOrEquals(_types.MessagePublisher.Value))
        {
            return $"Publisher: {contract ?? "Unknown"}";
        }
        if (elementType.Value.IsSubsetOfOrEquals(_types.MessageContract.Value))
        {
            return $"Message: {contract ?? "Unknown"}";
        }
        if (elementType.Value.IsSubsetOfOrEquals(_types.CqrsHandler.Value))
        {
            return "CQRS Handler";
        }
        if (elementType.Value.IsSubsetOfOrEquals(_types.BackgroundService.Value))
        {
            return "Background Service";
        }
        
        return elementId.Value;
    }
    
    /// <summary>
    /// Gets a label for a link type.
    /// </summary>
    public string GetLinkLabel(LinkType linkType)
    {
        if (linkType.Value.Equals(_types.Calls.Value))
            return "calls";
        if (linkType.Value.Equals(_types.SendsRequest.Value))
            return "sends HTTP request";
        if (linkType.Value.Equals(_types.HandledBy.Value))
            return "handled by";
        if (linkType.Value.Equals(_types.ProcessedBy.Value))
            return "processed by";
        if (linkType.Value.Equals(_types.UsesClient.Value))
            return "uses client";
        if (linkType.Value.Equals(_types.UsesService.Value))
            return "uses service";
        if (linkType.Value.Equals(_types.UsesStorage.Value))
            return "uses storage";
        if (linkType.Value.Equals(_types.Publishes.Value))
            return "publishes";
        if (linkType.Value.Equals(_types.PublishesNotification.Value))
            return "publishes notification";
        if (linkType.Value.Equals(_types.Queries.Value))
            return "queries";
        if (linkType.Value.Equals(_types.ReadsFrom.Value))
            return "reads from";
        if (linkType.Value.Equals(_types.MapsTo.Value))
            return "maps to";
        
        return "links to";
    }
    
    private string FormatEndpointLabel(string? verb, string? route)
    {
        if (!string.IsNullOrEmpty(verb) && !string.IsNullOrEmpty(route))
        {
            return $"{verb} {route}";
        }
        if (!string.IsNullOrEmpty(route))
        {
            return route!;
        }
        return "Endpoint";
    }
    
    private string FormatHttpClientLabel(string? verb, string? route)
    {
        if (!string.IsNullOrEmpty(verb) && !string.IsNullOrEmpty(route))
        {
            return $"HTTP {verb} → {route}";
        }
        if (!string.IsNullOrEmpty(route))
        {
            return $"HTTP → {route}";
        }
        return "HTTP Client";
    }
    
    private async Task<string?> TryGetAttributeAsync(ElementId elementId, string attributeName)
    {
        try
        {
            var provider = _slice.GetElementAttributeProviderAsyncCallback(attributeName);
            var value = await provider(elementId);
            return string.IsNullOrEmpty(value) ? null : value;
        }
        catch
        {
            return null;
        }
    }
}
