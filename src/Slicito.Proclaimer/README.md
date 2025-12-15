# Slicito.Proclaimer

Slicito.Proclaimer is a native Slicito extension that provides service architecture analysis and flow visualization for .NET applications. It replaces the legacy TheProclaimer CLI tool with Visual Studio-integrated graph and tree views.

## Features

- **Endpoint Discovery**: Discovers ASP.NET Core endpoints (controllers and minimal APIs) with HTTP methods and routes.
- **Flow Analysis**: Traces execution flows through calls, HTTP requests, messaging, and data access links using slice facts.
- **Visual Studio Integration**: Interactive graph visualization with click-to-navigate support.
- **Canonical Schema**: Focused, Slicito-native element/link types for services, endpoints, HTTP clients, repositories/DB, messaging, and background services.

## Architecture

### Element Types

Proclaimer defines the following canonical element types:

- **Service**: Logical service or application boundary.
- **Endpoint**: HTTP entrypoints (controllers or minimal APIs).
- **HttpClient**: Outbound HTTP calls (raw or typed clients).
- **Repository**: Data access components.
- **Database**: Datastores targeted by repositories/endpoints.
- **Queue / Topic**: Messaging surfaces.
- **BackgroundService**: Hosted/background processes.

### Link Types

Links represent relationships between elements:

- `BelongsToService` – Associates elements back to their owning service.
- `Calls` – Logical call relationships.
- `SendsHttpRequest` – Outbound HTTP requests to HttpClient elements.
- `WritesTo` / `ReadsFrom` – Data access relationships to databases.
- `PublishesTo` / `ConsumesFrom` – Messaging interactions with queues/topics.

## Usage

```csharp
using Slicito.Proclaimer;
using Slicito.Common;

// Create types
var typeSystem = new TypeSystem();
var dotNetTypes = new DotNetTypes(typeSystem);
var proclaimerTypes = new ProclaimerTypes(typeSystem);
var sliceManager = new SliceManager(typeSystem);

// Analyze solution
var dotnetContext = new DotNetExtractor(dotNetTypes, sliceManager)
    .Extract(ImmutableArray.Create(solution));

// Build Proclaimer slice
var builder = new ProclaimerSliceFragmentBuilder(
    dotnetContext,
    dotNetTypes,
    proclaimerTypes,
    sliceManager);

var fragment = await builder.BuildAsync();

// Get discovered endpoints
var endpoints = await fragment.Slice.GetRootElementsAsync(
    proclaimerTypes.Endpoint);
```

### Flow Analysis

```csharp
// Create flow service
var flowService = new ProclaimerFlowService(
    fragment.Slice,
    proclaimerTypes);

// Compute flow from an endpoint
var flowRoot = await flowService.ComputeFlowAsync(endpointId);

// Build graph for visualization
var labelProvider = new ProclaimerLabelProvider(
    fragment.Slice,
    proclaimerTypes);
var graphBuilder = new ProclaimerFlowGraphBuilder(labelProvider);
var graph = await graphBuilder.BuildGraphAsync(flowRoot);
```

### Visual Studio Integration

```csharp
// Create controller for VS integration
var controller = new ProclaimerFlowGraphController(
    dotnetContext,
    proclaimerTypes,
    dotNetTypes,
    sliceManager,
    navigator); // ICodeNavigator for click-to-navigate

// Initialize and get graph
var graph = await controller.InitAsync();
```

## Components

### ProclaimerTypes

Strongly-typed handles to the canonical Proclaimer element and link types.

```csharp
public class ProclaimerTypes : IProgramTypes
{
    public ElementType Service { get; }
    public ElementType Endpoint { get; }
    public ElementType HttpClient { get; }
    public LinkType BelongsToService { get; }
    public LinkType SendsHttpRequest { get; }
    // ...other canonical element and link types
}
```

### ProclaimerSliceFragmentBuilder

Builds a Proclaimer slice fragment on top of a DotNet solution context. Discovery logic for services/endpoints/HTTP/DB/messaging is layered on over time while keeping the canonical schema stable.
