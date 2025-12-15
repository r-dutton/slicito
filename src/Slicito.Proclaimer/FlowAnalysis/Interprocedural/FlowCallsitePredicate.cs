using Microsoft.CodeAnalysis.Operations;

namespace Slicito.Proclaimer.FlowAnalysis.Interprocedural;

public delegate bool FlowCallsitePredicate(IInvocationOperation invocation);
