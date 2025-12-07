using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Slicito.Abstractions;
using Slicito.Abstractions.Facts;
using Slicito.Common.Implementation;
using Slicito.DotNet;
using Slicito.DotNet.Facts;

namespace Slicito.Proclaimer.Analyzers;

/// <summary>
/// Analyzes methods to detect HttpClient usage and outgoing HTTP requests.
/// </summary>
internal class HttpClientAnalyzer
{
    private readonly DotNetSolutionContext _dotnetContext;
    private readonly DotNetTypes _dotnetTypes;
    
    public HttpClientAnalyzer(DotNetSolutionContext dotnetContext, DotNetTypes dotnetTypes)
    {
        _dotnetContext = dotnetContext;
        _dotnetTypes = dotnetTypes;
    }
    
    /// <summary>
    /// Discovers all HTTP client usages by analyzing all methods in the solution.
    /// </summary>
    public async Task<ImmutableArray<HttpClientUsage>> DiscoverAllHttpClientUsagesAsync()
    {
        var usages = ImmutableArray.CreateBuilder<HttpClientUsage>();
        
        // Get all methods from the DotNet slice
        var allMethods = await _dotnetContext.Slice.GetRootElementsAsync(_dotnetTypes.Method);
        var dotnetFragment = _dotnetContext.TypedSliceFragment;
        
        foreach (var method in allMethods)
        {
            // Create a procedure element wrapper for this method
            var procedureElement = new SimpleProcedureElement(method.Id);
            
            // Get all operations for this method
            var operations = await dotnetFragment.GetOperationsAsync(procedureElement);
            
            foreach (var operation in operations)
            {
                // Check if this is a call operation
                var operationType = _dotnetContext.Slice.GetElementType(operation.Id);
                if (!operationType.Value.IsSubsetOfOrEquals(_dotnetTypes.Call.Value))
                    continue;
                
                // Get the symbol for the called method
                var symbol = _dotnetContext.GetSymbol(operation.Id);
                if (symbol is not IMethodSymbol methodSymbol)
                    continue;
                
                var containingType = methodSymbol.ContainingType;
                
                // Check if this is an HttpClient method call
                if (!IsHttpClientType(containingType))
                    continue;
                
                var methodName = methodSymbol.Name;
                
                // Detect HTTP verb methods
                string? httpVerb = null;
                if (methodName == "GetAsync" || methodName == "GetStringAsync" || 
                    methodName == "GetByteArrayAsync" || methodName == "GetStreamAsync")
                    httpVerb = "GET";
                else if (methodName == "PostAsync")
                    httpVerb = "POST";
                else if (methodName == "PutAsync")
                    httpVerb = "PUT";
                else if (methodName == "DeleteAsync")
                    httpVerb = "DELETE";
                else if (methodName == "PatchAsync")
                    httpVerb = "PATCH";
                
                if (httpVerb == null)
                    continue;
                
                // Create HTTP client usage
                usages.Add(new HttpClientUsage(
                    method.Id,
                    operation.Id,
                    httpVerb,
                    null  // URL extraction would require constant propagation analysis
                ));
            }
        }
        
        return usages.ToImmutable();
    }
    
    private bool IsHttpClientType(ITypeSymbol type)
    {
        // Check for System.Net.Http.HttpClient
        return type.Name == "HttpClient" && 
               type.ContainingNamespace?.ToString() == "System.Net.Http";
    }
    
    // Helper class to wrap ElementId as ICSharpProcedureElement
    private class SimpleProcedureElement : ElementBase, ICSharpProcedureElement
    {
        public SimpleProcedureElement(ElementId id) : base(id)
        {
        }
        
        public string Runtime => DotNetAttributeValues.Runtime.DotNet;
        public string Language => DotNetAttributeValues.Language.CSharp;
    }
}

/// <summary>
/// Represents a detected HTTP client usage.
/// </summary>
internal record HttpClientUsage(
    ElementId SourceMethodId,
    ElementId CallOperationId,
    string HttpVerb,
    string? TargetUrl
);
