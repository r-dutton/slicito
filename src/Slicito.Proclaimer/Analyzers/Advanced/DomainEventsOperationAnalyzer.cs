using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Slicito.Abstractions;
using Slicito.Abstractions.Facts;
using Slicito.Common.Implementation;
using Slicito.DotNet;

namespace Slicito.Proclaimer.Analyzers.Advanced;

public record DomainEventsOperationAnalysisResult(
    ImmutableArray<DomainEventPublish> EventPublishes,
    ImmutableArray<DomainEventDispatch> EventDispatches);

public record DomainEventPublish(
    ElementId MethodId,
    ElementId OperationId,
    ITypeSymbol EventType,
    int LineNumber);

public record DomainEventDispatch(
    ElementId MethodId,
    ElementId OperationId,
    string DispatcherType,
    int LineNumber);

public class DomainEventsOperationAnalyzer
{
    private readonly DotNetSolutionContext _dotnetContext;
    private readonly DotNetTypes _dotnetTypes;

    public DomainEventsOperationAnalyzer(DotNetSolutionContext dotnetContext, DotNetTypes dotnetTypes)
    {
        _dotnetContext = dotnetContext;
        _dotnetTypes = dotnetTypes;
    }

    public async Task<DomainEventsOperationAnalysisResult> AnalyzeMethodAsync(ElementId methodId)
    {
        var publishes = ImmutableArray.CreateBuilder<DomainEventPublish>();
        var dispatches = ImmutableArray.CreateBuilder<DomainEventDispatch>();

        var procedureElement = new SimpleProcedureElement(methodId);
        var operations = await _dotnetContext.TypedSliceFragment.GetOperationsAsync(procedureElement);

        foreach (var operation in operations)
        {
            var operationType = _dotnetContext.Slice.GetElementType(operation.Id);
            if (!operationType.Value.IsSubsetOfOrEquals(_dotnetTypes.Call.Value))
                continue;

            var symbol = _dotnetContext.GetSymbol(operation.Id);
            if (symbol is not IMethodSymbol methodSymbol)
                continue;

            if (IsDomainEventPublish(methodSymbol))
            {
                var eventType = DetermineEventType(methodSymbol);
                if (eventType is not null)
                {
                    var lineNumber = GetLineNumber(operation.Id);
                    publishes.Add(new DomainEventPublish(methodId, operation.Id, eventType, lineNumber));
                }
            }
            else if (IsEventDispatcher(methodSymbol))
            {
                var dispatcherType = methodSymbol.ContainingType?.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat) ?? "";
                if (!string.IsNullOrEmpty(dispatcherType))
                {
                    var lineNumber = GetLineNumber(operation.Id);
                    dispatches.Add(new DomainEventDispatch(methodId, operation.Id, dispatcherType, lineNumber));
                }
            }
        }

        return new DomainEventsOperationAnalysisResult(publishes.ToImmutable(), dispatches.ToImmutable());
    }

    private bool IsDomainEventPublish(IMethodSymbol method)
    {
        return (method.Name == "Raise" || method.Name == "Publish" || method.Name == "Dispatch") &&
               IsDomainEventDispatcher(method.ContainingType);
    }

    private bool IsEventDispatcher(IMethodSymbol method)
    {
        return IsDomainEventDispatcher(method.ContainingType ?? method.ReceiverType);
    }

    private bool IsDomainEventDispatcher(ITypeSymbol? type)
    {
        var typeName = type?.Name ?? "";
        var display = type?.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat) ?? "";
        
        return typeName.Contains("EventDispatcher", StringComparison.OrdinalIgnoreCase) ||
               typeName.Contains("DomainEvents", StringComparison.OrdinalIgnoreCase) ||
               display.Contains("IDomainEvent", StringComparison.Ordinal);
    }

    private ITypeSymbol? DetermineEventType(IMethodSymbol method)
    {
        if (method.TypeArguments.Length > 0)
        {
            return method.TypeArguments[0];
        }

        if (method.Parameters.Length > 0)
        {
            return method.Parameters[0].Type;
        }

        return null;
    }

    private int GetLineNumber(ElementId operationId)
    {
        var symbol = _dotnetContext.GetSymbol(operationId);
        if (symbol?.Locations.FirstOrDefault() is { } location && location.IsInSource)
        {
            return location.GetLineSpan().StartLinePosition.Line + 1;
        }
        return 0;
    }

    private class SimpleProcedureElement : ElementBase, Slicito.DotNet.Facts.ICSharpProcedureElement
    {
        public SimpleProcedureElement(ElementId id) : base(id)
        {
        }

        public string Runtime => Slicito.DotNet.DotNetAttributeValues.Runtime.DotNet;
        public string Language => Slicito.DotNet.DotNetAttributeValues.Language.CSharp;
    }
}
