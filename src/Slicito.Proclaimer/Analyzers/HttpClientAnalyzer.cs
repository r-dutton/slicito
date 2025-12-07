using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Slicito.Abstractions;

namespace Slicito.Proclaimer.Analyzers;

/// <summary>
/// Analyzes code to detect HttpClient usage and outgoing HTTP requests.
/// </summary>
internal class HttpClientAnalyzer
{
    private readonly IMethodSymbol _method;
    private readonly IOperation _methodBody;
    private readonly ElementId _methodElementId;
    
    public HttpClientAnalyzer(IMethodSymbol method, IOperation methodBody, ElementId methodElementId)
    {
        _method = method;
        _methodBody = methodBody;
        _methodElementId = methodElementId;
    }
    
    /// <summary>
    /// Discovers HTTP client usages in the method.
    /// </summary>
    public ImmutableArray<HttpClientUsage> DiscoverHttpClientUsages()
    {
        var usages = ImmutableArray.CreateBuilder<HttpClientUsage>();
        
        // Walk the operation tree looking for HttpClient method invocations
        WalkOperations(_methodBody, usages);
        
        return usages.ToImmutable();
    }
    
    private void WalkOperations(IOperation operation, ImmutableArray<HttpClientUsage>.Builder usages)
    {
        if (operation is IInvocationOperation invocation)
        {
            AnalyzeInvocation(invocation, usages);
        }
        
        // Recursively visit child operations
        foreach (var child in operation.ChildOperations)
        {
            WalkOperations(child, usages);
        }
    }
    
    private void AnalyzeInvocation(IInvocationOperation invocation, ImmutableArray<HttpClientUsage>.Builder usages)
    {
        var targetMethod = invocation.TargetMethod;
        var containingType = targetMethod.ContainingType;
        
        // Check if this is an HttpClient method call
        if (!IsHttpClientType(containingType))
            return;
        
        var methodName = targetMethod.Name;
        
        // Detect HTTP verb methods: GetAsync, PostAsync, PutAsync, DeleteAsync, PatchAsync
        string? httpVerb = null;
        if (methodName == "GetAsync" || methodName == "GetStringAsync" || methodName == "GetByteArrayAsync" || methodName == "GetStreamAsync")
            httpVerb = "GET";
        else if (methodName == "PostAsync")
            httpVerb = "POST";
        else if (methodName == "PutAsync")
            httpVerb = "PUT";
        else if (methodName == "DeleteAsync")
            httpVerb = "DELETE";
        else if (methodName == "PatchAsync")
            httpVerb = "PATCH";
        else if (methodName == "SendAsync")
            httpVerb = ExtractVerbFromHttpRequestMessage(invocation);
        
        if (httpVerb == null)
            return;
        
        // Try to extract the URL
        var url = ExtractUrl(invocation);
        
        usages.Add(new HttpClientUsage(
            _methodElementId,
            httpVerb,
            url,
            invocation.Syntax.GetLocation().GetLineSpan().StartLinePosition.Line
        ));
    }
    
    private bool IsHttpClientType(ITypeSymbol type)
    {
        // Check for System.Net.Http.HttpClient
        return type.Name == "HttpClient" && 
               type.ContainingNamespace?.ToString() == "System.Net.Http";
    }
    
    private string? ExtractUrl(IInvocationOperation invocation)
    {
        // Try to extract URL from first string argument
        if (invocation.Arguments.Length > 0)
        {
            var firstArg = invocation.Arguments[0];
            
            // Check for constant string
            if (firstArg.Value.ConstantValue.HasValue && 
                firstArg.Value.ConstantValue.Value is string url)
            {
                return url;
            }
            
            // Check for string interpolation or concatenation
            // For now, we'll mark as unknown if not a constant
            return null;
        }
        
        return null;
    }
    
    private string? ExtractVerbFromHttpRequestMessage(IInvocationOperation invocation)
    {
        // For SendAsync, we need to look at the HttpRequestMessage parameter
        // This is more complex and would require analyzing the request message creation
        // For now, return null (unknown verb)
        return null;
    }
}

/// <summary>
/// Represents a detected HTTP client usage.
/// </summary>
internal record HttpClientUsage(
    ElementId SourceMethodId,
    string HttpVerb,
    string? TargetUrl,
    int LineNumber
);
