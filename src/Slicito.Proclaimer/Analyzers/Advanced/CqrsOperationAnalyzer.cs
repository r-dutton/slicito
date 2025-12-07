using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Slicito.Abstractions;
using Slicito.Abstractions.Facts;
using Slicito.Common.Implementation;
using Slicito.DotNet;

namespace Slicito.Proclaimer.Analyzers.Advanced;

/// <summary>
/// Result of analyzing operations in a method for CQRS patterns (MediatR usage).
/// </summary>
public record CqrsOperationAnalysisResult(
    ImmutableArray<MediatorSendInvocation> Sends,
    ImmutableArray<MediatorPublishInvocation> Publishes);

/// <summary>
/// Represents a MediatR Send invocation discovered in operations.
/// </summary>
public record MediatorSendInvocation(
    ElementId MethodId,
    ElementId OperationId,
    ITypeSymbol RequestType,
    int LineNumber);

/// <summary>
/// Represents a MediatR Publish invocation discovered in operations.
/// </summary>
public record MediatorPublishInvocation(
    ElementId MethodId,
    ElementId OperationId,
    ITypeSymbol NotificationType,
    int LineNumber);

/// <summary>
/// Analyzes IOperation trees to detect CQRS/MediatR patterns.
/// Extends Slicito's operation analysis with Proclaimer-specific pattern detection.
/// </summary>
public class CqrsOperationAnalyzer
{
    private readonly DotNetSolutionContext _dotnetContext;
    private readonly DotNetTypes _dotnetTypes;

    public CqrsOperationAnalyzer(DotNetSolutionContext dotnetContext, DotNetTypes dotnetTypes)
    {
        _dotnetContext = dotnetContext;
        _dotnetTypes = dotnetTypes;
    }

    /// <summary>
    /// Analyzes all operations in a method to detect MediatR Send/Publish calls.
    /// </summary>
    public async Task<CqrsOperationAnalysisResult> AnalyzeMethodAsync(ElementId methodId)
    {
        var sends = ImmutableArray.CreateBuilder<MediatorSendInvocation>();
        var publishes = ImmutableArray.CreateBuilder<MediatorPublishInvocation>();

        // Get operations for this method from DotNet slice
        var procedureElement = new SimpleProcedureElement(methodId);
        var operations = await _dotnetContext.TypedSliceFragment.GetOperationsAsync(procedureElement);

        foreach (var operation in operations)
        {
            // Check if this is a call operation
            var operationType = _dotnetContext.Slice.GetElementType(operation.Id);
            if (!operationType.Value.IsSubsetOfOrEquals(_dotnetTypes.Call.Value))
                continue;

            // Get the symbol for the invoked method
            var symbol = _dotnetContext.GetSymbol(operation.Id);
            if (symbol is not IMethodSymbol methodSymbol)
                continue;

            // Check for MediatR.Send
            if (IsMediatorSend(methodSymbol))
            {
                var requestType = GetRequestType(methodSymbol);
                if (requestType is not null)
                {
                    var lineNumber = GetLineNumber(operation.Id);
                    sends.Add(new MediatorSendInvocation(methodId, operation.Id, requestType, lineNumber));
                }
            }
            // Check for MediatR.Publish
            else if (IsMediatorPublish(methodSymbol))
            {
                var notificationType = GetNotificationType(methodSymbol);
                if (notificationType is not null)
                {
                    var lineNumber = GetLineNumber(operation.Id);
                    publishes.Add(new MediatorPublishInvocation(methodId, operation.Id, notificationType, lineNumber));
                }
            }
        }

        return new CqrsOperationAnalysisResult(sends.ToImmutable(), publishes.ToImmutable());
    }

    private bool IsMediatorSend(IMethodSymbol method)
    {
        return method.Name == "Send" &&
               (IsMediator(method.ContainingType) || HasMediatorParameter(method));
    }

    private bool IsMediatorPublish(IMethodSymbol method)
    {
        return method.Name == "Publish" &&
               (IsMediator(method.ContainingType) || HasMediatorParameter(method));
    }

    private bool IsMediator(ITypeSymbol? type)
    {
        return type?.Name == "IMediator" &&
               type.ContainingNamespace?.ToDisplayString() == "MediatR";
    }

    private bool HasMediatorParameter(IMethodSymbol method)
    {
        return method.Parameters.Any(p => IsMediator(p.Type));
    }

    private ITypeSymbol? GetRequestType(IMethodSymbol sendMethod)
    {
        // For Send<TResponse>(IRequest<TResponse> request), get the request type
        if (sendMethod.TypeArguments.Length > 0)
        {
            return sendMethod.TypeArguments[0]; // This is actually the response type
        }

        // Try to get from first parameter
        if (sendMethod.Parameters.Length > 0)
        {
            var firstParam = sendMethod.Parameters[0];
            // The parameter should be IRequest<T> or similar
            return firstParam.Type;
        }

        return null;
    }

    private ITypeSymbol? GetNotificationType(IMethodSymbol publishMethod)
    {
        // For Publish<TNotification>(TNotification notification), get the notification type
        if (publishMethod.TypeArguments.Length > 0)
        {
            return publishMethod.TypeArguments[0];
        }

        // Try to get from first parameter
        if (publishMethod.Parameters.Length > 0)
        {
            return publishMethod.Parameters[0].Type;
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
