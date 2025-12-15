using System;
using Slicito.Abstractions;
using Slicito.Abstractions.Facts;
using Slicito.ProgramAnalysis;

namespace Slicito.Proclaimer;

/// <summary>
/// Provides strongly-typed access to canonical Proclaimer element and link types.
/// </summary>
public class ProclaimerTypes : IProgramTypes
{
    private readonly ITypeSystem _typeSystem;

    public ProclaimerTypes(ITypeSystem typeSystem)
    {
        _typeSystem = typeSystem ?? throw new ArgumentNullException(nameof(typeSystem));

        Service = GetElementType(ProclaimerAttributeValues.Kind.Service);
        Endpoint = GetElementType(ProclaimerAttributeValues.Kind.Endpoint);
        HttpClient = GetElementType(ProclaimerAttributeValues.Kind.HttpClient);
        Repository = GetElementType(ProclaimerAttributeValues.Kind.Repository);
        Database = GetElementType(ProclaimerAttributeValues.Kind.Database);
        Queue = GetElementType(ProclaimerAttributeValues.Kind.Queue);
        Topic = GetElementType(ProclaimerAttributeValues.Kind.Topic);
        BackgroundService = GetElementType(ProclaimerAttributeValues.Kind.BackgroundService);

        Contains = _typeSystem.GetLinkType([(CommonAttributeNames.Kind, CommonAttributeValues.Kind.Contains)]);
        BelongsToService = GetLinkType(ProclaimerAttributeValues.LinkKind.BelongsToService);
        Calls = GetLinkType(ProclaimerAttributeValues.LinkKind.Calls);
        SendsHttpRequest = GetLinkType(ProclaimerAttributeValues.LinkKind.SendsHttpRequest);
        WritesTo = GetLinkType(ProclaimerAttributeValues.LinkKind.WritesTo);
        ReadsFrom = GetLinkType(ProclaimerAttributeValues.LinkKind.ReadsFrom);
        PublishesTo = GetLinkType(ProclaimerAttributeValues.LinkKind.PublishesTo);
        ConsumesFrom = GetLinkType(ProclaimerAttributeValues.LinkKind.ConsumesFrom);
    }

    // Element types
    public ElementType Service { get; }
    public ElementType Endpoint { get; }
    public ElementType HttpClient { get; }
    public ElementType Repository { get; }
    public ElementType Database { get; }
    public ElementType Queue { get; }
    public ElementType Topic { get; }
    public ElementType BackgroundService { get; }

    // Link types
    public LinkType Contains { get; } // For compatibility with IProgramTypes
    public LinkType BelongsToService { get; }
    public LinkType Calls { get; }
    public LinkType SendsHttpRequest { get; }
    public LinkType WritesTo { get; }
    public LinkType ReadsFrom { get; }
    public LinkType PublishesTo { get; }
    public LinkType ConsumesFrom { get; }

    // IProgramTypes implementation (for compatibility with Slicito's program analysis)
    ElementType IProgramTypes.Procedure => Endpoint | BackgroundService;
    ElementType IProgramTypes.NestedProcedures => _typeSystem.GetElementType([(ProclaimerAttributeNames.Kind, "__proclaimer.none.nested__")]);
    ElementType IProgramTypes.Operation => Endpoint | HttpClient | Repository;
    ElementType IProgramTypes.Call => HttpClient | Repository;

    bool IProgramTypes.HasName(ElementType elementType) =>
        elementType.Value.TryGetIntersection((Service | Endpoint | HttpClient | Repository | Database | Queue | Topic | BackgroundService).Value) is not null;

    public bool HasCodeLocation(ElementType elementType) =>
        elementType.Value.TryGetIntersection((Endpoint | HttpClient | Repository | BackgroundService).Value) is not null;

    private ElementType GetElementType(string kind) =>
        _typeSystem.GetElementType([(ProclaimerAttributeNames.Kind, kind)]);

    private LinkType GetLinkType(string kind) =>
        _typeSystem.GetLinkType([(ProclaimerAttributeNames.Kind, kind)]);
}
