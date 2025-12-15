using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Threading;
using Analyzer.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;
using Slicito.Proclaimer.FlowAnalysis.Interprocedural;
using FlowAnalysisCore = Slicito.Proclaimer.FlowAnalysis.Core.FlowAnalysis;

namespace Slicito.Proclaimer.FlowAnalysis.Dependencies;

public sealed class FlowPointsToFacade
{
    private static readonly AnalyzerOptions EmptyAnalyzerOptions = new(ImmutableArray<AdditionalText>.Empty);

    private static readonly DiagnosticDescriptor FlowAnalysisRule = new(
        id: "SLICITO0002",
        title: "Slicito Proclaimer points-to analysis",
        messageFormat: "Slicito Proclaimer points-to analysis placeholder",
        category: "Slicito.Proclaimer",
        defaultSeverity: DiagnosticSeverity.Hidden,
        isEnabledByDefault: true);

    private static readonly PointsToAnalysisResult? PlaceholderResult = null;

    private static readonly InterproceduralAnalysisPredicate NoOpPredicate = new(
        static _ => false,
        static _ => false,
        static _ => false);

    private readonly ConcurrentDictionary<AnalysisCacheKey, Lazy<PointsToAnalysisResult?>> _analysisCache = new();

    public FlowPointsToFacade(
        InterproceduralSettings configuration,
        FlowCallsitePredicate pruningPredicate)
    {
        Configuration = configuration;
        PruningPredicate = pruningPredicate;
    }

    public InterproceduralSettings Configuration { get; }

    public FlowCallsitePredicate PruningPredicate { get; }

    public bool TryGetAbstractValue(IOperation operation, out PointsToAbstractValue value)
    {
        value = null!;

        if (operation is null)
        {
            return false;
        }

        if (operation.SemanticModel is not { } model)
        {
            return false;
        }

        var owningSymbol = model.GetEnclosingSymbol(operation.Syntax.SpanStart);
        if (owningSymbol is null)
        {
            return false;
        }

        if (!TryGetAnalysis(owningSymbol, model, operation.Syntax, out var analysis) || analysis is null)
        {
            return false;
        }

        value = analysis[operation];
        return value.Kind != PointsToAbstractValueKind.Invalid;
    }

    public ImmutableArray<ITypeSymbol> GetLocationTypes(PointsToAbstractValue value)
    {
        if (value is null || value.Kind != PointsToAbstractValueKind.KnownLocations)
        {
            return ImmutableArray<ITypeSymbol>.Empty;
        }

        var builder = ImmutableArray.CreateBuilder<ITypeSymbol>();
        foreach (var location in value.Locations)
        {
            if (location.LocationType is { } type)
            {
                builder.Add(type);
            }
        }

        return builder.ToImmutable();
    }

    public ImmutableArray<ITypeSymbol> TryGetLocationTypes(IOperation operation)
    {
        return TryGetAbstractValue(operation, out var value)
            ? GetLocationTypes(value)
            : ImmutableArray<ITypeSymbol>.Empty;
    }

    private bool TryGetAnalysis(
        ISymbol owningSymbol,
        SemanticModel contextModel,
        SyntaxNode contextSyntax,
        out PointsToAnalysisResult? analysis)
    {
        analysis = null;
        var declaration = FindDeclarationSyntax(owningSymbol, contextSyntax);
        if (declaration is null)
        {
            return false;
        }

        var key = new AnalysisCacheKey(declaration.SyntaxTree, declaration.Span);
        var lazy = _analysisCache.GetOrAdd(key, _ => new Lazy<PointsToAnalysisResult?>(() =>
            ComputeAnalysis(owningSymbol, declaration, contextModel), LazyThreadSafetyMode.ExecutionAndPublication));

        analysis = lazy.Value;
        return analysis is not null;
    }

