using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Slicito.Abstractions;
using Slicito.Abstractions.Facts;
using Slicito.Common.Implementation;
using Slicito.DotNet;

namespace Slicito.Proclaimer.Analyzers.Advanced;

/// <summary>
/// Comprehensive operation analyzer that walks IOperation trees to detect multiple patterns.
/// This is inspired by TheProclaimer's multiple operation visitors but unified for Slicito.
/// Ported from TheProclaimer's ControllerOperationVisitor, EfOperationVisitor, and MappingOperationVisitor.
/// 
/// LIMITATIONS vs TheProclaimer:
/// - Does not use FlowDataFlowOperationVisitor base class
/// - Does not have access to FlowPointsToFacade or FlowValueContentFacade
/// - EF entity type extraction is simplified (no deep type flow analysis)
/// - Mapping profile detection not implemented
/// 
/// TODO: Add CreateMap configuration detection for AutoMapper profiles
/// TODO: Add LINQ query pattern analysis (Where, Select, Include chains)
/// </summary>
public class ComprehensiveOperationAnalyzer
{
    private readonly DotNetSolutionContext _dotnetContext;
    private readonly DotNetTypes _dotnetTypes;

    public ComprehensiveOperationAnalyzer(DotNetSolutionContext dotnetContext, DotNetTypes dotnetTypes)
    {
        _dotnetContext = dotnetContext;
        _dotnetTypes = dotnetTypes;
    }

    /// <summary>
    /// Analyzes all operations across all methods in the solution to detect various patterns.
    /// </summary>
    public async Task<ComprehensiveAnalysisResult> AnalyzeAllMethodsAsync()
    {
        var cqrsResults = new List<CqrsOperationAnalysisResult>();
        var httpResults = new List<HttpOperationAnalysisResult>();
        var efResults = new List<EfOperationAnalysisResult>();
        var mappingResults = new List<MappingOperationAnalysisResult>();
        var cachingResults = new List<CachingOperationAnalysisResult>();
        var validationResults = new List<ValidationOperationAnalysisResult>();

        // Get all methods from the DotNet slice
        var allMethods = await _dotnetContext.Slice.GetRootElementsAsync(_dotnetTypes.Method);

        foreach (var method in allMethods)
        {
            var visitor = new OperationPatternVisitor(_dotnetContext, _dotnetTypes, method.Id);
            var procedureElement = new SimpleProcedureElement(method.Id);
            var operations = await _dotnetContext.TypedSliceFragment.GetOperationsAsync(procedureElement);

            foreach (var operation in operations)
            {
                var operationType = _dotnetContext.Slice.GetElementType(operation.Id);
                if (!operationType.Value.IsSubsetOfOrEquals(_dotnetTypes.Call.Value))
                    continue;

                var symbol = _dotnetContext.GetSymbol(operation.Id);
                if (symbol is IMethodSymbol methodSymbol)
                {
                    visitor.AnalyzeInvocation(operation.Id, methodSymbol);
                }
            }

            cqrsResults.Add(visitor.GetCqrsResult());
            httpResults.Add(visitor.GetHttpResult());
            efResults.Add(visitor.GetEfResult());
            mappingResults.Add(visitor.GetMappingResult());
            cachingResults.Add(visitor.GetCachingResult());
            validationResults.Add(visitor.GetValidationResult());
        }

        return new ComprehensiveAnalysisResult(
            cqrsResults.ToImmutableArray(),
            httpResults.ToImmutableArray(),
            efResults.ToImmutableArray(),
            mappingResults.ToImmutableArray(),
            cachingResults.ToImmutableArray(),
            validationResults.ToImmutableArray());
    }

    /// <summary>
    /// Visitor that identifies various framework patterns in operations.
    /// </summary>
    private class OperationPatternVisitor
    {
        private readonly DotNetSolutionContext _context;
        private readonly DotNetTypes _types;
        private readonly ElementId _methodId;

