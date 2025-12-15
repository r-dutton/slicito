using System.Collections.Generic;
using System.Collections.Immutable;
using Slicito.Abstractions;
using Slicito.Abstractions.Facts;

namespace Slicito.Proclaimer;

/// <summary>
/// Analyzes and computes flows through a Proclaimer slice using canonical link types.
/// </summary>
public class ProclaimerFlowService
{
    private readonly ISlice _slice;
    private readonly ProclaimerTypes _types;

    public ProclaimerFlowService(ISlice slice, ProclaimerTypes types)
    {
        _slice = slice;
        _types = types;
    }

    /// <summary>
    /// Computes the flow starting from a given element (typically an endpoint).
    /// </summary>
    public async Task<FlowNode> ComputeFlowAsync(ElementId rootElementId, int maxDepth = 10)
    {
        var visited = new HashSet<ElementId>();
        return await ComputeFlowRecursiveAsync(rootElementId, visited, 0, maxDepth);
    }

    private async Task<FlowNode> ComputeFlowRecursiveAsync(ElementId elementId, HashSet<ElementId> visited, int currentDepth, int maxDepth)
    {
        var elementType = _slice.GetElementType(elementId);
        var attributes = await GetElementAttributesAsync(elementId);
        var nodeType = DetermineNodeType(elementType);

        if (visited.Contains(elementId) || currentDepth >= maxDepth)
        {
            return FlowNode.Create(elementId, nodeType, attributes);
        }

        visited.Add(elementId);

        var children = await GetFlowChildrenAsync(elementId, visited, currentDepth, maxDepth);
        return new FlowNode(elementId, nodeType, attributes, children);
    }

    private async Task<ImmutableArray<FlowNode>> GetFlowChildrenAsync(ElementId elementId, HashSet<ElementId> visited, int currentDepth, int maxDepth)
    {
        var childrenBuilder = ImmutableArray.CreateBuilder<FlowNode>();

        var linkTypesToFollow = new[]
        {
            _types.Calls,
            _types.SendsHttpRequest,
            _types.WritesTo,
            _types.ReadsFrom,
            _types.PublishesTo,
            _types.ConsumesFrom,
        };

        foreach (var linkType in linkTypesToFollow)
        {
            var linkExplorer = _slice.GetLinkExplorer(linkType);
            var targets = await linkExplorer.GetTargetElementsAsync(elementId);

            foreach (var target in targets)
            {
                var childNode = await ComputeFlowRecursiveAsync(target.Id, visited, currentDepth + 1, maxDepth);
                childrenBuilder.Add(childNode);
            }
        }

        return childrenBuilder.ToImmutable();
    }

    private async Task<ImmutableDictionary<string, string>> GetElementAttributesAsync(ElementId elementId)
    {
        var attributesBuilder = ImmutableDictionary.CreateBuilder<string, string>();

        var attributeNames = new[]
        {
            ProclaimerAttributeNames.ServiceName,
            ProclaimerAttributeNames.HttpMethod,
            ProclaimerAttributeNames.Route,
            ProclaimerAttributeNames.UrlTemplate,
            ProclaimerAttributeNames.RepositoryName,
            ProclaimerAttributeNames.DatabaseName,
            ProclaimerAttributeNames.QueueName,
            ProclaimerAttributeNames.TopicName,
            ProclaimerAttributeNames.MessageType,
            ProclaimerAttributeNames.Provenance,
            ProclaimerAttributeNames.Confidence
        };

        foreach (var attrName in attributeNames)
        {
            try
            {
                var provider = _slice.GetElementAttributeProviderAsyncCallback(attrName);
                var value = await provider(elementId);
                if (!string.IsNullOrEmpty(value))
                {
                    attributesBuilder[attrName] = value;
                }
            }
            catch
            {
                // Attribute not available for this element type
            }
        }

        return attributesBuilder.ToImmutable();
    }

    private string DetermineNodeType(ElementType elementType)
    {
        if (elementType.Value.IsSubsetOfOrEquals(_types.Endpoint.Value))
            return "Endpoint";
        if (elementType.Value.IsSubsetOfOrEquals(_types.HttpClient.Value))
            return "HttpClient";
        if (elementType.Value.IsSubsetOfOrEquals(_types.Repository.Value))
            return "Repository";
        if (elementType.Value.IsSubsetOfOrEquals(_types.Database.Value))
            return "Database";
        if (elementType.Value.IsSubsetOfOrEquals(_types.Queue.Value))
            return "Queue";
        if (elementType.Value.IsSubsetOfOrEquals(_types.Topic.Value))
            return "Topic";
        if (elementType.Value.IsSubsetOfOrEquals(_types.BackgroundService.Value))
            return "BackgroundService";
        if (elementType.Value.IsSubsetOfOrEquals(_types.Service.Value))
            return "Service";

        return "Unknown";
    }
}
