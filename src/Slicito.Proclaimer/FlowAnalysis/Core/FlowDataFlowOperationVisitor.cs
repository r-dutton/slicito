using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Slicito.Proclaimer.FlowAnalysis.Dependencies;

namespace Slicito.Proclaimer.FlowAnalysis.Core;

public abstract class FlowDataFlowOperationVisitor
{
    protected readonly Compilation Compilation;
    protected readonly SemanticModel Model;
    protected readonly FlowPointsToFacade PointsTo;
    protected readonly FlowValueContentFacade ValueContent;

    protected FlowDataFlowOperationVisitor(
        Compilation compilation, SemanticModel model,
        FlowPointsToFacade pointsTo, FlowValueContentFacade valueContent)
    {
        Compilation = compilation;
        Model = model;
        PointsTo = pointsTo;
        ValueContent = valueContent;
    }

    public virtual void Visit(ControlFlowGraph graph)
    {
        foreach (var block in graph.Blocks)
        {
            Visit(block);
        }
    }

    public virtual void Visit(BasicBlock block)
    {
        foreach (var operation in block.Operations)
        {
            Visit(operation);
        }

        if (block.BranchValue is { } branchOperation)
        {
            Visit(branchOperation);
        }
    }

    public virtual void Visit(IOperation op)
    {
        switch (op)
        {
            case IInvocationOperation invocation:
                VisitInvocation(invocation);
                break;
            case IAssignmentOperation assignment:
                VisitAssignment(assignment);
                break;
            default:
                foreach (var child in op.ChildOperations)
                {
                    Visit(child);
                }
                break;
        }
    }

    protected virtual void VisitAssignment(IAssignmentOperation op)
    {
        foreach (var child in op.ChildOperations)
        {
            Visit(child);
        }
    }

    protected virtual void VisitInvocation(IInvocationOperation op)
    {
        foreach (var argument in op.Arguments)
        {
            Visit(argument.Value);
        }
    }
}
