using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Slicito.Abstractions;
using Slicito.DotNet;
using Slicito.Proclaimer.Analyzers;

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
        var cqrsTypes = await DiscoverCqrsTypesAsync();
        var efTypes = await DiscoverEntityFrameworkTypesAsync();
        var repositories = await DiscoverRepositoriesAsync();
        var backgroundServices = await DiscoverBackgroundServicesAsync();
        var httpClientUsages = await DiscoverHttpClientUsagesAsync();
        
        // Build the slice with discovered elements
        var builder = _sliceManager.CreateBuilder()
            // Endpoints
            .AddRootElements(_proclaimerTypes.EndpointController, () => LoadEndpoints(endpointElements))
            .AddElementAttribute(_proclaimerTypes.EndpointController, ProclaimerAttributeNames.Verb, LoadEndpointVerb(endpointElements))
            .AddElementAttribute(_proclaimerTypes.EndpointController, ProclaimerAttributeNames.Route, LoadEndpointRoute(endpointElements));
        
        // CQRS Requests
        if (cqrsTypes.Requests.Length > 0)
        {
            builder.AddRootElements(_proclaimerTypes.CqrsRequest, () => LoadCqrsRequests(cqrsTypes.Requests));
        }
        
        // CQRS Handlers
        if (cqrsTypes.Handlers.Length > 0)
        {
            builder.AddRootElements(_proclaimerTypes.CqrsHandler, () => LoadCqrsHandlers(cqrsTypes.Handlers));
        }
        
        // Notifications
        if (cqrsTypes.Notifications.Length > 0)
        {
            builder.AddRootElements(_proclaimerTypes.MessageContract, () => LoadNotifications(cqrsTypes.Notifications));
        }
        
        // Notification Handlers
        if (cqrsTypes.NotificationHandlers.Length > 0)
        {
            builder.AddRootElements(_proclaimerTypes.NotificationHandler, () => LoadNotificationHandlers(cqrsTypes.NotificationHandlers));
        }
        
        // EF DbContexts
        if (efTypes.DbContexts.Length > 0)
        {
            builder.AddRootElements(_proclaimerTypes.EfDbContext, () => LoadDbContexts(efTypes.DbContexts));
        }
        
        // EF Entities
        if (efTypes.Entities.Length > 0)
        {
            builder.AddRootElements(_proclaimerTypes.EfEntity, () => LoadEfEntities(efTypes.Entities));
        }
        
        // Repositories
        if (repositories.Length > 0)
        {
            builder.AddRootElements(_proclaimerTypes.Repository, () => LoadRepositories(repositories));
        }
        
        // Background Services
        if (backgroundServices.Length > 0)
        {
            builder.AddRootElements(_proclaimerTypes.BackgroundService, () => LoadBackgroundServices(backgroundServices));
        }
        
        // HTTP Client usages
        if (httpClientUsages.Length > 0)
        {
            builder.AddRootElements(_proclaimerTypes.HttpClient, () => LoadHttpClients(httpClientUsages));
            builder.AddElementAttribute(_proclaimerTypes.HttpClient, ProclaimerAttributeNames.Verb, LoadHttpClientVerb(httpClientUsages));
            
            // Add SendsRequest links from methods to HTTP clients
            // The source is the method (any type), target is HttpClient
            builder.AddLinks(
                _proclaimerTypes.SendsRequest,
                _dotnetTypes.Method,  // Source is any method
                _proclaimerTypes.HttpClient,  // Target is HTTP client
                sourceId => LoadSendsRequestLinks(httpClientUsages, sourceId));
        }
        
        var slice = builder.Build();
        
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
    
    // CQRS Discovery
    private async Task<CqrsTypesInfo> DiscoverCqrsTypesAsync()
    {
        var requests = ImmutableArray.CreateBuilder<TypeElementInfo>();
        var handlers = ImmutableArray.CreateBuilder<TypeElementInfo>();
        var notifications = ImmutableArray.CreateBuilder<TypeElementInfo>();
        var notificationHandlers = ImmutableArray.CreateBuilder<TypeElementInfo>();
        
        var types = await GetAllTypesAsync();
        
        foreach (var type in types)
        {
            if (CqrsAnalyzer.IsRequest(type.Symbol))
            {
                requests.Add(type);
            }
            else if (CqrsAnalyzer.IsRequestHandler(type.Symbol))
            {
                handlers.Add(type);
            }
            else if (CqrsAnalyzer.IsNotification(type.Symbol))
            {
                notifications.Add(type);
            }
            else if (CqrsAnalyzer.IsNotificationHandler(type.Symbol))
            {
                notificationHandlers.Add(type);
            }
        }
        
        return new CqrsTypesInfo(
            requests.ToImmutable(),
            handlers.ToImmutable(),
            notifications.ToImmutable(),
            notificationHandlers.ToImmutable());
    }
    
    private async Task<EntityFrameworkTypesInfo> DiscoverEntityFrameworkTypesAsync()
    {
        var dbContexts = ImmutableArray.CreateBuilder<TypeElementInfo>();
        var entities = ImmutableArray.CreateBuilder<TypeElementInfo>();
        var types = await GetAllTypesAsync();
        
        foreach (var type in types)
        {
            if (EntityFrameworkAnalyzer.IsDbContext(type.Symbol))
            {
                dbContexts.Add(type);
                
                // Also discover DbSet properties (entities)
                var dbSetProperties = EntityFrameworkAnalyzer.GetDbSetProperties(type.Symbol);
                foreach (var property in dbSetProperties)
                {
                    // Get the entity type from DbSet<TEntity>
                    var propertyType = property.Type as INamedTypeSymbol;
                    if (propertyType?.TypeArguments.Length == 1)
                    {
                        var entityType = propertyType.TypeArguments[0] as INamedTypeSymbol;
                        if (entityType != null)
                        {
                            // Find the element ID for this entity type
                            var entityTypeElement = types.FirstOrDefault(t => 
                                SymbolEqualityComparer.Default.Equals(t.Symbol, entityType));
                            if (entityTypeElement != null)
                            {
                                entities.Add(entityTypeElement);
                            }
                        }
                    }
                }
            }
        }
        
        return new EntityFrameworkTypesInfo(dbContexts.ToImmutable(), entities.ToImmutable());
    }
    
    private async Task<ImmutableArray<TypeElementInfo>> DiscoverRepositoriesAsync()
    {
        var repositories = ImmutableArray.CreateBuilder<TypeElementInfo>();
        var types = await GetAllTypesAsync();
        
        foreach (var type in types)
        {
            if (RepositoryAnalyzer.IsRepository(type.Symbol))
            {
                repositories.Add(type);
            }
        }
        
        return repositories.ToImmutable();
    }
    
    private async Task<ImmutableArray<TypeElementInfo>> DiscoverBackgroundServicesAsync()
    {
        var services = ImmutableArray.CreateBuilder<TypeElementInfo>();
        var types = await GetAllTypesAsync();
        
        foreach (var type in types)
        {
            if (BackgroundServiceAnalyzer.IsHostedService(type.Symbol))
            {
                services.Add(type);
            }
        }
        
        return services.ToImmutable();
    }
    
    private async Task<ImmutableArray<HttpClientUsage>> DiscoverHttpClientUsagesAsync()
    {
        var httpClientAnalyzer = new HttpClientAnalyzer(_dotnetContext, _dotnetTypes);
        return await httpClientAnalyzer.DiscoverAllHttpClientUsagesAsync();
    }
    
    private async Task<ImmutableArray<TypeElementInfo>> GetAllTypesAsync()
    {
        var types = ImmutableArray.CreateBuilder<TypeElementInfo>();
        
        // Get all type elements from the DotNet slice
        var typeElements = await _dotnetContext.Slice.GetRootElementsAsync(_dotnetTypes.Type);
        
        foreach (var typeElement in typeElements)
        {
            var symbol = _dotnetContext.GetSymbol(typeElement.Id);
            if (symbol is INamedTypeSymbol namedType)
            {
                types.Add(new TypeElementInfo(typeElement.Id, namedType));
            }
        }
        
        return types.ToImmutable();
    }
    
    // CQRS Loaders
    private ValueTask<IEnumerable<ISliceBuilder.PartialElementInfo>> LoadCqrsRequests(
        ImmutableArray<TypeElementInfo> requests)
    {
        var result = requests
            .Select(r => new ISliceBuilder.PartialElementInfo(r.ElementId, _proclaimerTypes.CqrsRequest))
            .ToArray();
        return new ValueTask<IEnumerable<ISliceBuilder.PartialElementInfo>>(result);
    }
    
    private ValueTask<IEnumerable<ISliceBuilder.PartialElementInfo>> LoadCqrsHandlers(
        ImmutableArray<TypeElementInfo> handlers)
    {
        var result = handlers
            .Select(h => new ISliceBuilder.PartialElementInfo(h.ElementId, _proclaimerTypes.CqrsHandler))
            .ToArray();
        return new ValueTask<IEnumerable<ISliceBuilder.PartialElementInfo>>(result);
    }
    
    private ValueTask<IEnumerable<ISliceBuilder.PartialElementInfo>> LoadNotifications(
        ImmutableArray<TypeElementInfo> notifications)
    {
        var result = notifications
            .Select(n => new ISliceBuilder.PartialElementInfo(n.ElementId, _proclaimerTypes.MessageContract))
            .ToArray();
        return new ValueTask<IEnumerable<ISliceBuilder.PartialElementInfo>>(result);
    }
    
    private ValueTask<IEnumerable<ISliceBuilder.PartialElementInfo>> LoadNotificationHandlers(
        ImmutableArray<TypeElementInfo> handlers)
    {
        var result = handlers
            .Select(h => new ISliceBuilder.PartialElementInfo(h.ElementId, _proclaimerTypes.NotificationHandler))
            .ToArray();
        return new ValueTask<IEnumerable<ISliceBuilder.PartialElementInfo>>(result);
    }
    
    // EF Loaders
    private ValueTask<IEnumerable<ISliceBuilder.PartialElementInfo>> LoadDbContexts(
        ImmutableArray<TypeElementInfo> dbContexts)
    {
        var result = dbContexts
            .Select(db => new ISliceBuilder.PartialElementInfo(db.ElementId, _proclaimerTypes.EfDbContext))
            .ToArray();
        return new ValueTask<IEnumerable<ISliceBuilder.PartialElementInfo>>(result);
    }
    
    // Repository Loaders
    private ValueTask<IEnumerable<ISliceBuilder.PartialElementInfo>> LoadRepositories(
        ImmutableArray<TypeElementInfo> repositories)
    {
        var result = repositories
            .Select(r => new ISliceBuilder.PartialElementInfo(r.ElementId, _proclaimerTypes.Repository))
            .ToArray();
        return new ValueTask<IEnumerable<ISliceBuilder.PartialElementInfo>>(result);
    }
    
    // Background Service Loaders
    private ValueTask<IEnumerable<ISliceBuilder.PartialElementInfo>> LoadBackgroundServices(
        ImmutableArray<TypeElementInfo> services)
    {
        var result = services
            .Select(s => new ISliceBuilder.PartialElementInfo(s.ElementId, _proclaimerTypes.BackgroundService))
            .ToArray();
        return new ValueTask<IEnumerable<ISliceBuilder.PartialElementInfo>>(result);
    }
    
    // EF Entity Loaders
    private ValueTask<IEnumerable<ISliceBuilder.PartialElementInfo>> LoadEfEntities(
        ImmutableArray<TypeElementInfo> entities)
    {
        var result = entities
            .Select(e => new ISliceBuilder.PartialElementInfo(e.ElementId, _proclaimerTypes.EfEntity))
            .ToArray();
        return new ValueTask<IEnumerable<ISliceBuilder.PartialElementInfo>>(result);
    }
    
    // HTTP Client Loaders
    private ValueTask<IEnumerable<ISliceBuilder.PartialElementInfo>> LoadHttpClients(
        ImmutableArray<HttpClientUsage> usages)
    {
        var result = usages
            .Select(u => new ISliceBuilder.PartialElementInfo(u.CallOperationId, _proclaimerTypes.HttpClient))
            .ToArray();
        return new ValueTask<IEnumerable<ISliceBuilder.PartialElementInfo>>(result);
    }
    
    private ISliceBuilder.LoadElementAttributeAsyncCallback LoadHttpClientVerb(
        ImmutableArray<HttpClientUsage> usages)
    {
        var lookup = usages.ToImmutableDictionary(u => u.CallOperationId, u => u.HttpVerb);
        
        return elementId =>
        {
            var value = lookup.TryGetValue(elementId, out var verb) ? verb : "";
            return new ValueTask<string>(value);
        };
    }
    
    private ValueTask<IEnumerable<ISliceBuilder.PartialLinkInfo>> LoadSendsRequestLinks(
        ImmutableArray<HttpClientUsage> usages,
        ElementId sourceMethodId)
    {
        // Find all HTTP client usages for this method
        var methodUsages = usages
            .Where(u => u.SourceMethodId == sourceMethodId)
            .Select(u => new ISliceBuilder.PartialLinkInfo(
                new ISliceBuilder.PartialElementInfo(u.CallOperationId, _proclaimerTypes.HttpClient)))
            .ToArray();
        
        return new ValueTask<IEnumerable<ISliceBuilder.PartialLinkInfo>>(methodUsages);
    }
    
    // Supporting types
    private record TypeElementInfo(ElementId ElementId, INamedTypeSymbol Symbol);
    private record CqrsTypesInfo(
        ImmutableArray<TypeElementInfo> Requests,
        ImmutableArray<TypeElementInfo> Handlers,
        ImmutableArray<TypeElementInfo> Notifications,
        ImmutableArray<TypeElementInfo> NotificationHandlers);
    private record EntityFrameworkTypesInfo(
        ImmutableArray<TypeElementInfo> DbContexts,
        ImmutableArray<TypeElementInfo> Entities);
    
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
