using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Slicito.Abstractions;

namespace Slicito.Proclaimer.Analyzers;

/// <summary>
/// Analyzes types to detect MediatR CQRS patterns.
/// </summary>
internal static class CqrsAnalyzer
{
    /// <summary>
    /// Determines if a type implements IRequest or IRequest&lt;T&gt;.
    /// </summary>
    public static bool IsRequest(INamedTypeSymbol type)
    {
        return ImplementsInterface(type, "MediatR.IRequest") ||
               ImplementsGenericInterface(type, "MediatR.IRequest", 1);
    }
    
    /// <summary>
    /// Determines if a type implements IRequestHandler.
    /// </summary>
    public static bool IsRequestHandler(INamedTypeSymbol type)
    {
        return ImplementsGenericInterface(type, "MediatR.IRequestHandler", 1) ||
               ImplementsGenericInterface(type, "MediatR.IRequestHandler", 2);
    }
    
    /// <summary>
    /// Determines if a type implements INotification.
    /// </summary>
    public static bool IsNotification(INamedTypeSymbol type)
    {
        return ImplementsInterface(type, "MediatR.INotification");
    }
    
    /// <summary>
    /// Determines if a type implements INotificationHandler.
    /// </summary>
    public static bool IsNotificationHandler(INamedTypeSymbol type)
    {
        return ImplementsGenericInterface(type, "MediatR.INotificationHandler", 1);
    }
    
    private static bool ImplementsInterface(INamedTypeSymbol type, string interfaceName)
    {
        foreach (var iface in type.AllInterfaces)
        {
            if (GetFullTypeName(iface) == interfaceName)
                return true;
        }
        return false;
    }
    
    private static bool ImplementsGenericInterface(INamedTypeSymbol type, string interfaceName, int typeParameterCount)
    {
        foreach (var iface in type.AllInterfaces)
        {
            if (iface.IsGenericType && 
                iface.TypeArguments.Length == typeParameterCount)
            {
                var constructedFrom = iface.ConstructedFrom;
                if (GetFullTypeName(constructedFrom) == interfaceName)
                    return true;
            }
        }
        return false;
    }
    
    private static string GetFullTypeName(ITypeSymbol type)
    {
        if (type.ContainingNamespace == null || type.ContainingNamespace.IsGlobalNamespace)
            return type.Name;
        
        return $"{type.ContainingNamespace}.{type.Name}";
    }
}

/// <summary>
/// Analyzes types to detect Entity Framework patterns.
/// </summary>
internal static class EntityFrameworkAnalyzer
{
    /// <summary>
    /// Determines if a type is a DbContext.
    /// </summary>
    public static bool IsDbContext(INamedTypeSymbol type)
    {
        var baseType = type.BaseType;
        while (baseType != null)
        {
            if (baseType.Name == "DbContext" && 
                baseType.ContainingNamespace?.ToString() == "Microsoft.EntityFrameworkCore")
                return true;
            baseType = baseType.BaseType;
        }
        return false;
    }
    
    /// <summary>
    /// Gets DbSet properties from a DbContext.
    /// </summary>
    public static ImmutableArray<IPropertySymbol> GetDbSetProperties(INamedTypeSymbol dbContext)
    {
        var dbSets = ImmutableArray.CreateBuilder<IPropertySymbol>();
        
        foreach (var member in dbContext.GetMembers())
        {
            if (member is IPropertySymbol property && IsDbSetProperty(property))
            {
                dbSets.Add(property);
            }
        }
        
        return dbSets.ToImmutable();
    }
    
    private static bool IsDbSetProperty(IPropertySymbol property)
    {
        var propertyType = property.Type as INamedTypeSymbol;
        if (propertyType == null || !propertyType.IsGenericType)
            return false;
        
        var constructedFrom = propertyType.ConstructedFrom;
        return constructedFrom.Name == "DbSet" &&
               constructedFrom.ContainingNamespace?.ToString() == "Microsoft.EntityFrameworkCore";
    }
}

/// <summary>
/// Analyzes types to detect Repository patterns.
/// </summary>
internal static class RepositoryAnalyzer
{
    /// <summary>
    /// Determines if a type looks like a repository.
    /// </summary>
    public static bool IsRepository(INamedTypeSymbol type)
    {
        // Check if name contains "Repository"
        if (!type.Name.Contains("Repository"))
            return false;
        
        // Check if it's an interface or a class
        return type.TypeKind == TypeKind.Interface || type.TypeKind == TypeKind.Class;
    }
}

/// <summary>
/// Analyzes types to detect Background Services.
/// </summary>
internal static class BackgroundServiceAnalyzer
{
    /// <summary>
    /// Determines if a type is a hosted service.
    /// </summary>
    public static bool IsHostedService(INamedTypeSymbol type)
    {
        // Check if implements IHostedService
        foreach (var iface in type.AllInterfaces)
        {
            if (iface.Name == "IHostedService" &&
                iface.ContainingNamespace?.ToString() == "Microsoft.Extensions.Hosting")
                return true;
        }
        
        // Check if inherits from BackgroundService
        var baseType = type.BaseType;
        while (baseType != null)
        {
            if (baseType.Name == "BackgroundService" &&
                baseType.ContainingNamespace?.ToString() == "Microsoft.Extensions.Hosting")
                return true;
            baseType = baseType.BaseType;
        }
        
        return false;
    }
}
