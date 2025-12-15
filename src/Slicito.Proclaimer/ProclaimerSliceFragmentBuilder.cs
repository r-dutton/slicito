using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Net.Http;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Slicito.Abstractions;
using Slicito.Abstractions.Facts;
using Slicito.DotNet;
using Slicito.DotNet.AspNetCore;
using Slicito.Proclaimer.Facts;

namespace Slicito.Proclaimer;

/// <summary>
/// Builds a Proclaimer slice fragment by analyzing a DotNet solution.
/// This class currently establishes the canonical schema; discovery logic is added in subsequent tasks.
/// </summary>
public class ProclaimerSliceFragmentBuilder : ITypedSliceFragmentBuilder<IProclaimerSliceFragment>
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
    /// Builds the Proclaimer slice using the canonical schema. Element discovery will be layered on in later tasks.
    /// </summary>
    public async ValueTask<IProclaimerSliceFragment> BuildAsync()
    {
        var dotnetFragment = _dotnetContext.TypedSliceFragment;

        var services = await DiscoverServicesAsync(dotnetFragment);
        var methodSymbolMap = await BuildMethodSymbolMapAsync();
        var endpoints = await DiscoverEndpointsAsync(dotnetFragment, services, methodSymbolMap);

        var servicesById = services.ToDictionary(s => s.Id, s => s);
        var endpointsById = endpoints.ToDictionary(e => e.Id, e => e);

        var builder = _sliceManager.CreateBuilder();

        builder
            .AddRootElements(_proclaimerTypes.Service, () =>
                new ValueTask<IEnumerable<ISliceBuilder.PartialElementInfo>>(services.Select(s =>
                    new ISliceBuilder.PartialElementInfo(s.Id, _proclaimerTypes.Service))))
            .AddRootElements(_proclaimerTypes.Endpoint, () =>
                new ValueTask<IEnumerable<ISliceBuilder.PartialElementInfo>>(endpoints.Select(e =>
                    new ISliceBuilder.PartialElementInfo(e.Id, _proclaimerTypes.Endpoint))))
            .AddRootElements(_proclaimerTypes.HttpClient, EmptyElements)
            .AddRootElements(_proclaimerTypes.Repository, EmptyElements)
            .AddRootElements(_proclaimerTypes.Database, EmptyElements)
            .AddRootElements(_proclaimerTypes.Queue, EmptyElements)
            .AddRootElements(_proclaimerTypes.Topic, EmptyElements)
            .AddRootElements(_proclaimerTypes.BackgroundService, EmptyElements)
            .AddElementAttribute(_proclaimerTypes.Service, ProclaimerAttributeNames.ServiceName, id =>
                new ValueTask<string>(servicesById[id].ServiceName))
            .AddElementAttribute(_proclaimerTypes.Endpoint, ProclaimerAttributeNames.ServiceName, id =>
                new ValueTask<string>(endpointsById[id].ServiceName))
            .AddElementAttribute(_proclaimerTypes.Endpoint, ProclaimerAttributeNames.HttpMethod, id =>
                new ValueTask<string>(endpointsById[id].HttpMethod))
            .AddElementAttribute(_proclaimerTypes.Endpoint, ProclaimerAttributeNames.Route, id =>
                new ValueTask<string>(endpointsById[id].Route))
            .AddElementAttribute(_proclaimerTypes.Endpoint, CommonAttributeNames.CodeLocation, id =>
                new ValueTask<string>(endpointsById[id].CodeLocation ?? string.Empty))
            .AddLinks(_proclaimerTypes.BelongsToService, _proclaimerTypes.Endpoint, _proclaimerTypes.Service, sourceId =>
                new ValueTask<IEnumerable<ISliceBuilder.PartialLinkInfo>>(endpointsById.TryGetValue(sourceId, out var endpoint)
                    ? new[]
                    {
                        new ISliceBuilder.PartialLinkInfo(
                            new ISliceBuilder.PartialElementInfo(endpoint.ServiceId, _proclaimerTypes.Service),
                            _proclaimerTypes.BelongsToService)
                    }
                    : Array.Empty<ISliceBuilder.PartialLinkInfo>()))
            .AddLinks(_proclaimerTypes.Calls, _proclaimerTypes.Endpoint, _dotnetTypes.Method, sourceId =>
                new ValueTask<IEnumerable<ISliceBuilder.PartialLinkInfo>>(endpointsById.TryGetValue(sourceId, out var endpoint)
                    ? new[]
                    {
                        new ISliceBuilder.PartialLinkInfo(
                            new ISliceBuilder.PartialElementInfo(endpoint.Handler.Id, _dotnetTypes.Method),
                            _proclaimerTypes.Calls)
                    }
                    : Array.Empty<ISliceBuilder.PartialLinkInfo>()));

        var slice = builder.Build();

        return new ProclaimerSliceFragment(slice, _proclaimerTypes);
    }

    private static ValueTask<IEnumerable<ISliceBuilder.PartialElementInfo>> EmptyElements() =>
        new(Array.Empty<ISliceBuilder.PartialElementInfo>());

    private async Task<IReadOnlyList<ServiceInfo>> DiscoverServicesAsync(IDotNetSliceFragment dotnetFragment)
    {
        var services = new List<ServiceInfo>();
        var seenIds = new HashSet<ElementId>();

        var solutions = await dotnetFragment.GetSolutionsAsync();
        foreach (var solution in solutions)
        {
            var projects = await dotnetFragment.GetProjectsAsync(solution);
            foreach (var project in projects)
            {
                var serviceId = CreateServiceId(project.Id);
                if (!seenIds.Add(serviceId))
                {
                    continue;
                }

                var roslynProject = _dotnetContext.GetProject(project.Id);
                var serviceName = Path.GetFileNameWithoutExtension(roslynProject.FilePath);

                services.Add(new ServiceInfo(serviceId, serviceName, project.Id, roslynProject.FilePath!));
            }
        }

        return services;
    }

    private async Task<IReadOnlyList<EndpointInfo>> DiscoverEndpointsAsync(
        IDotNetSliceFragment dotnetFragment,
        IReadOnlyCollection<ServiceInfo> services,
        IReadOnlyDictionary<IMethodSymbol, ElementInfo> methodSymbolMap)
    {
        var serviceByProjectPath = services.ToDictionary(s => s.ProjectPath, s => s);

        var codeLocationProvider = _dotnetContext.Slice.GetElementAttributeProviderAsyncCallback(
            CommonAttributeNames.CodeLocation);

        var apiEndpoints = await new ApiEndpointList.Builder(_dotnetContext.Slice, _dotnetContext, _dotnetTypes)
            .BuildAsync();

        var results = new Dictionary<ElementId, EndpointInfo>();

        foreach (var apiEndpoint in apiEndpoints.Endpoints)
        {
            _dotnetContext.GetSymbolAndRelatedProject(apiEndpoint.HandlerElement.Id, out var handlerProject);

            if (!serviceByProjectPath.TryGetValue(handlerProject.FilePath!, out var serviceInfo))
            {
                continue;
            }

            var endpointId = CreateEndpointId(apiEndpoint.HandlerElement.Id, apiEndpoint.Method, apiEndpoint.Path);
            var codeLocation = await codeLocationProvider(apiEndpoint.HandlerElement.Id);

            var endpoint = new EndpointInfo(
                endpointId,
                serviceInfo.ServiceName,
                apiEndpoint.Method.Method,
                apiEndpoint.Path,
                serviceInfo.Id,
                apiEndpoint.HandlerElement,
                codeLocation);

            if (!results.ContainsKey(endpoint.Id))
            {
                results.Add(endpoint.Id, endpoint);
            }
        }

        var minimalEndpoints = await DiscoverMinimalApiEndpointsAsync(serviceByProjectPath, methodSymbolMap);

        foreach (var endpoint in minimalEndpoints)
        {
            if (results.ContainsKey(endpoint.Id))
            {
                continue;
            }

            var codeLocation = await codeLocationProvider(endpoint.Handler.Id);
            results.Add(endpoint.Id, endpoint with { CodeLocation = codeLocation });
        }

        return results.Values.ToList();
    }

    private async Task<Dictionary<IMethodSymbol, ElementInfo>> BuildMethodSymbolMapAsync()
    {
        var methods = await DotNetMethodHelper.GetAllMethodsWithDisplayNamesAsync(_dotnetContext.Slice, _dotnetTypes);
        var map = new Dictionary<IMethodSymbol, ElementInfo>(SymbolEqualityComparer.Default);

        foreach (var method in methods)
        {
            if (_dotnetContext.GetSymbol(method.Method.Id) is IMethodSymbol methodSymbol &&
                !map.ContainsKey(methodSymbol))
            {
                map.Add(methodSymbol, method.Method);
            }
        }

        return map;
    }

    private async Task<IReadOnlyList<EndpointInfo>> DiscoverMinimalApiEndpointsAsync(
        IReadOnlyDictionary<string, ServiceInfo> serviceByProjectPath,
        IReadOnlyDictionary<IMethodSymbol, ElementInfo> methodSymbolMap)
    {
        var results = ImmutableArray.CreateBuilder<EndpointInfo>();

        foreach (var service in serviceByProjectPath.Values)
        {
            var project = _dotnetContext.GetProject(service.ProjectId);
            var compilation = await project.GetCompilationAsync();

            if (compilation is null)
            {
                continue;
            }

            foreach (var syntaxTree in compilation.SyntaxTrees)
            {
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var root = await syntaxTree.GetRootAsync();

                foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
                {
                    var methodSymbol = semanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
                    var (httpMethod, route) = GetMinimalApiHttpMethodAndRoute(invocation, methodSymbol, semanticModel);

                    if (httpMethod is null || route is null)
                    {
                        continue;
                    }

                    var handlerSymbol = GetMinimalApiHandlerSymbol(invocation, semanticModel);

                    if (handlerSymbol is null || !methodSymbolMap.TryGetValue(handlerSymbol, out var handlerElement))
                    {
                        continue;
                    }

                    var endpointId = CreateEndpointId(handlerElement.Id, httpMethod, route);

                    results.Add(new EndpointInfo(
                        endpointId,
                        service.ServiceName,
                        httpMethod.Method,
                        route,
                        service.Id,
                        handlerElement,
                        string.Empty));
                }
            }
        }

        return results.ToImmutable();
    }

    private static (HttpMethod? HttpMethod, string? Route) GetMinimalApiHttpMethodAndRoute(
        InvocationExpressionSyntax invocation,
        IMethodSymbol? methodSymbol,
        SemanticModel semanticModel)
    {
        var candidateMethod = methodSymbol?.ReducedFrom ?? methodSymbol;

        if (candidateMethod is null || !IsMinimalApiMapMethod(candidateMethod))
        {
            return (null, null);
        }

        if (invocation.ArgumentList.Arguments.Count == 0)
        {
            return (null, null);
        }

        var routeExpression = invocation.ArgumentList.Arguments[0].Expression;
        var route = semanticModel.GetConstantValue(routeExpression).Value as string;

        if (string.IsNullOrWhiteSpace(route))
        {
            return (null, null);
        }

        var httpMethod = new HttpMethod(candidateMethod.Name["Map".Length..].ToUpperInvariant());
        var normalizedRoute = NormalizeRoute(route!);

        return (httpMethod, normalizedRoute);
    }

    private static IMethodSymbol? GetMinimalApiHandlerSymbol(InvocationExpressionSyntax invocation, SemanticModel semanticModel)
    {
        if (invocation.ArgumentList.Arguments.Count < 2)
        {
            return null;
        }

        var handlerExpression = invocation.ArgumentList.Arguments[1].Expression;
        var handlerInfo = semanticModel.GetSymbolInfo(handlerExpression);

        return handlerInfo.Symbol as IMethodSymbol ?? handlerInfo.CandidateSymbols.OfType<IMethodSymbol>().FirstOrDefault();
    }

    private static bool IsMinimalApiMapMethod(IMethodSymbol methodSymbol)
    {
        if (!methodSymbol.Name.StartsWith("Map", StringComparison.Ordinal))
        {
            return false;
        }

        var methodName = methodSymbol.Name["Map".Length..];

        return methodName is "Get" or "Post" or "Put" or "Delete" or "Patch" or "Head" or "Options";
    }

    private static string NormalizeRoute(string route)
    {
        var trimmed = route.Trim();

        return trimmed.StartsWith("/", StringComparison.Ordinal) ? trimmed : $"/{trimmed}";
    }

    private static ElementId CreateServiceId(ElementId projectId) => new($"proclaimer:service:{projectId.Value}");

    private static ElementId CreateEndpointId(ElementId methodId, HttpMethod method, string route) =>
        new($"proclaimer:endpoint:{methodId.Value}:{method.Method}:{route}");
}

