using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Slicito.Abstractions;
using Slicito.Abstractions.Facts;
using Slicito.Common.Implementation;
using Slicito.DotNet;

namespace Slicito.Proclaimer.Analyzers.Advanced;

/// <summary>
/// Analyzes service operations including invocations, field/property references, options usage, and logging.
/// Ported from TheProclaimer's ServiceOperationVisitor.
/// 
/// LIMITATIONS vs TheProclaimer:
/// - Does not use FlowPointsToFacade (requires Roslyn.Analyzers.DataFlow package)
/// - Does not use FlowValueContentFacade (requires Roslyn.Analyzers.DataFlow package)
/// - Field/property reference tracking is basic (no points-to analysis for type resolution)
/// - Service instance resolution is simplified (no scoped service resolution)
/// </summary>
public record ServiceOperationAnalysisResult(
    ImmutableArray<ServiceUsageInvocation> ServiceUsages,
    ImmutableArray<OptionsUsageInvocation> OptionsUsages,
    ImmutableArray<ConfigurationUsageInvocation> ConfigurationUsages,
    ImmutableArray<ValidationCallInvocation> ValidationCalls,
    ImmutableArray<LogInvocation> LogInvocations,
    ImmutableArray<FieldReferenceUsage> FieldReferences,
    ImmutableArray<PropertyReferenceUsage> PropertyReferences);

public record ServiceUsageInvocation(
    ElementId MethodId,
    ElementId OperationId,
    string ServiceType,
    string InvocationMethod,
    int LineNumber);

public record OptionsUsageInvocation(
    ElementId MethodId,
    ElementId OperationId,
    ITypeSymbol OptionsType,
    int LineNumber);

public record ConfigurationUsageInvocation(
    ElementId MethodId,
    ElementId OperationId,
    string? ConfigKey,
    int LineNumber);

public record ValidationCallInvocation(
    ElementId MethodId,
    ElementId OperationId,
    string ValidatorType,
    string ValidationMethod,
    int LineNumber);

public record LogInvocation(
    ElementId MethodId,
    ElementId OperationId,
    string LogLevel,
    int LineNumber);

public record FieldReferenceUsage(
    ElementId MethodId,
    ElementId OperationId,
    string FieldName,
    string FieldType,
    int LineNumber);

public record PropertyReferenceUsage(
    ElementId MethodId,
    ElementId OperationId,
    string PropertyName,
    string PropertyType,
    int LineNumber);

public class ServiceOperationAnalyzer
{
    private readonly DotNetSolutionContext _dotnetContext;
    private readonly DotNetTypes _dotnetTypes;

    public ServiceOperationAnalyzer(DotNetSolutionContext dotnetContext, DotNetTypes dotnetTypes)
    {
        _dotnetContext = dotnetContext;
        _dotnetTypes = dotnetTypes;
    }

