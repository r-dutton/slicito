# Slicito.Proclaimer

Slicito.Proclaimer is a native Slicito extension that provides service architecture analysis and flow visualization for .NET applications. It replaces the legacy TheProclaimer CLI tool with Visual Studio-integrated graph and tree views.

## Features

- **Endpoint Discovery**: Automatically discovers ASP.NET Core controller endpoints with HTTP methods and routes
- **Flow Analysis**: Traces execution flows through calls, HTTP requests, messaging, and data access
- **Visual Studio Integration**: Interactive graph visualization with click-to-navigate support
- **Extensible Schema**: 30+ element types and 27 link types for comprehensive service architecture modeling

## Architecture

### Element Types

Proclaimer defines specialized element types for service architectures:

- **Endpoints**: `EndpointController`, `EndpointMinimalApi`, `ControllerAction`
- **Communication**: `HttpClient`
- **CQRS/MediatR**: `CqrsRequest`, `CqrsHandler`, `MediatrSend`, `MediatrPublish`
- **Data Access**: `EfDbContext`, `EfEntity`, `DbTable`, `Repository`
- **Messaging**: `MessagePublisher`, `MessageContract`
- **Application**: `AppService`, `BackgroundService`
- **And more**: Mapping, validation, configuration, caching

### Link Types

Links represent relationships between elements:

- `Calls` - Method calls
- `SendsRequest` - HTTP requests
- `HandledBy`, `ProcessedBy` - Handler relationships
- `Publishes` - Event publishing
- `UsesStorage`, `Queries` - Data access
- `MapsTo` - Object mapping
- And 20+ more specialized link types

## Usage

### Basic Usage

```csharp
using Slicito.Proclaimer;

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
    proclaimerTypes.EndpointController);
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

Strongly-typed handles to all Proclaimer element and link types.

```csharp
public class ProclaimerTypes : IProgramTypes
{
    public ElementType EndpointController { get; }
    public ElementType HttpClient { get; }
    public LinkType SendsRequest { get; }
    // ... 30+ element types, 27 link types
}
```

### ProclaimerSliceFragmentBuilder

Analyzes .NET solutions and discovers service architecture elements.

- Discovers ASP.NET controller endpoints
- Extracts HTTP methods and routes
- Creates typed elements in the slice

### ProclaimerFlowService

Computes execution flows through service architectures.

- Recursive traversal with cycle detection
- Configurable depth limits
- Traverses multiple link types (calls, HTTP, messaging, data access)

### ProclaimerLabelProvider

Provides human-readable labels for visualization.

- Formats endpoints as `"GET /api/users"`
- Formats HTTP clients, CQRS handlers, repositories
- Supports all Proclaimer element types

### ProclaimerFlowGraphBuilder

Converts flow trees to graph models for Visual Studio.

- Builds node/edge graph structures
- Adds navigation commands
- Supports click-to-code integration

### ProclaimerFlowGraphController

Visual Studio controller for interactive flow visualization.

- Discovers endpoints automatically
- Computes and visualizes flows
- Supports navigation to source code

## Example: Discovered Endpoint

For a controller like:

```csharp
[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(int id)
    {
        // ...
    }
}
```

Proclaimer discovers:
- Element: `EndpointController`
- Verb: `"GET"`
- Route: `"/api/users/{id}"`
- Label: `"GET /api/users/{id}"`

## Advanced Features (Phase 6 - In Development)

The following features are planned for Phase 6:

- **HTTP Client Detection**: Discover HttpClient usage and outgoing HTTP requests
- **Messaging Flows**: Detect message publishers and subscribers
- **Repository Pattern**: Identify repository and database interactions
- **Background Services**: Discover hosted services and background workers
- **Flow Grouping**: Group and deduplicate flows for cleaner visualization

## Migration from TheProclaimer

Slicito.Proclaimer replaces the legacy TheProclaimer CLI tool:

- **Before**: FlowGrep CLI with JSON/text output
- **After**: Visual Studio integration with interactive graphs
- **Benefits**: 
  - Live visualization during development
  - Click-to-navigate to source code
  - Real-time analysis of code changes
  - Integration with existing Slicito tooling

## Testing

Run schema tests:

```bash
dotnet test tests/Slicito.Proclaimer.Tests/Slicito.Proclaimer.Tests.csproj
```

## Contributing

See the main Slicito documentation for contribution guidelines.

## Status

**Current**: Phases 0-5 complete
- ✅ Schema infrastructure (30+ element types, 27 link types)
- ✅ Endpoint discovery from ASP.NET controllers
- ✅ Flow analysis with recursive traversal
- ✅ Label providers and graph builders
- ✅ Visual Studio controller integration

**Next**: Phases 6-7
- 🚧 Advanced semantic analyzers (HTTP clients, messaging, repositories)
- 🚧 Documentation and migration guides

## License

See the main Slicito repository for licensing information.
