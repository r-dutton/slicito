using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis.MSBuild;
using Slicito.Abstractions;
using Slicito.Common;
using Slicito.DotNet;
using Slicito.Proclaimer.Facts;
using Slicito.Tests.Common;

namespace Slicito.Proclaimer.Tests;

[TestClass]
public class ProclaimerSliceFragmentBuilderTests
{
    private static DotNetSolutionContext? _dotnetContext;
    private static ProclaimerTypes? _proclaimerTypes;
    private static DotNetTypes? _dotnetTypes;
    private static IProclaimerSliceFragment? _fragment;

    [ClassInitialize]
    public static async Task Initialize(TestContext _)
    {
        var solutionPath = ProclaimerSamplePaths.GetSolutionPath();
        var solution = await MSBuildWorkspace.Create().OpenSolutionAsync(solutionPath);

        var typeSystem = new TypeSystem();
        _dotnetTypes = new DotNetTypes(typeSystem);
        _proclaimerTypes = new ProclaimerTypes(typeSystem);
        var sliceManager = new SliceManager(typeSystem);

        _dotnetContext = new DotNetSolutionContext([solution], _dotnetTypes, sliceManager);

        var builder = new ProclaimerSliceFragmentBuilder(
            _dotnetContext,
            _dotnetTypes,
            _proclaimerTypes,
            sliceManager);

        _fragment = await builder.BuildAsync();
    }

    [TestMethod]
    public async Task Services_Are_Discovered_From_Projects()
    {
        _fragment.Should().NotBeNull();

        var services = (await _fragment!.GetServicesAsync()).ToList();

        services.Should().HaveCount(2);
        services.Select(s => s.ServiceName).Should().BeEquivalentTo(["SampleWebApi", "MinimalWebApi"]);
    }

    [TestMethod]
    public async Task Endpoints_Are_Detected_With_Routes_And_Methods()
    {
        _fragment.Should().NotBeNull();
        _proclaimerTypes.Should().NotBeNull();

        var endpoints = (await _fragment!.GetEndpointsAsync()).ToList();

        endpoints.Should().HaveCount(4);
        endpoints.Should().Contain(e => e.HttpMethod == "GET" && e.Route == "/api/widgets/{id}");
        endpoints.Should().Contain(e => e.HttpMethod == "POST" && e.Route == "/api/widgets/create");
        endpoints.Should().Contain(e => e.HttpMethod == "GET" && e.Route == "/api/minimal/widgets/{id}");
        endpoints.Should().Contain(e => e.HttpMethod == "POST" && e.Route == "/api/minimal/widgets");

        var belongsToService = _fragment.Slice.GetLinkExplorer(_proclaimerTypes!.BelongsToService);
        var services = (await _fragment.GetServicesAsync()).ToDictionary(s => s.ServiceName);
        var serviceNameProvider = _fragment.Slice.GetElementAttributeProviderAsyncCallback(ProclaimerAttributeNames.ServiceName);

        foreach (var endpoint in endpoints)
        {
            var target = await belongsToService.TryGetTargetElementAsync(endpoint.Id);
            target.Should().NotBeNull();

            var serviceName = await serviceNameProvider(target!.Value.Id);
            services.ContainsKey(serviceName).Should().BeTrue();
        }
    }

    [TestMethod]
    public async Task Endpoints_Link_To_Handler_Methods()
    {
        _fragment.Should().NotBeNull();
        _dotnetContext.Should().NotBeNull();
        _proclaimerTypes.Should().NotBeNull();

        var endpoints = (await _fragment!.GetEndpointsAsync()).ToList();
        var callExplorer = _fragment.Slice.GetLinkExplorer(_proclaimerTypes!.Calls);

        var handlerNames = new List<string>();

        foreach (var endpoint in endpoints)
        {
            var targets = await callExplorer.GetTargetElementsAsync(endpoint.Id);
            targets.Should().ContainSingle();

            var handlerSymbol = _dotnetContext!.GetSymbol(targets.Single().Id);
            handlerNames.Add(handlerSymbol.Name);
        }

        handlerNames.Should().BeEquivalentTo(["GetWidget", "CreateWidget", "GetMinimalWidget", "CreateMinimalWidget"]);
    }
}