        private readonly List<MediatorSendInvocation> _sends = new();
        private readonly List<MediatorPublishInvocation> _publishes = new();
        private readonly List<HttpClientInvocation> _httpCalls = new();
        private readonly List<EfOperationInvocation> _efOps = new();
        private readonly List<MappingInvocation> _mappings = new();
        private readonly List<CacheOperationInvocation> _cacheOps = new();
        private readonly List<ValidationInvocation> _validations = new();

        public OperationPatternVisitor(DotNetSolutionContext context, DotNetTypes types, ElementId methodId)
        {
            _context = context;
            _types = types;
            _methodId = methodId;
        }

        public void AnalyzeInvocation(ElementId operationId, IMethodSymbol method)
        {
            var lineNumber = GetLineNumber(operationId);

            // MediatR Send
            if (method.Name == "Send" && IsMediator(method.ContainingType))
            {
                var requestType = GetGenericArgument(method);
                if (requestType != null)
                {
                    _sends.Add(new MediatorSendInvocation(_methodId, operationId, requestType, lineNumber));
                }
            }
            // MediatR Publish
            else if (method.Name == "Publish" && IsMediator(method.ContainingType))
            {
                var notificationType = GetGenericArgument(method);
                if (notificationType != null)
                {
                    _publishes.Add(new MediatorPublishInvocation(_methodId, operationId, notificationType, lineNumber));
                }
            }
            // HttpClient calls
            else if (IsHttpClient(method.ContainingType) && TryGetHttpVerb(method.Name, out var verb))
            {
                _httpCalls.Add(new HttpClientInvocation(_methodId, operationId, verb, null, lineNumber));
            }
            // Entity Framework DbSet operations
            else if (IsDbSet(method.ContainingType))
            {
                var operation = ClassifyEfOperation(method.Name);
                var entityType = GetDbSetEntityType(method.ContainingType);
                _efOps.Add(new EfOperationInvocation(_methodId, operationId, operation, entityType, lineNumber));
            }
            // AutoMapper
            else if ((method.Name == "Map" || method.Name == "ProjectTo") && IsMapper(method.ContainingType))
            {
                var sourceType = method.Parameters.FirstOrDefault()?.Type;
                var destType = GetGenericArgument(method);
                if (sourceType != null && destType != null)
                {
                    _mappings.Add(new MappingInvocation(_methodId, operationId, sourceType, destType, lineNumber));
                }
            }
            // Cache operations
            else if (IsCache(method.ContainingType))
            {
                var cacheOp = ClassifyCacheOperation(method.Name);
                _cacheOps.Add(new CacheOperationInvocation(_methodId, operationId, cacheOp, null, lineNumber));
            }
            // Validation
            else if (method.Name == "Validate" || method.Name == "ValidateAsync")
            {
                var validatedType = method.Parameters.FirstOrDefault()?.Type;
                if (validatedType != null)
                {
                    _validations.Add(new ValidationInvocation(_methodId, operationId, validatedType, lineNumber));
                }
            }
        }

        public CqrsOperationAnalysisResult GetCqrsResult() =>
            new(_sends.ToImmutableArray(), _publishes.ToImmutableArray());

        public HttpOperationAnalysisResult GetHttpResult() =>
            new(_httpCalls.ToImmutableArray());

        public EfOperationAnalysisResult GetEfResult() =>
            new(_efOps.ToImmutableArray());

        public MappingOperationAnalysisResult GetMappingResult() =>
            new(_mappings.ToImmutableArray());

        public CachingOperationAnalysisResult GetCachingResult() =>
            new(_cacheOps.ToImmutableArray());

        public ValidationOperationAnalysisResult GetValidationResult() =>
            new(_validations.ToImmutableArray());

        private bool IsMediator(ITypeSymbol? type) =>
            type?.Name == "IMediator" && type.ContainingNamespace?.ToDisplayString() == "MediatR";

        private bool IsHttpClient(ITypeSymbol? type) =>
            type?.Name == "HttpClient" && type.ContainingNamespace?.ToDisplayString() == "System.Net.Http";

        private bool IsDbSet(ITypeSymbol? type) =>
            type?.Name == "DbSet" && type.ContainingNamespace?.ToDisplayString() == "Microsoft.EntityFrameworkCore";

