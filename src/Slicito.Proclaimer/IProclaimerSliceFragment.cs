using Slicito.Abstractions;

namespace Slicito.Proclaimer;

/// <summary>
/// Provides typed access to Proclaimer-specific slice elements.
/// Extends the base DotNet slice with Proclaimer-specific facts.
/// </summary>
public interface IProclaimerSliceFragment : ITypedSliceFragment
{
    // For initial implementation, we'll discover endpoints and HTTP clients from the DotNet slice
    // More specialized element queries will be added as we port more analyzers
}