    public async Task<ServiceOperationAnalysisResult> AnalyzeMethodAsync(ElementId methodId)
    {
        var serviceUsages = ImmutableArray.CreateBuilder<ServiceUsageInvocation>();
        var optionsUsages = ImmutableArray.CreateBuilder<OptionsUsageInvocation>();
        var configUsages = ImmutableArray.CreateBuilder<ConfigurationUsageInvocation>();
        var validationCalls = ImmutableArray.CreateBuilder<ValidationCallInvocation>();
        var logInvocations = ImmutableArray.CreateBuilder<LogInvocation>();
        var fieldReferences = ImmutableArray.CreateBuilder<FieldReferenceUsage>();
        var propertyReferences = ImmutableArray.CreateBuilder<PropertyReferenceUsage>();

        var procedureElement = new SimpleProcedureElement(methodId);
        var operations = await _dotnetContext.TypedSliceFragment.GetOperationsAsync(procedureElement);

        foreach (var operation in operations)
        {
            var operationType = _dotnetContext.Slice.GetElementType(operation.Id);
            
            // TODO: Add support for field and property reference operations
            // Currently only processes Call operations due to Slicito's operation type system
            // TheProclaimer processes IFieldReferenceOperation and IPropertyReferenceOperation
            // via overriding Visit(IOperation) method in FlowDataFlowOperationVisitor
            
            if (!operationType.Value.IsSubsetOfOrEquals(_dotnetTypes.Call.Value))
                continue;

            var symbol = _dotnetContext.GetSymbol(operation.Id);
            if (symbol is not IMethodSymbol methodSymbol)
                continue;

            var receiver = methodSymbol.ReceiverType ?? methodSymbol.ContainingType;
            var lineNumber = GetLineNumber(operation.Id);

            if (IsValidatorCall(methodSymbol))
            {
                var validatorType = receiver?.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat) ?? "";
                if (!string.IsNullOrEmpty(validatorType))
                {
                    validationCalls.Add(new ValidationCallInvocation(
                        methodId, operation.Id, validatorType, methodSymbol.Name, lineNumber));
                }
            }
            else if (IsLoggerType(receiver))
            {
                var logLevel = ExtractLogLevel(methodSymbol.Name);
                if (logLevel is not null)
                {
                    logInvocations.Add(new LogInvocation(
                        methodId, operation.Id, logLevel, lineNumber));
                }
            }
            else if (IsCacheService(receiver))
            {
                // Cache operations handled by CrossCuttingAnalyzers
            }
            else if (TryResolveOptionsType(receiver, out var optionsType))
            {
                optionsUsages.Add(new OptionsUsageInvocation(
                        methodId, operation.Id, optionsType, lineNumber));
            }
            else if (IsConfigurationAccess(methodSymbol))
            {
                // TODO: Extract configuration key from string literal
                // Requires FlowValueContentFacade for constant propagation
                var configKey = ExtractConfigurationKey(methodSymbol);
                configUsages.Add(new ConfigurationUsageInvocation(
                    methodId, operation.Id, configKey, lineNumber));
            }
            else if (IsServiceInvocation(methodSymbol, receiver))
            {
                var serviceType = receiver?.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat) ?? "";
                if (!string.IsNullOrEmpty(serviceType))
                {
                    serviceUsages.Add(new ServiceUsageInvocation(
                        methodId, operation.Id, serviceType, methodSymbol.Name, lineNumber));
                }
            }
        }

        return new ServiceOperationAnalysisResult(
            serviceUsages.ToImmutable(),
            optionsUsages.ToImmutable(),
            configUsages.ToImmutable(),
            validationCalls.ToImmutable(),
            logInvocations.ToImmutable(),
            fieldReferences.ToImmutable(),
            propertyReferences.ToImmutable());
    }

    private bool IsValidatorCall(IMethodSymbol method)
    {
        return method.Name == "Validate" || method.Name == "ValidateAsync" ||
               method.ContainingType?.Name.IndexOf("Validator", StringComparison.OrdinalIgnoreCase) >= 0 == true;
    }

    private bool IsLoggerType(ITypeSymbol? type)
    {
        var typeName = type?.Name ?? "";
        return typeName.IndexOf("ILogger", StringComparison.OrdinalIgnoreCase) >= 0 ||
               typeName.IndexOf("Logger", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private string? ExtractLogLevel(string methodName)
    {
        if (methodName.IndexOf("Debug", StringComparison.OrdinalIgnoreCase) >= 0) return "Debug";
        if (methodName.IndexOf("Info", StringComparison.OrdinalIgnoreCase) >= 0) return "Information";
        if (methodName.IndexOf("Warn", StringComparison.OrdinalIgnoreCase) >= 0) return "Warning";
        if (methodName.IndexOf("Error", StringComparison.OrdinalIgnoreCase) >= 0) return "Error";
        if (methodName.IndexOf("Critical", StringComparison.OrdinalIgnoreCase) >= 0 ||
            methodName.IndexOf("Fatal", StringComparison.OrdinalIgnoreCase) >= 0) return "Critical";
        if (methodName.IndexOf("Trace", StringComparison.OrdinalIgnoreCase) >= 0) return "Trace";
        
        return null;
    }

    private bool IsCacheService(ITypeSymbol? type)
    {
        var display = type?.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat) ?? "";
        return display.IndexOf("IMemoryCache", StringComparison.OrdinalIgnoreCase) >= 0 ||
               display.IndexOf("IDistributedCache", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private bool TryResolveOptionsType(ITypeSymbol? type, out ITypeSymbol optionsType)
    {
        optionsType = default!;
        
        if (type is not INamedTypeSymbol namedType)
            return false;

        var display = namedType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
        if (display.IndexOf("IOptions", StringComparison.OrdinalIgnoreCase) < 0)
            return false;

        if (namedType.TypeArguments.Length > 0)
        {
            optionsType = namedType.TypeArguments[0];
            return true;
        }

        return false;
    }

    private bool IsConfigurationAccess(IMethodSymbol method)
    {
        var receiver = method.ReceiverType ?? method.ContainingType;
        var display = receiver?.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat) ?? "";
        
        return display.IndexOf("IConfiguration", StringComparison.OrdinalIgnoreCase) >= 0 ||
               (method.Name == "get_Item" && display.IndexOf("Configuration", StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private string? ExtractConfigurationKey(IMethodSymbol method)
    {
        // This would require value content analysis to extract string constants
        // For now, return null as we don't have that infrastructure in Slicito
        return null;
    }

    private bool IsServiceInvocation(IMethodSymbol method, ITypeSymbol? receiver)
    {
        if (receiver is null)
            return false;

        // Consider it a service if it's not a framework type and not a primitive
        var display = receiver.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
        
        // Exclude framework types
        if (display.StartsWith("System.", StringComparison.Ordinal) ||
            display.StartsWith("Microsoft.", StringComparison.Ordinal))
        {
            return false;
        }

        // Exclude primitives and common types
        if (receiver.TypeKind == TypeKind.Enum ||
            receiver.SpecialType != SpecialType.None)
        {
            return false;
        }

        return true;
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