        private bool IsMapper(ITypeSymbol? type) =>
            type?.Name == "IMapper" && type.ContainingNamespace?.ToDisplayString() == "AutoMapper";

        private bool IsCache(ITypeSymbol? type) =>
            type?.Name == "IMemoryCache" || type?.Name == "IDistributedCache";

        private ITypeSymbol? GetGenericArgument(IMethodSymbol method) =>
            method.TypeArguments.FirstOrDefault() ?? method.Parameters.FirstOrDefault()?.Type;

        private ITypeSymbol? GetDbSetEntityType(ITypeSymbol? dbSetType) =>
            (dbSetType as INamedTypeSymbol)?.TypeArguments.FirstOrDefault();

        private bool TryGetHttpVerb(string methodName, out string verb)
        {
            verb = methodName switch
            {
                "GetAsync" or "GetStringAsync" or "GetByteArrayAsync" or "GetStreamAsync" => "GET",
                "PostAsync" => "POST",
                "PutAsync" => "PUT",
                "DeleteAsync" => "DELETE",
                "PatchAsync" => "PATCH",
                _ => ""
            };
            return !string.IsNullOrEmpty(verb);
        }

        private string ClassifyEfOperation(string methodName) => methodName switch
        {
            "Add" or "AddAsync" or "AddRange" or "AddRangeAsync" => "Add",
            "Update" or "UpdateRange" => "Update",
            "Remove" or "RemoveRange" => "Remove",
            "Find" or "FindAsync" => "Find",
            "SaveChanges" or "SaveChangesAsync" => "SaveChanges",
            _ => "Query"
        };

        private string ClassifyCacheOperation(string methodName) => methodName switch
        {
            "Get" or "GetAsync" or "TryGetValue" => "Get",
            "Set" or "SetAsync" => "Set",
            "Remove" or "RemoveAsync" => "Remove",
            _ => "Other"
        };

        private int GetLineNumber(ElementId operationId)
        {
            var symbol = _context.GetSymbol(operationId);
            if (symbol?.Locations.FirstOrDefault() is { } location && location.IsInSource)
            {
                return location.GetLineSpan().StartLinePosition.Line + 1;
            }
            return 0;
        }
    }

    private class SimpleProcedureElement : ElementBase, Slicito.DotNet.Facts.ICSharpProcedureElement
    {
        public SimpleProcedureElement(ElementId id) : base(id)
        {
        }

        public string Runtime => Slicito.DotNet.DotNetAttributeValues.Runtime.DotNet;
        public string Language => Slicito.DotNet.DotNetAttributeValues.Language.CSharp;
    }
}

// Result types
public record ComprehensiveAnalysisResult(
    ImmutableArray<CqrsOperationAnalysisResult> CqrsResults,
    ImmutableArray<HttpOperationAnalysisResult> HttpResults,
    ImmutableArray<EfOperationAnalysisResult> EfResults,
    ImmutableArray<MappingOperationAnalysisResult> MappingResults,
    ImmutableArray<CachingOperationAnalysisResult> CachingResults,
    ImmutableArray<ValidationOperationAnalysisResult> ValidationResults);

public record EfOperationAnalysisResult(ImmutableArray<EfOperationInvocation> Operations);
public record EfOperationInvocation(ElementId MethodId, ElementId OperationId, string Operation, ITypeSymbol? EntityType, int LineNumber);

public record MappingOperationAnalysisResult(ImmutableArray<MappingInvocation> Mappings);
public record MappingInvocation(ElementId MethodId, ElementId OperationId, ITypeSymbol SourceType, ITypeSymbol DestinationType, int LineNumber);

public record CachingOperationAnalysisResult(ImmutableArray<CacheOperationInvocation> Operations);
public record CacheOperationInvocation(ElementId MethodId, ElementId OperationId, string Operation, string? Key, int LineNumber);

public record ValidationOperationAnalysisResult(ImmutableArray<ValidationInvocation> Validations);
public record ValidationInvocation(ElementId MethodId, ElementId OperationId, ITypeSymbol ValidatedType, int LineNumber);
