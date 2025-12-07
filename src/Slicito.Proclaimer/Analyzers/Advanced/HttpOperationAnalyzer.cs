using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Slicito.Abstractions;
using Slicito.Abstractions.Facts;
using Slicito.Common.Implementation;
using Slicito.DotNet;

namespace Slicito.Proclaimer.Analyzers.Advanced;

/// <summary>
/// Result of analyzing operations in a method for HTTP client usage.
/// </summary>
public record HttpOperationAnalysisResult(
    ImmutableArray<HttpClientInvocation> HttpCalls);

/// <summary>
/// Represents an HTTP client invocation discovered in operations.
/// </summary>
public record HttpClientInvocation(
    ElementId MethodId,
    ElementId OperationId,
    string HttpVerb,
    string? Route,
    int LineNumber);

/// <summary>
/// Analyzes IOperation trees to detect HTTP client patterns.
/// Extends existing HttpClientAnalyzer with route extraction and URL tracking.
/// Ported from TheProclaimer's HttpOperationVisitor.
/// 
/// LIMITATIONS vs TheProclaimer:
/// - Does not use FlowValueContentFacade for route string literal extraction
/// - Route resolution is basic (no string interpolation support)
/// - Query parameter extraction not implemented (requires value content analysis)
/// - HttpRequestMessage construction tracking not implemented
/// 
/// TODO: Add TryResolveRoute() method using value content analysis
/// TODO: Add ExtractQueryParameters() for URL analysis
/// TODO: Add HttpRequestMessage pattern detection
/// </summary>
public class HttpOperationAnalyzer
{
    private readonly DotNetSolutionContext _dotnetContext;
    private readonly DotNetTypes _dotnetTypes;

    public HttpOperationAnalyzer(DotNetSolutionContext dotnetContext, DotNetTypes dotnetTypes)
    {
        _dotnetContext = dotnetContext;
        _dotnetTypes = dotnetTypes;
    }

    /// <summary>
    /// Analyzes all operations in a method to detect HTTP client calls.
    /// </summary>
    public async Task<HttpOperationAnalysisResult> AnalyzeMethodAsync(ElementId methodId)
    {
        var httpCalls = ImmutableArray.CreateBuilder<HttpClientInvocation>();

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

            if (IsHttpClientCall(methodSymbol, out var httpVerb))
            {
                // TODO: Extract route from arguments using value content analysis
                var route = TryExtractRoute(methodSymbol);
                var lineNumber = GetLineNumber(operation.Id);
                httpCalls.Add(new HttpClientInvocation(methodId, operation.Id, httpVerb, route, lineNumber));
            }
        }

        return new HttpOperationAnalysisResult(httpCalls.ToImmutable());
    }

    private bool IsHttpClientCall(IMethodSymbol method, out string httpVerb)
    {
        httpVerb = string.Empty;
        var containingType = method.ContainingType;

        if (containingType?.Name != "HttpClient" ||
            containingType.ContainingNamespace?.ToDisplayString() != "System.Net.Http")
        {
            return false;
        }

        var methodName = method.Name;
        if (methodName == "GetAsync" || methodName == "GetStringAsync" ||
            methodName == "GetByteArrayAsync" || methodName == "GetStreamAsync")
        {
            httpVerb = "GET";
            return true;
        }
        else if (methodName == "PostAsync")
        {
            httpVerb = "POST";
            return true;
        }
        else if (methodName == "PutAsync")
        {
            httpVerb = "PUT";
            return true;
        }
        else if (methodName == "DeleteAsync")
        {
            httpVerb = "DELETE";
            return true;
        }
        else if (methodName == "PatchAsync")
        {
            httpVerb = "PATCH";
            return true;
        }

        return false;
    }

    private string? TryExtractRoute(IMethodSymbol method)
    {
        // TODO: Implement value content analysis to extract route from arguments
        // This would require analyzing the IOperation tree for the first string argument
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
