using System.Collections.Immutable;
using Slicito.Abstractions;

namespace Slicito.Proclaimer;

/// <summary>
/// Represents a node in a Proclaimer flow - an element and its relationships.
/// </summary>
public record FlowNode(
    ElementId ElementId,
    string NodeType,
    ImmutableDictionary<string, string> Attributes,
    ImmutableArray<FlowNode> Children)
{
    public static FlowNode Create(
        ElementId elementId,
        string nodeType,
        ImmutableDictionary<string, string>? attributes = null,
        ImmutableArray<FlowNode>? children = null)
    {
        return new FlowNode(
            elementId,
            nodeType,
            attributes ?? ImmutableDictionary<string, string>.Empty,
            children ?? ImmutableArray<FlowNode>.Empty);
    }
}
