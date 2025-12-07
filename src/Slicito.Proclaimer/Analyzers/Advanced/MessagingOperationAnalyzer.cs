using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Slicito.Abstractions;
using Slicito.Abstractions.Facts;
using Slicito.Common.Implementation;
using Slicito.DotNet;

namespace Slicito.Proclaimer.Analyzers.Advanced;

public record MessagingOperationAnalysisResult(ImmutableArray<MessagePublishInvocation> Publishes);

public record MessagePublishInvocation(
    ElementId MethodId,
    ElementId OperationId,
    string PublisherType,
    string MethodName,
    ITypeSymbol? MessageType,
    int LineNumber);

public class MessagingOperationAnalyzer
{
    private readonly DotNetSolutionContext _dotnetContext;
    private readonly DotNetTypes _dotnetTypes;

    public MessagingOperationAnalyzer(DotNetSolutionContext dotnetContext, DotNetTypes dotnetTypes)
    {
        _dotnetContext = dotnetContext;
        _dotnetTypes = dotnetTypes;
    }

    public async Task<MessagingOperationAnalysisResult> AnalyzeMethodAsync(ElementId methodId)
    {
        var publishes = ImmutableArray.CreateBuilder<MessagePublishInvocation>();

        var procedureElement = new SimpleProcedureElement(methodId);
        var operations = await _dotnetContext.TypedSliceFragment.GetOperationsAsync(procedureElement);

        foreach (var operation in operations)
        {
            var operationType = _dotnetContext.Slice.GetElementType(operation.Id);
            if (!operationType.Value.IsSubsetOfOrEquals(_dotnetTypes.Call.Value))
                continue;

            var symbol = _dotnetContext.GetSymbol(operation.Id);
            if (symbol is not IMethodSymbol methodSymbol)
                continue;

            if (IsBusPublish(methodSymbol))
            {
                var publisherType = GetPublisherType(methodSymbol);
                var messageType = DetermineMessageType(methodSymbol);
                var lineNumber = GetLineNumber(operation.Id);

                publishes.Add(new MessagePublishInvocation(
                    methodId,
                    operation.Id,
                    publisherType,
                    methodSymbol.Name,
                    messageType,
                    lineNumber));
            }
        }

        return new MessagingOperationAnalysisResult(publishes.ToImmutable());
    }

    private bool IsBusPublish(IMethodSymbol method)
    {
        var methodName = method.Name;
        if (string.IsNullOrWhiteSpace(methodName))
            return false;

        if (!(methodName.StartsWith("Publish", StringComparison.OrdinalIgnoreCase) ||
              methodName.StartsWith("Send", StringComparison.OrdinalIgnoreCase) ||
              methodName.StartsWith("Enqueue", StringComparison.OrdinalIgnoreCase) ||
              methodName.StartsWith("Dispatch", StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        var receiver = method.ReceiverType ?? method.ContainingType;
        if (receiver is null)
            return false;

        var display = receiver.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
        if (IsLikelyPublisherTypeName(display))
            return true;

        return display.Contains("Bus", StringComparison.OrdinalIgnoreCase) ||
               display.Contains("Queue", StringComparison.OrdinalIgnoreCase) ||
               display.Contains("Topic", StringComparison.OrdinalIgnoreCase) ||
               display.Contains("Dispatcher", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsLikelyPublisherTypeName(string typeName)
    {
        return typeName.Contains("IPublishEndpoint", StringComparison.OrdinalIgnoreCase) ||
               typeName.Contains("ISendEndpoint", StringComparison.OrdinalIgnoreCase) ||
               typeName.Contains("IBus", StringComparison.OrdinalIgnoreCase) ||
               typeName.Contains("ServiceBusClient", StringComparison.OrdinalIgnoreCase) ||
               typeName.Contains("QueueClient", StringComparison.OrdinalIgnoreCase) ||
               typeName.Contains("TopicClient", StringComparison.OrdinalIgnoreCase) ||
               typeName.Contains("IBasicPublisher", StringComparison.OrdinalIgnoreCase) ||
               typeName.Contains("IModel", StringComparison.OrdinalIgnoreCase) && typeName.Contains("RabbitMQ", StringComparison.OrdinalIgnoreCase);
    }

    private string GetPublisherType(IMethodSymbol method)
    {
        var type = method.ReceiverType ?? method.ContainingType;
        return type?.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat) ?? "Unknown";
    }

    private ITypeSymbol? DetermineMessageType(IMethodSymbol method)
    {
        if (method.IsGenericMethod && method.TypeArguments.Length > 0)
        {
            return method.TypeArguments[0];
        }

        if (method.Parameters.Length > 0)
        {
            return method.Parameters[0].Type;
        }

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
