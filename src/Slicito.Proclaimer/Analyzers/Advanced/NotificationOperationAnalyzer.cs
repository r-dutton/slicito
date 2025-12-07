using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Slicito.Abstractions;
using Slicito.Abstractions.Facts;
using Slicito.Common.Implementation;
using Slicito.DotNet;

namespace Slicito.Proclaimer.Analyzers.Advanced;

public record NotificationOperationAnalysisResult(
    ImmutableArray<NotificationHandlerRequestInvocation> RequestInvocations,
    ImmutableArray<NotificationHandlerPublish> PublishedNotifications,
    ImmutableArray<NotificationHandlerMapperCall> MapperCalls,
    ImmutableArray<NotificationHandlerRepositoryCall> RepositoryCalls);

public record NotificationHandlerRequestInvocation(
    ElementId MethodId,
    ElementId OperationId,
    ITypeSymbol RequestType,
    int LineNumber);

public record NotificationHandlerPublish(
    ElementId MethodId,
    ElementId OperationId,
    ITypeSymbol NotificationType,
    int LineNumber);

public record NotificationHandlerMapperCall(
    ElementId MethodId,
    ElementId OperationId,
    ITypeSymbol? SourceType,
    ITypeSymbol DestinationType,
    int LineNumber);

public record NotificationHandlerRepositoryCall(
    ElementId MethodId,
    ElementId OperationId,
    string RepositoryType,
    string MethodName,
    string Operation,
    int LineNumber);

public class NotificationOperationAnalyzer
{
    private readonly DotNetSolutionContext _dotnetContext;
    private readonly DotNetTypes _dotnetTypes;

    public NotificationOperationAnalyzer(DotNetSolutionContext dotnetContext, DotNetTypes dotnetTypes)
    {
        _dotnetContext = dotnetContext;
        _dotnetTypes = dotnetTypes;
    }

    public async Task<NotificationOperationAnalysisResult> AnalyzeMethodAsync(ElementId methodId)
    {
        var requestInvocations = ImmutableArray.CreateBuilder<NotificationHandlerRequestInvocation>();
        var publishes = ImmutableArray.CreateBuilder<NotificationHandlerPublish>();
        var mappings = ImmutableArray.CreateBuilder<NotificationHandlerMapperCall>();
        var repoCalls = ImmutableArray.CreateBuilder<NotificationHandlerRepositoryCall>();

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

            if (IsMediatorSend(methodSymbol))
            {
                var requestType = DetermineRequestType(methodSymbol);
                if (requestType is not null)
                {
                    var lineNumber = GetLineNumber(operation.Id);
                    requestInvocations.Add(new NotificationHandlerRequestInvocation(
                        methodId, operation.Id, requestType, lineNumber));
                }
            }
            else if (IsMediatorPublish(methodSymbol))
            {
                var notificationType = DetermineNotificationType(methodSymbol);
                if (notificationType is not null)
                {
                    var lineNumber = GetLineNumber(operation.Id);
                    publishes.Add(new NotificationHandlerPublish(
                        methodId, operation.Id, notificationType, lineNumber));
                }
            }
            else if (IsMapperMap(methodSymbol))
            {
                var (sourceType, destType) = GetMapTypes(methodSymbol);
                if (destType is not null)
                {
                    var lineNumber = GetLineNumber(operation.Id);
                    mappings.Add(new NotificationHandlerMapperCall(
                        methodId, operation.Id, sourceType, destType, lineNumber));
                }
            }
            else if (IsRepositoryCall(methodSymbol))
            {
                var repoType = methodSymbol.ReceiverType ?? methodSymbol.ContainingType;
                var repoTypeName = repoType?.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat) ?? "";
                if (!string.IsNullOrEmpty(repoTypeName))
                {
                    var operation_op = DetermineRepositoryOperation(methodSymbol.Name);
                    var lineNumber = GetLineNumber(operation.Id);
                    repoCalls.Add(new NotificationHandlerRepositoryCall(
                        methodId, operation.Id, repoTypeName, methodSymbol.Name, operation_op, lineNumber));
                }
            }
        }

        return new NotificationOperationAnalysisResult(
            requestInvocations.ToImmutable(),
            publishes.ToImmutable(),
            mappings.ToImmutable(),
            repoCalls.ToImmutable());
    }

    private bool IsMediatorSend(IMethodSymbol method)
    {
        return method.Name == "Send" && IsMediator(method.ContainingType);
    }

    private bool IsMediatorPublish(IMethodSymbol method)
    {
        return method.Name == "Publish" && IsMediator(method.ContainingType);
    }

    private bool IsMediator(ITypeSymbol? type)
    {
        return type?.Name == "IMediator" &&
               type.ContainingNamespace?.ToDisplayString() == "MediatR";
    }

    private bool IsMapperMap(IMethodSymbol method)
    {
        return (method.Name == "Map" || method.Name == "ProjectTo") &&
               IsMapper(method.ContainingType ?? method.ReceiverType);
    }

    private bool IsMapper(ITypeSymbol? type)
    {
        var display = type?.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat) ?? "";
        return display.IndexOf("IMapper", StringComparison.OrdinalIgnoreCase) >= 0 ||
               display.IndexOf("AutoMapper", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private bool IsRepositoryCall(IMethodSymbol method)
    {
        var receiverType = method.ReceiverType ?? method.ContainingType;
        var typeName = receiverType?.Name ?? "";
        return typeName.IndexOf("Repository", StringComparison.OrdinalIgnoreCase) >= 0 ||
               typeName.IndexOf("DbContext", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private ITypeSymbol? DetermineRequestType(IMethodSymbol method)
    {
        if (method.Parameters.Length > 0)
        {
            return method.Parameters[0].Type;
        }

        if (method.TypeArguments.Length > 0)
        {
            return method.TypeArguments[0];
        }

        return null;
    }

    private ITypeSymbol? DetermineNotificationType(IMethodSymbol method)
    {
        if (method.Parameters.Length > 0)
        {
            return method.Parameters[0].Type;
        }

        if (method.TypeArguments.Length > 0)
        {
            return method.TypeArguments[0];
        }

        return null;
    }

    private (ITypeSymbol? sourceType, ITypeSymbol? destType) GetMapTypes(IMethodSymbol method)
    {
        ITypeSymbol? destType = null;
        ITypeSymbol? sourceType = null;

        if (method.IsGenericMethod && method.TypeArguments.Length > 0)
        {
            destType = method.TypeArguments[0];
        }

        if (method.Parameters.Length > 0)
        {
            sourceType = method.Parameters[0].Type;
        }

        return (sourceType, destType);
    }

    private string DetermineRepositoryOperation(string methodName)
    {
        if (methodName.IndexOf("Add", StringComparison.OrdinalIgnoreCase) >= 0 ||
            methodName.IndexOf("Insert", StringComparison.OrdinalIgnoreCase) >= 0 ||
            methodName.IndexOf("Create", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return "insert";
        }

        if (methodName.IndexOf("Update", StringComparison.OrdinalIgnoreCase) >= 0 ||
            methodName.IndexOf("Save", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return "update";
        }

        if (methodName.IndexOf("Delete", StringComparison.OrdinalIgnoreCase) >= 0 ||
            methodName.IndexOf("Remove", StringComparison.OrdinalIgnoreCase) >= 0)
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
