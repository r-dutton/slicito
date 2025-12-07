using Slicito.Abstractions;

namespace Slicito.Proclaimer.Facts;

/// <summary>
/// Represents a Proclaimer endpoint element (controller endpoint or minimal API).
/// </summary>
public interface IProclaimerEndpointElement
{
    ElementId Id { get; }
    string HttpMethod { get; }
    string Route { get; }
}

/// <summary>
/// Represents an HTTP client element.
/// </summary>
public interface IProclaimerHttpClientElement
{
    ElementId Id { get; }
}

/// <summary>
/// Represents a CQRS request element.
/// </summary>
public interface IProclaimerCqrsRequestElement
{
    ElementId Id { get; }
}

/// <summary>
/// Represents a CQRS handler element.
/// </summary>
public interface IProclaimerCqrsHandlerElement
{
    ElementId Id { get; }
}

/// <summary>
/// Represents a message publisher element.
/// </summary>
public interface IProclaimerMessagePublisherElement
{
    ElementId Id { get; }
}

/// <summary>
/// Represents a repository element.
/// </summary>
public interface IProclaimerRepositoryElement
{
    ElementId Id { get; }
}

/// <summary>
/// Represents an Entity Framework DbContext element.
/// </summary>
public interface IProclaimerEfDbContextElement
{
    ElementId Id { get; }
}

/// <summary>
/// Represents a background service element.
/// </summary>
public interface IProclaimerBackgroundServiceElement
{
    ElementId Id { get; }
}
