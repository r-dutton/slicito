using System.Collections.Immutable;
using System.Linq;
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

        var serviceName = await TryGetAttributeAsync(elementId, ProclaimerAttributeNames.ServiceName);
        var httpMethod = await TryGetAttributeAsync(elementId, ProclaimerAttributeNames.HttpMethod);
        var route = await TryGetAttributeAsync(elementId, ProclaimerAttributeNames.Route);
        var url = await TryGetAttributeAsync(elementId, ProclaimerAttributeNames.UrlTemplate);
        var repository = await TryGetAttributeAsync(elementId, ProclaimerAttributeNames.RepositoryName);
        var database = await TryGetAttributeAsync(elementId, ProclaimerAttributeNames.DatabaseName);
        var queue = await TryGetAttributeAsync(elementId, ProclaimerAttributeNames.QueueName);
        var topic = await TryGetAttributeAsync(elementId, ProclaimerAttributeNames.TopicName);

        if (elementType.Value.IsSubsetOfOrEquals(_types.Endpoint.Value))
        {
            return FormatEndpointLabel(serviceName, httpMethod, route);
        }

        if (elementType.Value.IsSubsetOfOrEquals(_types.HttpClient.Value))
        {
            return FormatHttpClientLabel(serviceName, httpMethod, url);
        }

        if (elementType.Value.IsSubsetOfOrEquals(_types.Repository.Value))
        {
            return $"Repository: {repository ?? "(unknown)"}";
        }

        if (elementType.Value.IsSubsetOfOrEquals(_types.Database.Value))
        {
            return $"Database: {database ?? "(unknown)"}";
        }

        if (elementType.Value.IsSubsetOfOrEquals(_types.Queue.Value))
        {
            return $"Queue: {queue ?? "(unknown)"}";
        }

        if (elementType.Value.IsSubsetOfOrEquals(_types.Topic.Value))
        {
            return $"Topic: {topic ?? "(unknown)"}";
        }

        if (elementType.Value.IsSubsetOfOrEquals(_types.BackgroundService.Value))
        {
            return $"Background Service: {serviceName ?? elementId.Value}";
        }

        if (elementType.Value.IsSubsetOfOrEquals(_types.Service.Value))
        {
            return serviceName ?? "Service";
        }

        return elementId.Value;
    }

    /// <summary>
    /// Gets a label for a link type.
    /// </summary>
    public string GetLinkLabel(LinkType linkType)
    {
        if (linkType.Value.Equals(_types.BelongsToService.Value))
            return "belongs to";
        if (linkType.Value.Equals(_types.Calls.Value))
            return "calls";
        if (linkType.Value.Equals(_types.SendsHttpRequest.Value))
            return "sends HTTP request";
        if (linkType.Value.Equals(_types.WritesTo.Value))
            return "writes to";
        if (linkType.Value.Equals(_types.ReadsFrom.Value))
            return "reads from";
        if (linkType.Value.Equals(_types.PublishesTo.Value))
            return "publishes to";
        if (linkType.Value.Equals(_types.ConsumesFrom.Value))
            return "consumes from";

        return "links to";
    }

    private static string FormatEndpointLabel(string? serviceName, string? verb, string? route)
    {
        var routePart = string.IsNullOrEmpty(route) ? "/" : route;
        var servicePart = string.IsNullOrEmpty(serviceName) ? "Endpoint" : serviceName;
        var verbPart = string.IsNullOrEmpty(verb) ? string.Empty : verb;

        return string.Join(" ", new[] { servicePart, verbPart, routePart }.Where(p => !string.IsNullOrEmpty(p)));
    }

    private static string FormatHttpClientLabel(string? serviceName, string? verb, string? url)
    {
        var servicePart = string.IsNullOrEmpty(serviceName) ? "HTTP" : serviceName;
        var verbPart = string.IsNullOrEmpty(verb) ? "HTTP" : verb;
        var target = string.IsNullOrEmpty(url) ? "client" : url;

        return $"{servicePart} {verbPart} → {target}".Trim();
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
