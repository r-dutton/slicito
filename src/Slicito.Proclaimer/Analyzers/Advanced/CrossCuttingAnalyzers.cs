using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Slicito.Abstractions;
using Slicito.Abstractions.Facts;
using Slicito.Common.Implementation;
using Slicito.DotNet;

namespace Slicito.Proclaimer.Analyzers.Advanced;

/// <summary>
/// Analyzes configuration patterns (IConfiguration, IOptions, appsettings.json).
/// Ported from TheProclaimer's ProjectAnalyzer.Configuration.cs.
/// 
/// LIMITATIONS vs TheProclaimer:
/// - Does not use FlowValueContentFacade for config key extraction
/// - Cannot extract string literal configuration keys (requires value content analysis)
/// 
/// TODO: Add TryExtractConfigKey() using value content analysis
/// TODO: Add appsettings.json parsing support
/// </summary>
public class ConfigurationAnalyzer
{
    private readonly DotNetSolutionContext _dotnetContext;
    private readonly DotNetTypes _dotnetTypes;

    public ConfigurationAnalyzer(DotNetSolutionContext dotnetContext, DotNetTypes dotnetTypes)
    {
        _dotnetContext = dotnetContext;
        _dotnetTypes = dotnetTypes;
    }

    public async Task<ConfigurationAnalysisResult> AnalyzeAllMethodsAsync()
    {
        var configAccesses = new List<ConfigurationAccess>();
        var allMethods = await _dotnetContext.Slice.GetRootElementsAsync(_dotnetTypes.Method);

        foreach (var method in allMethods)
        {
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
                    if (IsConfigurationAccess(methodSymbol))
                    {
                        var key = TryExtractConfigKey(methodSymbol);
                        var lineNumber = GetLineNumber(operation.Id);
                        configAccesses.Add(new ConfigurationAccess(method.Id, operation.Id, key, lineNumber));
                    }
                }
            }
        }

        return new ConfigurationAnalysisResult(configAccesses.ToImmutableArray());
    }

    private bool IsConfigurationAccess(IMethodSymbol method)
    {
        var containingType = method.ContainingType;
        return containingType?.Name == "IConfiguration" ||
               containingType?.Name == "IOptions" ||
               (method.Name == "GetSection" || method.Name == "GetValue");
    }

    private string? TryExtractConfigKey(IMethodSymbol method)
    {
        // TODO: Use value content analysis to extract key from first string parameter
        return null;
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

public record ConfigurationAnalysisResult(ImmutableArray<ConfigurationAccess> Accesses);
public record ConfigurationAccess(ElementId MethodId, ElementId OperationId, string? Key, int LineNumber);

/// <summary>
/// Analyzes dependency injection registration patterns.
/// Ported from TheProclaimer's ProjectAnalyzer.DependencyInjection.cs.
/// 
/// LIMITATIONS vs TheProclaimer:
/// - Factory registration patterns not fully implemented
/// - Service resolution tracking not implemented
/// </summary>
public class DependencyInjectionAnalyzer
{
    private readonly DotNetSolutionContext _dotnetContext;
    private readonly DotNetTypes _dotnetTypes;

    public DependencyInjectionAnalyzer(DotNetSolutionContext dotnetContext, DotNetTypes dotnetTypes)
    {
        _dotnetContext = dotnetContext;
        _dotnetTypes = dotnetTypes;
    }

    public async Task<DependencyInjectionAnalysisResult> AnalyzeAllMethodsAsync()
    {
        var registrations = new List<ServiceRegistration>();
        var allMethods = await _dotnetContext.Slice.GetRootElementsAsync(_dotnetTypes.Method);

        foreach (var method in allMethods)
        {
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
                    if (TryGetServiceRegistration(methodSymbol, out var lifetime, out var serviceType, out var implementationType))
                    {
                        var lineNumber = GetLineNumber(operation.Id);
                        registrations.Add(new ServiceRegistration(method.Id, operation.Id, lifetime, serviceType, implementationType, lineNumber));
                    }
                }
            }
        }

        return new DependencyInjectionAnalysisResult(registrations.ToImmutableArray());
    }

    private bool TryGetServiceRegistration(IMethodSymbol method, out string lifetime, out ITypeSymbol? serviceType, out ITypeSymbol? implementationType)
    {
        lifetime = string.Empty;
        serviceType = null;
        implementationType = null;

        if (method.ContainingType?.Name != "IServiceCollection")
            return false;

        lifetime = method.Name switch
        {
            "AddScoped" => "Scoped",
            "AddTransient" => "Transient",
            "AddSingleton" => "Singleton",
            _ => ""
        };

        if (string.IsNullOrEmpty(lifetime))
            return false;

        // Extract type arguments (service type and implementation type)
        if (method.TypeArguments.Length >= 1)
        {
            serviceType = method.TypeArguments[0];
            implementationType = method.TypeArguments.Length >= 2 ? method.TypeArguments[1] : serviceType;
        }

        return serviceType != null;
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

public record DependencyInjectionAnalysisResult(ImmutableArray<ServiceRegistration> Registrations);
public record ServiceRegistration(ElementId MethodId, ElementId OperationId, string Lifetime, ITypeSymbol? ServiceType, ITypeSymbol? ImplementationType, int LineNumber);

/// <summary>
/// Analyzes messaging patterns (MassTransit, Azure Service Bus, RabbitMQ).
/// Ported from TheProclaimer's MessagingOperationVisitor.
/// 
/// LIMITATIONS vs TheProclaimer:
/// - Does not use FlowPointsToFacade for message type resolution
/// - Consumer/handler linking not implemented
/// </summary>
public class MessagingAnalyzer
{
    private readonly DotNetSolutionContext _dotnetContext;
    private readonly DotNetTypes _dotnetTypes;

    public MessagingAnalyzer(DotNetSolutionContext dotnetContext, DotNetTypes dotnetTypes)
    {
        _dotnetContext = dotnetContext;
        _dotnetTypes = dotnetTypes;
    }

    public async Task<MessagingAnalysisResult> AnalyzeAllMethodsAsync()
    {
        var messagePublications = new List<MessagePublication>();
        var allMethods = await _dotnetContext.Slice.GetRootElementsAsync(_dotnetTypes.Method);

        foreach (var method in allMethods)
        {
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
                    if (TryGetMessagePublication(methodSymbol, out var messageType, out var bus))
                    {
                        var lineNumber = GetLineNumber(operation.Id);
                        messagePublications.Add(new MessagePublication(method.Id, operation.Id, messageType, bus, lineNumber));
                    }
                }
            }
        }

        return new MessagingAnalysisResult(messagePublications.ToImmutableArray());
    }

    private bool TryGetMessagePublication(IMethodSymbol method, out ITypeSymbol? messageType, out string bus)
    {
        messageType = null;
        bus = string.Empty;

        if (method.Name != "Publish" && method.Name != "Send" && method.Name != "PublishAsync" && method.Name != "SendAsync")
            return false;

        var containingType = method.ContainingType;
        var ns = containingType?.ContainingNamespace?.ToDisplayString();

        if (ns?.StartsWith("MassTransit") == true)
        {
            bus = "MassTransit";
            messageType = method.TypeArguments.FirstOrDefault() ?? method.Parameters.FirstOrDefault()?.Type;
            return messageType != null;
        }
        else if (ns?.Contains("Azure.Messaging.ServiceBus") == true)
        {
            bus = "AzureServiceBus";
            messageType = method.Parameters.FirstOrDefault()?.Type;
            return messageType != null;
        }
        else if (ns?.Contains("RabbitMQ") == true)
        {
            bus = "RabbitMQ";
            messageType = method.Parameters.FirstOrDefault()?.Type;
            return messageType != null;
        }

        return false;
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

public record MessagingAnalysisResult(ImmutableArray<MessagePublication> Publications);
public record MessagePublication(ElementId MethodId, ElementId OperationId, ITypeSymbol? MessageType, string Bus, int LineNumber);
