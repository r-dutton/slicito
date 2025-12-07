using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;

namespace Slicito.Proclaimer.FlowAnalysis.Interprocedural;

public readonly struct InterproceduralSettings
{
    public InterproceduralSettings(
        InterproceduralAnalysisKind kind,
        int maxCallChainLength,
        int maxLambdaOrLocalFunctionDepth)
    {
        Kind = kind;
        MaxCallChainLength = maxCallChainLength;
        MaxLambdaOrLocalFunctionDepth = maxLambdaOrLocalFunctionDepth;
    }

    public InterproceduralAnalysisKind Kind { get; }

    public int MaxCallChainLength { get; }

    public int MaxLambdaOrLocalFunctionDepth { get; }
}
