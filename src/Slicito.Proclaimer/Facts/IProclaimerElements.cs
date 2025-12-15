using Slicito.Abstractions;

namespace Slicito.Proclaimer.Facts;

/// <summary>
/// Represents a discovered service element.
/// </summary>
public interface IProclaimerServiceElement
{
    ElementId Id { get; }
    string ServiceName { get; }
}

/// <summary>
/// Represents a Proclaimer endpoint element.
/// </summary>
public interface IProclaimerEndpointElement
{
    ElementId Id { get; }
    string ServiceName { get; }
    string HttpMethod { get; }
    string Route { get; }
}

/// <summary>
/// Represents an HTTP client element.
/// </summary>
public interface IProclaimerHttpClientElement
{
    ElementId Id { get; }
    string ServiceName { get; }
}

/// <summary>
/// Represents a repository element.
/// </summary>
public interface IProclaimerRepositoryElement
{
    ElementId Id { get; }
    string ServiceName { get; }
}

/// <summary>
/// Represents a database element.
/// </summary>
public interface IProclaimerDatabaseElement
{
    ElementId Id { get; }
}

/// <summary>
/// Represents a messaging queue element.
/// </summary>
public interface IProclaimerQueueElement
{
    ElementId Id { get; }
}

/// <summary>
/// Represents a messaging topic element.
/// </summary>
public interface IProclaimerTopicElement
{
    ElementId Id { get; }
}

/// <summary>
/// Represents a background service element.
/// </summary>
public interface IProclaimerBackgroundServiceElement
{
    ElementId Id { get; }
    string ServiceName { get; }
}
