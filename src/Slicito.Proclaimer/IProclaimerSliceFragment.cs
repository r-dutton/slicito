using Slicito.Abstractions;
using Slicito.Proclaimer.Facts;

namespace Slicito.Proclaimer;

/// <summary>
/// Provides typed access to Proclaimer-specific slice elements.
/// Extends the base DotNet slice with Proclaimer-specific facts.
/// </summary>
public interface IProclaimerSliceFragment : ITypedSliceFragment
{
    ValueTask<IEnumerable<IProclaimerServiceElement>> GetServicesAsync();

    ValueTask<IEnumerable<IProclaimerEndpointElement>> GetEndpointsAsync();

    ValueTask<IEnumerable<IProclaimerEndpointElement>> GetEndpointsAsync(IProclaimerServiceElement service);
}
