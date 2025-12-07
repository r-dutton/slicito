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
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ValueContentAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;
using Slicito.Proclaimer.FlowAnalysis.Interprocedural;
using FlowAnalysisCore = Slicito.Proclaimer.FlowAnalysis.Core.FlowAnalysis;
using ValueContentAnalysisResult = Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.DataFlowAnalysisResult<Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ValueContentAnalysis.ValueContentBlockAnalysisResult, Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ValueContentAnalysis.ValueContentAbstractValue>;

namespace Slicito.Proclaimer.FlowAnalysis.Dependencies;

public sealed class FlowValueContentFacade
{
    private static readonly AnalyzerOptions EmptyAnalyzerOptions = new(ImmutableArray<AdditionalText>.Empty);

    private static readonly DiagnosticDescriptor FlowAnalysisRule = new(
        id: "SLICITO0001",
        title: "Slicito Proclaimer flow analysis",
        messageFormat: "Slicito Proclaimer flow analysis placeholder",
        category: "Slicito.Proclaimer",
        defaultSeverity: DiagnosticSeverity.Hidden,
        isEnabledByDefault: true);

    private static readonly ValueContentAnalysisResult? PlaceholderResult = null;

    private static readonly InterproceduralAnalysisPredicate NoOpPredicate = new(
        static _ => false,
        static _ => false,
        static _ => false);

    private readonly ConcurrentDictionary<AnalysisCacheKey, Lazy<ValueContentAnalysisResult?>> _analysisCache = new();

    public FlowValueContentFacade(
        InterproceduralSettings configuration,
        FlowCallsitePredicate pruningPredicate)
    {
        Settings = configuration;
        PruningPredicate = pruningPredicate;
    }

    public InterproceduralSettings Settings { get; }

    public FlowCallsitePredicate PruningPredicate { get; }

    public string? TryGetStringValue(IOperation op)
    {
        if (op is null)
        {
            return null;
        }

        if (op.ConstantValue is { HasValue: true, Value: string constant })
        {
            return constant;
        }

        if (op.SemanticModel is not { } model)
        {
            return null;
        }

        var owningSymbol = model.GetEnclosingSymbol(op.Syntax.SpanStart);
        if (owningSymbol is null)
        {
            return null;
        }

        if (!TryGetAnalysis(owningSymbol, model, op.Syntax, out var analysis))
        {
            return null;
        }

        return TryExtractString(analysis, op, out var reconstructed) ? reconstructed : null;
    }

    private bool TryGetAnalysis(
        ISymbol owningSymbol,
        SemanticModel contextModel,
        SyntaxNode contextSyntax,
        out ValueContentAnalysisResult? analysis)
    {
        analysis = null;
        var declaration = FindDeclarationSyntax(owningSymbol, contextSyntax);
        if (declaration is null)
        {
            return false;
        }

        var key = new AnalysisCacheKey(declaration.SyntaxTree, declaration.Span);
        var lazy = _analysisCache.GetOrAdd(key, _ => new Lazy<ValueContentAnalysisResult?>(() =>
            ComputeAnalysis(owningSymbol, declaration, contextModel), LazyThreadSafetyMode.ExecutionAndPublication));

        analysis = lazy.Value;
        return analysis is not null;
    }

    private ValueContentAnalysisResult? ComputeAnalysis(
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
                    Settings,
                    CancellationToken.None);

                if (!methodAnalysis.ValueContentComputed)
                {
                    var methodContext = methodAnalysis.Context;
                    var declaration = methodContext.Declaration ?? declarationSyntax;
                    var methodSemanticModel = methodContext.SemanticModel ?? compilation.GetSemanticModel(declaration.SyntaxTree);
                    var controlFlow = methodContext.ControlFlowGraph ?? ControlFlowGraph.Create(declaration, methodSemanticModel, CancellationToken.None);

                    ValueContentAnalysisResult? computed = PlaceholderResult;
                    if (controlFlow is not null)
                    {
                        computed = RunValueContentAnalysis(controlFlow, owningSymbol, compilation);
                    }

                    methodAnalysis.ValueContentAnalysis = computed;
                    methodAnalysis.ValueContentComputed = true;
                }

                return methodAnalysis.ValueContentAnalysis;
            }

            var semanticModel = compilation.GetSemanticModel(declarationSyntax.SyntaxTree);
            var cfg = ControlFlowGraph.Create(declarationSyntax, semanticModel, CancellationToken.None);
            return cfg is null ? null : RunValueContentAnalysis(cfg, owningSymbol, compilation);
        }
        catch (Exception ex) when (IsBenignAnalysisException(ex))
        {
            return null;
        }
    }

    private ValueContentAnalysisResult? RunValueContentAnalysis(
        ControlFlowGraph controlFlowGraph,
        ISymbol owningSymbol,
        Compilation compilation)
    {
        var wellKnownProvider = WellKnownTypeProvider.GetOrCreate(compilation);

        var settings = Settings;
        var interproceduralConfiguration = InterproceduralAnalysisConfiguration.Create(
            EmptyAnalyzerOptions,
            ImmutableArray.Create(FlowAnalysisRule),
            controlFlowGraph,
            compilation,
            settings.Kind,
            (uint)Math.Max(0, settings.MaxCallChainLength),
            (uint)Math.Max(0, settings.MaxLambdaOrLocalFunctionDepth));

        var pointsToResult = PointsToAnalysis.TryGetOrComputeResult(
            controlFlowGraph,
            owningSymbol,
            EmptyAnalyzerOptions,
            wellKnownProvider,
            PointsToAnalysisKind.PartialWithoutTrackingFieldsAndProperties,
            interproceduralConfiguration,
            NoOpPredicate,
            pessimisticAnalysis: false,
            performCopyAnalysis: false,
            exceptionPathsAnalysis: false);

        var valueContentResult = ValueContentAnalysis.TryGetOrComputeResult(
            controlFlowGraph,
            owningSymbol,
            wellKnownProvider,
            EmptyAnalyzerOptions,
            FlowAnalysisRule,
            PointsToAnalysisKind.PartialWithoutTrackingFieldsAndProperties,
            settings.Kind,
            pessimisticAnalysis: false);

        return valueContentResult;
    }

    private static bool TryExtractString(
        ValueContentAnalysisResult analysis,
        IOperation operation,
        out string? value)
    {
        value = null;
        var abstractValue = analysis[operation];
        if (abstractValue is null)
        {
            return false;
        }

        if (abstractValue.TryGetSingleNonNullLiteral(out string? literal) && literal is not null)
        {
            value = literal;
            return true;
        }

        if (abstractValue.IsLiteralState)
        {
            foreach (var candidate in abstractValue.LiteralValues)
            {
                if (candidate is string text)
                {
                    value = text;
                    return true;
                }

                if (candidate is null)
                {
                    value = null;
                    return true;
                }
            }
        }

        if (Equals(abstractValue, ValueContentAbstractValue.ContainsNullLiteralState))
        {
            value = null;
            return true;
        }

        return false;
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