/// <summary>
/// Implementation of IProclaimerSliceFragment.
/// </summary>
internal class ProclaimerSliceFragment : IProclaimerSliceFragment
{
    private readonly ProclaimerTypes _types;
    private readonly ILazyLinkExplorer _belongsToServiceExplorer;
    private readonly Func<ElementId, ValueTask<string>> _serviceNameProvider;
    private readonly Func<ElementId, ValueTask<string>> _httpMethodProvider;
    private readonly Func<ElementId, ValueTask<string>> _routeProvider;

    public ProclaimerSliceFragment(ISlice slice, ProclaimerTypes types)
    {
        Slice = slice;
        _types = types;

        _belongsToServiceExplorer = slice.GetLinkExplorer(types.BelongsToService);
        _serviceNameProvider = slice.GetElementAttributeProviderAsyncCallback(ProclaimerAttributeNames.ServiceName);
        _httpMethodProvider = slice.GetElementAttributeProviderAsyncCallback(ProclaimerAttributeNames.HttpMethod);
        _routeProvider = slice.GetElementAttributeProviderAsyncCallback(ProclaimerAttributeNames.Route);
    }

    public ISlice Slice { get; }

    public async ValueTask<IEnumerable<IProclaimerServiceElement>> GetServicesAsync()
    {
        var services = await Slice.GetRootElementsAsync(_types.Service);
        var result = new List<IProclaimerServiceElement>();

        foreach (var service in services)
        {
            var name = await _serviceNameProvider(service.Id);
            result.Add(new ProclaimerServiceElement(service.Id, name));
        }

        return result;
    }

