using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
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
        var endpoints = await DiscoverEndpointsAsync(dotnetFragment, services);

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
        IReadOnlyCollection<ServiceInfo> services)
    {
        var serviceByProjectPath = services.ToDictionary(s => s.ProjectPath, s => s);

        var codeLocationProvider = _dotnetContext.Slice.GetElementAttributeProviderAsyncCallback(
            CommonAttributeNames.CodeLocation);

        var apiEndpoints = await new ApiEndpointList.Builder(_dotnetContext.Slice, _dotnetContext, _dotnetTypes)
            .BuildAsync();

        var results = new List<EndpointInfo>();

        foreach (var apiEndpoint in apiEndpoints.Endpoints)
        {
            _dotnetContext.GetSymbolAndRelatedProject(apiEndpoint.HandlerElement.Id, out var handlerProject);

            if (!serviceByProjectPath.TryGetValue(handlerProject.FilePath!, out var serviceInfo))
            {
                continue;
            }

            var endpointId = CreateEndpointId(apiEndpoint.HandlerElement.Id, apiEndpoint.Method, apiEndpoint.Path);
            var codeLocation = await codeLocationProvider(apiEndpoint.HandlerElement.Id);

            results.Add(new EndpointInfo(
                endpointId,
                serviceInfo.ServiceName,
                apiEndpoint.Method.Method,
                apiEndpoint.Path,
                serviceInfo.Id,
                apiEndpoint.HandlerElement,
                codeLocation));
        }

        return results;
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
