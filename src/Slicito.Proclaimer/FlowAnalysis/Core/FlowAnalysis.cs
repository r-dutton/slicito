using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Slicito.Proclaimer.FlowAnalysis.Interprocedural;

namespace Slicito.Proclaimer.FlowAnalysis.Core;

public static class FlowAnalysis
{
    private static readonly ConditionalWeakTable<Compilation, ConcurrentDictionary<IMethodSymbol, MethodFlowContext>> ContextCache = new();
    private static readonly ConditionalWeakTable<Compilation, ConcurrentDictionary<IMethodSymbol, MethodFlowAnalysis>> AnalysisCache = new();

    public static MethodFlowContext GetOrCreateMethodContext(
        Compilation compilation,
        IMethodSymbol method,
        CancellationToken cancellationToken = default)
    {
        var cache = ContextCache.GetValue(
            compilation,
            static _ => new ConcurrentDictionary<IMethodSymbol, MethodFlowContext>(SymbolEqualityComparer.Default));

        return cache.GetOrAdd(
            method,
            static (symbol, state) => CreateContext(state.Compilation, symbol, state.CancellationToken),
            (Compilation: compilation, CancellationToken: cancellationToken));
    }

    public static MethodFlowAnalysis GetOrCreateMethodAnalysis(
        Compilation compilation,
        IMethodSymbol method,
        InterproceduralSettings settings,
        CancellationToken cancellationToken = default)
    {
        var cache = AnalysisCache.GetValue(
            compilation,
            static _ => new ConcurrentDictionary<IMethodSymbol, MethodFlowAnalysis>(SymbolEqualityComparer.Default));

        return cache.GetOrAdd(
            method,
            static (symbol, state) =>
            {
                var context = GetOrCreateMethodContext(state.Compilation, symbol, state.CancellationToken);
                return new MethodFlowAnalysis(context);
            },
            (Compilation: compilation, Settings: settings, CancellationToken: cancellationToken));
    }

    private static MethodFlowContext CreateContext(
        Compilation compilation,
        IMethodSymbol method,
        CancellationToken cancellationToken)
    {
        var declaration = method.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(cancellationToken);
        if (declaration is null)
        {
            return new MethodFlowContext(compilation, method, null, null, null, null);
        }

        var semanticModel = compilation.GetSemanticModel(declaration.SyntaxTree);
        var controlFlow = ControlFlowGraph.Create(declaration, semanticModel, cancellationToken);
        var operation = controlFlow is null
            ? semanticModel.GetOperation(declaration, cancellationToken) ?? semanticModel.GetOperation(declaration.Parent, cancellationToken)
            : null;

        return new MethodFlowContext(compilation, method, declaration, semanticModel, controlFlow, operation);
    }
}
