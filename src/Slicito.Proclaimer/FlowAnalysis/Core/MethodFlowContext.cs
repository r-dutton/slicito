using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Slicito.Proclaimer.FlowAnalysis.Core;

public sealed class MethodFlowContext
{
    public MethodFlowContext(
        Compilation compilation,
        IMethodSymbol method,
        SyntaxNode? declaration,
        SemanticModel? semanticModel,
        ControlFlowGraph? controlFlowGraph,
        IOperation? rootOperation)
    {
        Compilation = compilation;
        Method = method;
        Declaration = declaration;
        SemanticModel = semanticModel;
        ControlFlowGraph = controlFlowGraph;
        RootOperation = rootOperation;
    }

    public Compilation Compilation { get; }

    public IMethodSymbol Method { get; }

    public SyntaxNode? Declaration { get; }

    public SemanticModel? SemanticModel { get; }

    public ControlFlowGraph? ControlFlowGraph { get; }

    public IOperation? RootOperation { get; }

    public void Accept(FlowDataFlowOperationVisitor visitor)
    {
        if (ControlFlowGraph is not null)
        {
            visitor.Visit(ControlFlowGraph);
            return;
        }

        if (RootOperation is not null)
        {
            visitor.Visit(RootOperation);
        }
    }
}
