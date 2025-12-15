using System.Collections.Immutable;
using System.Linq;
using Slicito.Abstractions;
using Slicito.Abstractions.Models;
using Slicito.DotNet;
using Slicito.Proclaimer;

namespace Controllers;

/// <summary>
/// Sample controller demonstrating Proclaimer flow visualization.
/// This controller discovers ASP.NET endpoints and visualizes their flows.
/// </summary>
public class ProclaimerFlowSample
{
    private readonly DotNetSolutionContext _dotnetContext;
    private readonly DotNetTypes _dotnetTypes;
    private readonly ProclaimerTypes _proclaimerTypes;
    private readonly ISliceManager _sliceManager;
    
    public ProclaimerFlowSample(
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
    /// Creates a Proclaimer flow graph controller for Visual Studio integration.
    /// </summary>
    public ProclaimerFlowGraphController CreateController()
    {
        return new ProclaimerFlowGraphController(
            _dotnetContext,
            _proclaimerTypes,
            _dotnetTypes,
            _sliceManager);
    }
    
    /// <summary>
    /// Analyzes endpoints and returns a flow graph.
    /// </summary>
    public async Task<Graph> AnalyzeEndpointsAsync()
    {
        // Build Proclaimer slice
        var builder = new ProclaimerSliceFragmentBuilder(
            _dotnetContext,
            _dotnetTypes,
            _proclaimerTypes,
            _sliceManager);
        
        var fragment = await builder.BuildAsync();
        
        // Get all discovered endpoints
        var endpoints = await fragment.Slice.GetRootElementsAsync(_proclaimerTypes.Endpoint);
        var endpointsList = endpoints.ToList();

        if (endpointsList.Count == 0)
        {
            // No endpoints found
            return new Graph(ImmutableArray<Node>.Empty, ImmutableArray<Edge>.Empty);
        }
        
        // Analyze flow from first endpoint
        var flowService = new ProclaimerFlowService(fragment.Slice, _proclaimerTypes);
        var flowRoot = await flowService.ComputeFlowAsync(endpointsList[0].Id);
        
        // Build graph for visualization
        var labelProvider = new ProclaimerLabelProvider(fragment.Slice, _proclaimerTypes);
        var graphBuilder = new ProclaimerFlowGraphBuilder(labelProvider);
        
        return await graphBuilder.BuildGraphAsync(flowRoot);
    }
}
