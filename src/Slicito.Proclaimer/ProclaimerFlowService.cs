using System.Collections.Immutable;
using Slicito.Abstractions;
using Slicito.Abstractions.Facts;

namespace Slicito.Proclaimer;

/// <summary>
/// Analyzes and computes flows through a Proclaimer slice,
/// traversing calls, HTTP requests, messaging, and data access links.
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
    /// Computes the flow starting from a given endpoint element.
    /// </summary>
    /// <param name="rootElementId">The starting element (typically an endpoint)</param>
    /// <param name="maxDepth">Maximum depth to traverse (default 10)</param>
    /// <returns>Flow tree rooted at the given element</returns>
    public async Task<FlowNode> ComputeFlowAsync(ElementId rootElementId, int maxDepth = 10)
    {
        var visited = new HashSet<ElementId>();
        return await ComputeFlowRecursiveAsync(rootElementId, visited, 0, maxDepth);
    }
    
    private async Task<FlowNode> ComputeFlowRecursiveAsync(
        ElementId elementId,
        HashSet<ElementId> visited,
        int currentDepth,
        int maxDepth)
    {
        // Get element type
        var elementType = _slice.GetElementType(elementId);
        
        // Extract attributes
        var attributes = await GetElementAttributesAsync(elementId);
        
        // Determine node type from element type
        var nodeType = DetermineNodeType(elementType);
        
        // Prevent infinite recursion
        if (visited.Contains(elementId) || currentDepth >= maxDepth)
        {
            return FlowNode.Create(elementId, nodeType, attributes);
        }
        
        visited.Add(elementId);
        
        // Get all outgoing links for flow traversal
        var children = await GetFlowChildrenAsync(elementId, visited, currentDepth, maxDepth);
        
        return new FlowNode(elementId, nodeType, attributes, children);
    }
    
    private async Task<ImmutableArray<FlowNode>> GetFlowChildrenAsync(
        ElementId elementId,
        HashSet<ElementId> visited,
        int currentDepth,
        int maxDepth)
    {
        var childrenBuilder = ImmutableArray.CreateBuilder<FlowNode>();
        
        // Traverse different link types
        var linkTypesToFollow = new[]
        {
            _types.Calls,
            _types.SendsRequest,
            _types.HandledBy,
            _types.ProcessedBy,
            _types.UsesClient,
            _types.UsesService,
            _types.UsesStorage,
            _types.Publishes,
            _types.Queries
        };
        
        foreach (var linkType in linkTypesToFollow)
        {
            var linkExplorer = _slice.GetLinkExplorer(linkType);
            var targets = await linkExplorer.GetTargetElementsAsync(elementId);
            
            foreach (var target in targets)
            {
                var childNode = await ComputeFlowRecursiveAsync(
                    target.Id,
                    visited,
                    currentDepth + 1,
                    maxDepth);
                
                childrenBuilder.Add(childNode);
            }
        }
        
        return childrenBuilder.ToImmutable();
    }
    
    private async Task<ImmutableDictionary<string, string>> GetElementAttributesAsync(ElementId elementId)
    {
        var attributesBuilder = ImmutableDictionary.CreateBuilder<string, string>();
        
        // Try to get common attributes
        var attributeNames = new[]
        {
            ProclaimerAttributeNames.Verb,
            ProclaimerAttributeNames.Route,
            ProclaimerAttributeNames.BaseUrl,
            ProclaimerAttributeNames.Entity,
            ProclaimerAttributeNames.Table,
            ProclaimerAttributeNames.Contract,
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
        // Map element type to node type string
        // This is a simplified mapping - in practice would check which type it is
        if (elementType.Value.IsSubsetOfOrEquals(_types.EndpointController.Value))
            return "Endpoint";
        if (elementType.Value.IsSubsetOfOrEquals(_types.HttpClient.Value))
            return "HttpClient";
        if (elementType.Value.IsSubsetOfOrEquals(_types.CqrsHandler.Value))
            return "CqrsHandler";
        if (elementType.Value.IsSubsetOfOrEquals(_types.Repository.Value))
            return "Repository";
        if (elementType.Value.IsSubsetOfOrEquals(_types.EfDbContext.Value))
            return "DbContext";
        if (elementType.Value.IsSubsetOfOrEquals(_types.MessagePublisher.Value))
            return "MessagePublisher";
        if (elementType.Value.IsSubsetOfOrEquals(_types.BackgroundService.Value))
            return "BackgroundService";
        
        return "Unknown";
    }
}
