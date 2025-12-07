using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Slicito.Abstractions;
using Slicito.Abstractions.Facts;
using Slicito.Common.Implementation;
using Slicito.DotNet;

namespace Slicito.Proclaimer.Analyzers.Advanced;

public record MappingOperationAnalysisResult(
    ImmutableArray<MapperInvocation> MapInvocations,
    ImmutableArray<CreateMapInvocation> CreateMapInvocations);

public record MapperInvocation(
    ElementId MethodId,
    ElementId OperationId,
    ITypeSymbol SourceType,
    ITypeSymbol DestinationType,
    int LineNumber);

public record CreateMapInvocation(
    ElementId MethodId,
    ElementId OperationId,
    ITypeSymbol SourceType,
    ITypeSymbol DestinationType,
    int LineNumber);

public class MappingOperationAnalyzer
{
    private readonly DotNetSolutionContext _dotnetContext;
    private readonly DotNetTypes _dotnetTypes;

    public MappingOperationAnalyzer(DotNetSolutionContext dotnetContext, DotNetTypes dotnetTypes)
    {
        _dotnetContext = dotnetContext;
        _dotnetTypes = dotnetTypes;
    }

    public async Task<MappingOperationAnalysisResult> AnalyzeMethodAsync(ElementId methodId)
    {
        var mapInvocations = ImmutableArray.CreateBuilder<MapperInvocation>();
        var createMapInvocations = ImmutableArray.CreateBuilder<CreateMapInvocation>();

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

            if (IsMapInvocation(methodSymbol))
            {
                var (sourceType, destType) = GetMapTypes(methodSymbol);
                if (sourceType is not null && destType is not null)
                {
                    var lineNumber = GetLineNumber(operation.Id);
                    mapInvocations.Add(new MapperInvocation(
                        methodId,
                        operation.Id,
                        sourceType,
                        destType,
                        lineNumber));
                }
            }
            else if (IsCreateMapInvocation(methodSymbol))
            {
                var (sourceType, destType) = GetCreateMapTypes(methodSymbol);
                if (sourceType is not null && destType is not null)
                {
                    var lineNumber = GetLineNumber(operation.Id);
                    createMapInvocations.Add(new CreateMapInvocation(
                        methodId,
                        operation.Id,
                        sourceType,
                        destType,
                        lineNumber));
                }
            }
        }

        return new MappingOperationAnalysisResult(
            mapInvocations.ToImmutable(),
            createMapInvocations.ToImmutable());
    }

    private bool IsMapInvocation(IMethodSymbol method)
    {
        if (!string.Equals(method.Name, "Map", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(method.Name, "ProjectTo", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var receiver = method.ReceiverType ?? method.ContainingType;
        if (receiver is null)
            return false;

        var display = receiver.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
        return display.Contains("IMapper", StringComparison.Ordinal) ||
               display.Contains("AutoMapper", StringComparison.Ordinal);
    }

    private bool IsCreateMapInvocation(IMethodSymbol method)
    {
        return string.Equals(method.Name, "CreateMap", StringComparison.OrdinalIgnoreCase) &&
               method.IsGenericMethod &&
               method.TypeArguments.Length >= 2;
    }

    private (ITypeSymbol? sourceType, ITypeSymbol? destType) GetMapTypes(IMethodSymbol method)
    {
        if (method.IsGenericMethod && method.TypeArguments.Length > 0)
        {
            var destType = method.TypeArguments[0];
            
            ITypeSymbol? sourceType = null;
            if (method.Parameters.Length > 0)
            {
                sourceType = method.Parameters[0].Type;
            }
            
            return (sourceType, destType);
        }

        if (method.TypeArguments.Length >= 2)
        {
            return (method.TypeArguments[0], method.TypeArguments[1]);
        }

        return (null, null);
    }

    private (ITypeSymbol? sourceType, ITypeSymbol? destType) GetCreateMapTypes(IMethodSymbol method)
    {
        if (method.TypeArguments.Length >= 2)
        {
            return (method.TypeArguments[0], method.TypeArguments[1]);
        }

        return (null, null);
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
