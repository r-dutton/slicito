using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Slicito.Abstractions;
using Slicito.Abstractions.Facts;
using Slicito.Common.Implementation;
using Slicito.DotNet;

namespace Slicito.Proclaimer.Analyzers.Advanced;

public record EfOperationAnalysisResult(ImmutableArray<EfOperationInvocation> Operations);

public record EfOperationInvocation(
    ElementId MethodId,
    ElementId OperationId,
    string Operation,
    ITypeSymbol? EntityType,
    ITypeSymbol? ContextType,
    int LineNumber);

public class EfOperationAnalyzer
{
    private readonly DotNetSolutionContext _dotnetContext;
    private readonly DotNetTypes _dotnetTypes;

    public EfOperationAnalyzer(DotNetSolutionContext dotnetContext, DotNetTypes dotnetTypes)
    {
        _dotnetContext = dotnetContext;
        _dotnetTypes = dotnetTypes;
    }

    public async Task<EfOperationAnalysisResult> AnalyzeMethodAsync(ElementId methodId)
    {
        var operations = ImmutableArray.CreateBuilder<EfOperationInvocation>();

        var procedureElement = new SimpleProcedureElement(methodId);
        var methodOperations = await _dotnetContext.TypedSliceFragment.GetOperationsAsync(procedureElement);

        foreach (var operation in methodOperations)
        {
            var operationType = _dotnetContext.Slice.GetElementType(operation.Id);
            if (!operationType.Value.IsSubsetOfOrEquals(_dotnetTypes.Call.Value))
                continue;

            var symbol = _dotnetContext.GetSymbol(operation.Id);
            if (symbol is not IMethodSymbol methodSymbol)
                continue;

            if (IsEntityFrameworkInvocation(methodSymbol))
            {
                var entityType = GetEntityType(methodSymbol);
                if (entityType is not null)
                {
                    var contextType = GetContextSymbol(methodSymbol);
                    var efOperation = DetermineEfOperation(methodSymbol.Name);
                    var lineNumber = GetLineNumber(operation.Id);

                    operations.Add(new EfOperationInvocation(
                        methodId,
                        operation.Id,
                        efOperation,
                        entityType,
                        contextType,
                        lineNumber));
                }
            }
        }

        return new EfOperationAnalysisResult(operations.ToImmutable());
    }

    private bool IsEntityFrameworkInvocation(IMethodSymbol method)
    {
        var containing = method.ContainingType;
        if (containing is null)
            return false;

        var display = containing.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
        
        if (display.Contains("EntityFrameworkCore", StringComparison.Ordinal))
            return true;

        if (display.Contains("DbSet", StringComparison.Ordinal))
            return true;

        if (display.Contains("DbContext", StringComparison.Ordinal))
            return true;

        var instanceType = method.ReceiverType;
        if (instanceType is INamedTypeSymbol instanceNamed)
        {
            var instanceDisplay = instanceNamed.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
            return instanceDisplay.Contains("EntityFrameworkCore", StringComparison.Ordinal) ||
                   instanceNamed.Name.Contains("DbSet", StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    private ITypeSymbol? GetEntityType(IMethodSymbol method)
    {
        if (method.IsGenericMethod &&
            method.TypeArguments.Length == 1 &&
            string.Equals(method.Name, "Set", StringComparison.OrdinalIgnoreCase))
        {
            return method.TypeArguments[0];
        }

        if (TryExtractEntity(method.ReceiverType, out var entity))
        {
            return entity;
        }

        if (method.IsExtensionMethod && method.Parameters.Length > 0)
        {
            if (TryExtractEntity(method.Parameters[0].Type, out entity))
            {
                return entity;
            }
        }

        if (TryExtractEntity(method.ReturnType, out entity))
        {
            return entity;
        }

        return null;
    }

    private bool TryExtractEntity(ITypeSymbol? symbol, out ITypeSymbol entity)
    {
        entity = default!;
        if (symbol is not INamedTypeSymbol named)
            return false;

        if (named.IsGenericType)
        {
            var constructed = named.ConstructedFrom?.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat) ?? string.Empty;
            
            if ((string.Equals(named.Name, "DbSet", StringComparison.OrdinalIgnoreCase) ||
                 constructed.Contains("EntityFrameworkCore.DbSet", StringComparison.Ordinal)) &&
                named.TypeArguments.Length == 1)
            {
                entity = named.TypeArguments[0];
                return true;
            }

            if ((string.Equals(named.Name, "IQueryable", StringComparison.OrdinalIgnoreCase) ||
                 constructed.StartsWith("System.Linq.IQueryable", StringComparison.Ordinal)) &&
                named.TypeArguments.Length == 1)
            {
                entity = named.TypeArguments[0];
                return true;
            }

            if ((string.Equals(named.Name, "Task", StringComparison.OrdinalIgnoreCase) ||
                 constructed.StartsWith("System.Threading.Tasks.Task", StringComparison.Ordinal)) &&
                named.TypeArguments.Length == 1)
            {
                if (TryExtractEntity(named.TypeArguments[0], out entity))
                {
                    return true;
                }
            }

            if (named.TypeArguments.Length == 1 &&
                (constructed.Contains("IAsyncEnumerable", StringComparison.Ordinal) ||
                 constructed.Contains("IAsyncQueryable", StringComparison.Ordinal)))
            {
                if (TryExtractEntity(named.TypeArguments[0], out entity))
                {
                    return true;
                }
            }
        }

        foreach (var iface in named.AllInterfaces)
        {
            if (TryExtractEntity(iface, out entity))
            {
                return true;
            }
        }

        if (named.BaseType is not null && TryExtractEntity(named.BaseType, out entity))
        {
            return true;
        }

        return false;
    }

    private ITypeSymbol? GetContextSymbol(IMethodSymbol method)
    {
        if (method.ReceiverType is not null &&
            method.ReceiverType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat).Contains("DbContext", StringComparison.Ordinal))
        {
            return method.ReceiverType;
        }

        if (method.ContainingType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat).Contains("DbContext", StringComparison.Ordinal))
        {
            return method.ContainingType;
        }

        return null;
    }

    private string DetermineEfOperation(string methodName)
    {
        if (string.IsNullOrWhiteSpace(methodName))
            return "query";

        if (methodName.StartsWith("Add", StringComparison.OrdinalIgnoreCase) ||
            methodName.StartsWith("Create", StringComparison.OrdinalIgnoreCase) ||
            methodName.StartsWith("Insert", StringComparison.OrdinalIgnoreCase))
        {
            return "insert";
        }

        if (methodName.StartsWith("Update", StringComparison.OrdinalIgnoreCase) ||
            methodName.StartsWith("Attach", StringComparison.OrdinalIgnoreCase) ||
            methodName.StartsWith("Save", StringComparison.OrdinalIgnoreCase))
        {
            return "update";
        }

        if (methodName.StartsWith("Remove", StringComparison.OrdinalIgnoreCase) ||
            methodName.StartsWith("Delete", StringComparison.OrdinalIgnoreCase))
        {
            return "delete";
        }

        return "query";
    }

    private int GetLineNumber(ElementId operationId)
    {
        var symbol = _dotnetContext.GetSymbol(operationId);
        if (symbol?.Locations.FirstOrDefault() is { } location && location.IsInSource)
        {
            return location.GetLineSpan().StartLinePosition.Line + 1;
        }
        return 0;
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
