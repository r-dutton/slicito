using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Slicito.Abstractions;
using Slicito.DotNet;

namespace Slicito.Proclaimer;

/// <summary>
/// Builds a Proclaimer slice fragment by analyzing a DotNet solution and extracting
/// service architecture elements (endpoints, HTTP clients, CQRS patterns, etc.).
/// </summary>
public class ProclaimerSliceFragmentBuilder
{
    private readonly DotNetSolutionContext _dotnetContext;
    private readonly DotNetTypes _dotnetTypes;
    private readonly ProclaimerTypes _proclaimerTypes;
    private readonly ISliceManager _sliceManager;
    
    public ProclaimerSliceFragmentBuilder(
        DotNetSolutionContext dotnetContext,
        DotNetTypes dotnetTypes,
        ProclaimerTypes proclaimerTypes,
        ISliceManager sliceManager)
    {
        _dotnetContext = dotnetContext;
        _dotnetTypes = dotnetTypes;
        _proclaimerTypes = proclaimerTypes;
        _sliceManager = sliceManager;
    }
    
    /// <summary>
    /// Builds the Proclaimer slice by analyzing the DotNet solution.
    /// </summary>
    public async Task<IProclaimerSliceFragment> BuildAsync()
    {
        var endpointElements = await DiscoverEndpointsAsync();
        
        // Build the slice with discovered elements
        var slice = _sliceManager.CreateBuilder()
            .AddRootElements(_proclaimerTypes.EndpointController, () => LoadEndpoints(endpointElements))
            .AddElementAttribute(_proclaimerTypes.EndpointController, ProclaimerAttributeNames.Verb, LoadEndpointVerb(endpointElements))
            .AddElementAttribute(_proclaimerTypes.EndpointController, ProclaimerAttributeNames.Route, LoadEndpointRoute(endpointElements))
            .Build();
        
        return new ProclaimerSliceFragment(slice, _proclaimerTypes);
    }
    
    private async Task<ImmutableArray<EndpointInfo>> DiscoverEndpointsAsync()
    {
        var endpoints = ImmutableArray.CreateBuilder<EndpointInfo>();
        
        // Get all methods from the DotNet slice
        var methods = await DotNetMethodHelper.GetAllMethodsWithDisplayNamesAsync(
            _dotnetContext.Slice, 
            _dotnetTypes);
        
        foreach (var methodInfo in methods)
        {
            var methodElement = methodInfo.Method;
            var methodSymbol = (IMethodSymbol)_dotnetContext.GetSymbol(methodElement);
            
            // Check for HTTP method attributes (HttpGet, HttpPost, etc.)
            var httpMethodAttribute = methodSymbol.GetAttributes()
                .FirstOrDefault(IsHttpMethodAttribute);
            
            if (httpMethodAttribute == null)
                continue;
            
            // Check if containing type is a controller
            if (!IsController(methodSymbol.ContainingType))
                continue;
            
            // Extract HTTP method and route
            var (httpMethod, methodRoute) = GetHttpMethodAndRoute(httpMethodAttribute);
            var controllerRoute = GetControllerRoute(methodSymbol.ContainingType);
            var fullRoute = CombineRoutes(controllerRoute, methodRoute);
            
            // Create element ID for the endpoint
            // We'll create a new element ID based on the method element
            var endpointId = new ElementId($"{methodElement.Id.Value}:endpoint");
            
            endpoints.Add(new EndpointInfo(endpointId, methodElement.Id, httpMethod, fullRoute));
        }
        
        return endpoints.ToImmutable();
    }
    
    private bool IsHttpMethodAttribute(AttributeData data)
    {
        if (data.AttributeClass is null ||
            !data.AttributeClass.Name.StartsWith("Http") ||
            !data.AttributeClass.Name.EndsWith("Attribute"))
        {
            return false;
        }
        
        var baseType = data.AttributeClass.BaseType;
        return baseType?.Name == "HttpMethodAttribute";
    }
    
    private bool IsController(ITypeSymbol type)
    {
        if (!type.Name.EndsWith("Controller"))
            return false;
        
        return type.GetAttributes()
            .Any(attr => attr.AttributeClass?.Name is "ControllerAttribute" or "ApiControllerAttribute");
    }
    
    private string GetControllerRoute(ITypeSymbol type)
    {
        var routeAttribute = type.GetAttributes()
            .FirstOrDefault(attr => attr.AttributeClass?.Name == "RouteAttribute");
        
        if (routeAttribute == null)
            return "";
        
        var routeConstant = routeAttribute.ConstructorArguments.FirstOrDefault();
        return routeConstant.Kind == TypedConstantKind.Error ? "" : (string?)routeConstant.Value ?? "";
    }
    
    private (string method, string route) GetHttpMethodAndRoute(AttributeData data)
    {
        var methodString = data.AttributeClass!.Name["Http".Length..^"Attribute".Length];
        var method = methodString.ToUpperInvariant();
        
        var routeConstant = data.ConstructorArguments.FirstOrDefault();
        var route = routeConstant.Kind == TypedConstantKind.Error ? null : (string?)routeConstant.Value;
        
        return (method, route ?? "");
    }
    
    private string CombineRoutes(string controllerRoute, string endpointRoute)
    {
        var segments = new[]
        {
            controllerRoute.Trim('/'),
            endpointRoute.Trim('/')
        }
        .Where(s => !string.IsNullOrEmpty(s));
        
        var combinedRoute = string.Join("/", segments);
        return combinedRoute.Length == 0 ? "/" : $"/{combinedRoute}";
    }
    
    private ValueTask<IEnumerable<ISliceBuilder.PartialElementInfo>> LoadEndpoints(
        ImmutableArray<EndpointInfo> endpoints)
    {
        var result = endpoints
            .Select(e => new ISliceBuilder.PartialElementInfo(e.EndpointId, _proclaimerTypes.EndpointController))
            .ToArray();
        
        return new ValueTask<IEnumerable<ISliceBuilder.PartialElementInfo>>(result);
    }
    
    private ISliceBuilder.LoadElementAttributeAsyncCallback LoadEndpointVerb(
        ImmutableArray<EndpointInfo> endpoints)
    {
        var lookup = endpoints.ToImmutableDictionary(e => e.EndpointId, e => e.HttpMethod);
        
        return elementId =>
        {
            var value = lookup.TryGetValue(elementId, out var verb) ? verb : "";
            return new ValueTask<string>(value);
        };
    }
    
    private ISliceBuilder.LoadElementAttributeAsyncCallback LoadEndpointRoute(
        ImmutableArray<EndpointInfo> endpoints)
    {
        var lookup = endpoints.ToImmutableDictionary(e => e.EndpointId, e => e.Route);
        
        return elementId =>
        {
            var value = lookup.TryGetValue(elementId, out var route) ? route : "";
            return new ValueTask<string>(value);
        };
    }
    
    private record EndpointInfo(ElementId EndpointId, ElementId MethodId, string HttpMethod, string Route);
}

/// <summary>
/// Implementation of IProclaimerSliceFragment.
/// </summary>
internal class ProclaimerSliceFragment(ISlice slice, ProclaimerTypes proclaimerTypes) : IProclaimerSliceFragment
{
    public ISlice Slice { get; } = slice;
    
    // Keep reference to types for future use
    private readonly ProclaimerTypes _types = proclaimerTypes;
}