    private PointsToAnalysisResult? ComputeAnalysis(
        ISymbol owningSymbol,
        SyntaxNode declarationSyntax,
        SemanticModel contextModel)
    {
        try
        {
            var compilation = contextModel.Compilation;

            if (owningSymbol is IMethodSymbol methodSymbol)
            {
                var methodAnalysis = FlowAnalysisCore.GetOrCreateMethodAnalysis(
                    compilation,
                    methodSymbol,
                    Configuration,
                    CancellationToken.None);

                if (!methodAnalysis.PointsToComputed)
                {
                    var methodContext = methodAnalysis.Context;
                    var declaration = methodContext.Declaration ?? declarationSyntax;
                    var methodSemanticModel = methodContext.SemanticModel ?? compilation.GetSemanticModel(declaration.SyntaxTree);
                    var controlFlow = methodContext.ControlFlowGraph ?? ControlFlowGraph.Create(declaration, methodSemanticModel, CancellationToken.None);

                    PointsToAnalysisResult? computed = PlaceholderResult;
                    if (controlFlow is not null)
                    {
                        computed = RunPointsToAnalysis(controlFlow, owningSymbol, compilation);
                    }

                    methodAnalysis.PointsToAnalysis = computed;
                    methodAnalysis.PointsToComputed = true;
                }

                return methodAnalysis.PointsToAnalysis;
            }

            var semanticModel = compilation.GetSemanticModel(declarationSyntax.SyntaxTree);
            var cfg = ControlFlowGraph.Create(declarationSyntax, semanticModel, CancellationToken.None);
            return cfg is null ? null : RunPointsToAnalysis(cfg, owningSymbol, compilation);
        }
        catch (Exception ex) when (IsBenignAnalysisException(ex))
        {
            return null;
        }
    }

    private PointsToAnalysisResult? RunPointsToAnalysis(
        ControlFlowGraph controlFlowGraph,
        ISymbol owningSymbol,
        Compilation compilation)
    {
        var wellKnownProvider = WellKnownTypeProvider.GetOrCreate(compilation);
        var settings = Configuration;

        var interproceduralConfiguration = InterproceduralAnalysisConfiguration.Create(
            EmptyAnalyzerOptions,
            ImmutableArray.Create(FlowAnalysisRule),
            controlFlowGraph,
            compilation,
            settings.Kind,
            (uint)Math.Max(0, settings.MaxCallChainLength),
            (uint)Math.Max(0, settings.MaxLambdaOrLocalFunctionDepth));

        return PointsToAnalysis.TryGetOrComputeResult(
            controlFlowGraph,
            owningSymbol,
            EmptyAnalyzerOptions,
            wellKnownProvider,
            PointsToAnalysisKind.PartialWithoutTrackingFieldsAndProperties,
            interproceduralConfiguration,
            NoOpPredicate,
            false,
            false,
            false);
    }

    private static SyntaxNode? FindDeclarationSyntax(ISymbol symbol, SyntaxNode contextSyntax)
    {
        foreach (var reference in symbol.DeclaringSyntaxReferences)
        {
            var syntax = reference.GetSyntax();
            if (syntax.SyntaxTree == contextSyntax.SyntaxTree)
            {
                return syntax;
            }
        }

        return symbol.DeclaringSyntaxReferences.Length > 0
            ? symbol.DeclaringSyntaxReferences[0].GetSyntax()
            : null;
    }

    private static bool IsBenignAnalysisException(Exception exception)
        => exception is InvalidOperationException or NotSupportedException or OperationCanceledException;

    private readonly struct AnalysisCacheKey : IEquatable<AnalysisCacheKey>
    {
        public AnalysisCacheKey(SyntaxTree tree, TextSpan span)
        {
            Tree = tree;
            Span = span;
        }

        public SyntaxTree Tree { get; }

        public TextSpan Span { get; }

        public bool Equals(AnalysisCacheKey other)
            => ReferenceEquals(Tree, other.Tree) && Span.Equals(other.Span);

        public override bool Equals(object? obj)
            => obj is AnalysisCacheKey other && Equals(other);

        public override int GetHashCode()
            => HashCode.Combine(Tree, Span.Start, Span.Length);
    }
}
