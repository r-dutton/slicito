using System.Collections.Immutable;
using Slicito.Abstractions;
using Slicito.Abstractions.Models;

namespace Slicito.Proclaimer;

/// <summary>
/// Builds graph models from Proclaimer flow nodes for visualization.
/// </summary>
public class ProclaimerFlowGraphBuilder
{
    private readonly ProclaimerLabelProvider _labelProvider;
    
    public ProclaimerFlowGraphBuilder(ProclaimerLabelProvider labelProvider)
    {
        _labelProvider = labelProvider;
    }
    
    /// <summary>
    /// Builds a graph model from a flow node tree.
    /// </summary>
    public async Task<Graph> BuildGraphAsync(FlowNode rootNode)
    {
        var nodes = ImmutableArray.CreateBuilder<Node>();
        var edges = ImmutableArray.CreateBuilder<Edge>();
        var visited = new HashSet<string>();
        
        await BuildGraphRecursiveAsync(rootNode, nodes, edges, visited);
        
        return new Graph(nodes.ToImmutable(), edges.ToImmutable());
    }
    
    private async Task BuildGraphRecursiveAsync(
        FlowNode flowNode,
        ImmutableArray<Node>.Builder nodes,
        ImmutableArray<Edge>.Builder edges,
        HashSet<string> visited)
    {
        var nodeId = flowNode.ElementId.Value;
        
        // Avoid duplicates
        if (visited.Contains(nodeId))
        {
            return;
        }
        
        visited.Add(nodeId);
        
        // Create node with label
        var label = await _labelProvider.GetElementLabelAsync(flowNode.ElementId);
        var clickCommand = CreateNavigateCommand(flowNode.ElementId);
        
        nodes.Add(new Node(nodeId, label, clickCommand));
        
        // Process children and create edges
        foreach (var child in flowNode.Children)
        {
            // Add child node
            await BuildGraphRecursiveAsync(child, nodes, edges, visited);
            
            // Add edge from this node to child
            var edgeLabel = flowNode.NodeType switch
            {
                "Endpoint" => "calls",
                "HttpClient" => "sends request",
                "CqrsHandler" => "handles",
                "Repository" => "queries",
                "MessagePublisher" => "publishes",
                _ => null
            };
            
            edges.Add(new Edge(nodeId, child.ElementId.Value, edgeLabel, null));
        }
    }
    
    private static Command CreateNavigateCommand(ElementId elementId)
    {
        return new Command(
            "NavigateTo",
            ImmutableDictionary<string, string>.Empty.Add("Id", elementId.Value));
    }
}
