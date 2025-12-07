using System.Collections.Immutable;
using Slicito.Abstractions;
using Slicito.Abstractions.Interaction;
using Slicito.Abstractions.Models;
using Slicito.DotNet;

namespace Slicito.Proclaimer;

/// <summary>
/// Controller for visualizing Proclaimer flow graphs in Visual Studio.
/// </summary>
public class ProclaimerFlowGraphController : IController
{
    private readonly DotNetSolutionContext _dotnetContext;
    private readonly ProclaimerTypes _proclaimerTypes;
    private readonly DotNetTypes _dotnetTypes;
    private readonly ISliceManager _sliceManager;
    private readonly ICodeNavigator? _navigator;
    
    private IProclaimerSliceFragment? _proclaimerFragment;
    private ProclaimerFlowService? _flowService;
    private ProclaimerLabelProvider? _labelProvider;
    private ProclaimerFlowGraphBuilder? _graphBuilder;
    
    public ProclaimerFlowGraphController(
        DotNetSolutionContext dotnetContext,
        ProclaimerTypes proclaimerTypes,
        DotNetTypes dotnetTypes,
        ISliceManager sliceManager,
        ICodeNavigator? navigator = null)
    {
        _dotnetContext = dotnetContext;
        _proclaimerTypes = proclaimerTypes;
        _dotnetTypes = dotnetTypes;
        _sliceManager = sliceManager;
        _navigator = navigator;
    }
    
    public async Task<IModel> InitAsync()
    {
        // Build Proclaimer slice
        var builder = new ProclaimerSliceFragmentBuilder(
            _dotnetContext,
            _dotnetTypes,
            _proclaimerTypes,
            _sliceManager);
        
        _proclaimerFragment = await builder.BuildAsync();
        
        // Initialize services
        _flowService = new ProclaimerFlowService(_proclaimerFragment.Slice, _proclaimerTypes);
        _labelProvider = new ProclaimerLabelProvider(_proclaimerFragment.Slice, _proclaimerTypes);
        _graphBuilder = new ProclaimerFlowGraphBuilder(_labelProvider);
        
        // Get all endpoints
        var endpoints = await _proclaimerFragment.Slice.GetRootElementsAsync(_proclaimerTypes.EndpointController);
        var endpointsList = endpoints.ToList();
        
        if (endpointsList.Count == 0)
        {
            // No endpoints found - return empty graph
            return new Graph(
                ImmutableArray<Node>.Empty,
                ImmutableArray<Edge>.Empty);
        }
        
        // Select first endpoint as root (simple heuristic)
        // In a real implementation, this could be user-selectable
        var rootEndpoint = endpointsList[0];
        
        // Compute flow from this endpoint
        var flowRoot = await _flowService.ComputeFlowAsync(rootEndpoint.Id);
        
        // Build graph
        var graph = await _graphBuilder.BuildGraphAsync(flowRoot);
        
        return graph;
    }
    
    public async Task<IModel?> ProcessCommandAsync(Command command)
    {
        if (command.Name == "NavigateTo" &&
            command.Parameters.TryGetValue("Id", out var idString) &&
            _navigator != null)
        {
            var elementId = new ElementId(idString);
            await TryNavigateToAsync(elementId);
        }
        
        return null;
    }
    
    private async Task TryNavigateToAsync(ElementId elementId)
    {
        if (_navigator == null)
        {
            return;
        }
        
        try
        {
            var codeLocationProvider = _proclaimerFragment!.Slice.GetElementAttributeProviderAsyncCallback(
                CommonAttributeNames.CodeLocation);
            var codeLocationString = await codeLocationProvider(elementId);
            var codeLocation = CodeLocation.Parse(codeLocationString);
            
            if (codeLocation != null)
            {
                await _navigator.NavigateToAsync(codeLocation);
            }
        }
        catch
        {
            // Navigation failed - element may not have code location
        }
    }
}
