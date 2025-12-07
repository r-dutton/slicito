using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Slicito.Abstractions;
using Slicito.Abstractions.Facts;
using Slicito.Common.Implementation;
using Slicito.DotNet;

namespace Slicito.Proclaimer.Analyzers.Advanced;

public record PipelineOperationAnalysisResult(
    ImmutableArray<PipelineBehaviorInvocation> BehaviorInvocations,
    ImmutableArray<RequestPreProcessorInvocation> PreProcessors,
    ImmutableArray<RequestPostProcessorInvocation> PostProcessors);

public record PipelineBehaviorInvocation(
    ElementId MethodId,
    ElementId OperationId,
    ITypeSymbol BehaviorType,
    int LineNumber);

public record RequestPreProcessorInvocation(
    ElementId MethodId,
    ElementId OperationId,
    ITypeSymbol RequestType,
    int LineNumber);

public record RequestPostProcessorInvocation(
    ElementId MethodId,
    ElementId OperationId,
    ITypeSymbol RequestType,
    int LineNumber);

public class PipelineOperationAnalyzer
{
    private readonly DotNetSolutionContext _dotnetContext;
    private readonly DotNetTypes _dotnetTypes;

    public PipelineOperationAnalyzer(DotNetSolutionContext dotnetContext, DotNetTypes dotnetTypes)
    {
        _dotnetContext = dotnetContext;
        _dotnetTypes = dotnetTypes;
    }

    public async Task<PipelineOperationAnalysisResult> AnalyzeMethodAsync(ElementId methodId)
    {
        var behaviors = ImmutableArray.CreateBuilder<PipelineBehaviorInvocation>();
        var preProcessors = ImmutableArray.CreateBuilder<RequestPreProcessorInvocation>();
        var postProcessors = ImmutableArray.CreateBuilder<RequestPostProcessorInvocation>();

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

            if (IsPipelineBehavior(methodSymbol))
            {
                var behaviorType = methodSymbol.ContainingType;
                if (behaviorType is not null)
                {
                    var lineNumber = GetLineNumber(operation.Id);
                    behaviors.Add(new PipelineBehaviorInvocation(
                        methodId, operation.Id, behaviorType, lineNumber));
                }
            }
            else if (IsPreProcessor(methodSymbol))
            {
                var requestType = GetProcessorRequestType(methodSymbol);
                if (requestType is not null)
                {
                    var lineNumber = GetLineNumber(operation.Id);
                    preProcessors.Add(new RequestPreProcessorInvocation(
                        methodId, operation.Id, requestType, lineNumber));
                }
            }
            else if (IsPostProcessor(methodSymbol))
            {
                var requestType = GetProcessorRequestType(methodSymbol);
                if (requestType is not null)
                {
                    var lineNumber = GetLineNumber(operation.Id);
                    postProcessors.Add(new RequestPostProcessorInvocation(
                        methodId, operation.Id, requestType, lineNumber));
                }
            }
        }

        return new PipelineOperationAnalysisResult(
            behaviors.ToImmutable(),
            preProcessors.ToImmutable(),
            postProcessors.ToImmutable());
    }

    private bool IsPipelineBehavior(IMethodSymbol method)
    {
        var containingType = method.ContainingType;
        if (containingType is null)
            return false;

        foreach (var iface in containingType.AllInterfaces)
        {
            if (iface.Name == "IPipelineBehavior" && 
                iface.ContainingNamespace?.ToDisplayString() == "MediatR")
            {
                return true;
            }
        }

        return false;
    }

    private bool IsPreProcessor(IMethodSymbol method)
    {
        var containingType = method.ContainingType;
        if (containingType is null)
            return false;

        foreach (var iface in containingType.AllInterfaces)
        {
            if (iface.Name == "IRequestPreProcessor" &&
                iface.ContainingNamespace?.ToDisplayString() == "MediatR.Pipeline")
            {
                return true;
            }
        }

        return false;
    }

    private bool IsPostProcessor(IMethodSymbol method)
    {
        var containingType = method.ContainingType;
        if (containingType is null)
            return false;

        foreach (var iface in containingType.AllInterfaces)
        {
            if (iface.Name == "IRequestPostProcessor" &&
                iface.ContainingNamespace?.ToDisplayString() == "MediatR.Pipeline")
            {
                return true;
            }
        }

        return false;
    }

    private ITypeSymbol? GetProcessorRequestType(IMethodSymbol method)
    {
        var containingType = method.ContainingType;
        if (containingType is null)
            return null;

        foreach (var iface in containingType.AllInterfaces)
        {
            if ((iface.Name == "IRequestPreProcessor" || iface.Name == "IRequestPostProcessor") &&
                iface.TypeArguments.Length > 0)
            {
                return iface.TypeArguments[0];
            }
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
