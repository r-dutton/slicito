using System;
using System.Collections.Generic;
using Slicito.Abstractions;
using Slicito.DotNet;

namespace Slicito.Proclaimer;

/// <summary>
/// Builds a Proclaimer slice fragment by analyzing a DotNet solution.
/// This class currently establishes the canonical schema; discovery logic is added in subsequent tasks.
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
    /// Builds the Proclaimer slice using the canonical schema. Element discovery will be layered on in later tasks.
    /// </summary>
    public Task<IProclaimerSliceFragment> BuildAsync()
    {
        var builder = _sliceManager.CreateBuilder();

        static ValueTask<IEnumerable<ISliceBuilder.PartialElementInfo>> EmptyElements()
        {
            return new ValueTask<IEnumerable<ISliceBuilder.PartialElementInfo>>(Array.Empty<ISliceBuilder.PartialElementInfo>());
        }

        // Register empty root sets so the schema contains canonical element types even before discovery is implemented.
        builder
            .AddRootElements(_proclaimerTypes.Service, EmptyElements)
            .AddRootElements(_proclaimerTypes.Endpoint, EmptyElements)
            .AddRootElements(_proclaimerTypes.HttpClient, EmptyElements)
            .AddRootElements(_proclaimerTypes.Repository, EmptyElements)
            .AddRootElements(_proclaimerTypes.Database, EmptyElements)
            .AddRootElements(_proclaimerTypes.Queue, EmptyElements)
            .AddRootElements(_proclaimerTypes.Topic, EmptyElements)
            .AddRootElements(_proclaimerTypes.BackgroundService, EmptyElements);

        var slice = builder.Build();

        return Task.FromResult<IProclaimerSliceFragment>(new ProclaimerSliceFragment(slice, _proclaimerTypes));
    }
}

/// <summary>
/// Implementation of IProclaimerSliceFragment.
/// </summary>
internal record ProclaimerSliceFragment(ISlice Slice, ProclaimerTypes Types) : IProclaimerSliceFragment;