    public async ValueTask<IEnumerable<IProclaimerEndpointElement>> GetEndpointsAsync()
    {
        var endpoints = await Slice.GetRootElementsAsync(_types.Endpoint);
        return await CreateEndpointElementsAsync(endpoints.Select(e => e.Id));
    }

    public async ValueTask<IEnumerable<IProclaimerEndpointElement>> GetEndpointsAsync(IProclaimerServiceElement service)
    {
        var endpoints = await Slice.GetRootElementsAsync(_types.Endpoint);
        var matchingEndpoints = new List<ElementId>();

        foreach (var endpoint in endpoints)
        {
            var target = await _belongsToServiceExplorer.TryGetTargetElementAsync(endpoint.Id);
            if (target?.Id == service.Id)
            {
                matchingEndpoints.Add(endpoint.Id);
            }
        }

        return await CreateEndpointElementsAsync(matchingEndpoints);
    }

    private async Task<IEnumerable<IProclaimerEndpointElement>> CreateEndpointElementsAsync(IEnumerable<ElementId> endpointIds)
    {
        var result = new List<IProclaimerEndpointElement>();

        foreach (var endpointId in endpointIds)
        {
            var serviceName = await _serviceNameProvider(endpointId);
            var httpMethod = await _httpMethodProvider(endpointId);
            var route = await _routeProvider(endpointId);

            result.Add(new ProclaimerEndpointElement(endpointId, serviceName, httpMethod, route));
        }

        return result;
    }
}

internal readonly record struct ServiceInfo(ElementId Id, string ServiceName, ElementId ProjectId, string ProjectPath);

internal readonly record struct EndpointInfo(
    ElementId Id,
    string ServiceName,
    string HttpMethod,
    string Route,
    ElementId ServiceId,
    ElementInfo Handler,
    string CodeLocation);

internal record ProclaimerServiceElement(ElementId Id, string ServiceName) : IProclaimerServiceElement;

internal record ProclaimerEndpointElement(
    ElementId Id,
    string ServiceName,
    string HttpMethod,
    string Route) : IProclaimerEndpointElement;
