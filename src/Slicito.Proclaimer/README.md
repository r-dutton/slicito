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

## Advanced Features (Phase 5 - Complete)

Slicito.Proclaimer now includes comprehensive operation analysis that detects patterns across your codebase:

### Interprocedural Flow Analysis

- **CQRS/MediatR Analysis**: Detects `IMediator.Send()` and `Publish()` calls, links handlers to requests/notifications
- **HTTP Client Detection**: Discovers HttpClient usage with HTTP verbs (GET, POST, PUT, DELETE, PATCH)
- **Entity Framework Tracking**: Identifies DbSet operations (Add, Update, Remove, Find, SaveChanges) and LINQ queries
- **AutoMapper Detection**: Detects Map and ProjectTo operations, tracks source-to-destination type mappings
- **Caching Operations**: Identifies IMemoryCache and IDistributedCache Get/Set/Remove operations
- **Validation Analysis**: Detects FluentValidation usage and validator invocations
- **Configuration Access**: Tracks IConfiguration and IOptions usage
- **Dependency Injection**: Analyzes service registrations (AddScoped, AddTransient, AddSingleton) with lifetime tracking
- **Messaging Patterns**: Detects MassTransit, Azure Service Bus, and RabbitMQ publish/send operations
- **Notification Handlers**: Tracks INotificationHandler implementations and their operations (Send, Publish, Mapping, Repository calls)
- **Domain Events**: Detects domain event publication and dispatcher usage
- **MediatR Pipeline**: Identifies pipeline behaviors, pre-processors, and post-processors
- **Service Operations**: Tracks service invocations, options usage, logging, and validation calls

### How It Works

The operation analyzers work at the Roslyn IOperation level, walking the operation trees of all methods to identify framework-specific patterns. Results are emitted as Slicito slice elements and links, enabling:

1. **Discovery**: Find all places a specific pattern is used
2. **Flow Tracking**: Trace data and control flow through multiple layers
3. **Impact Analysis**: Understand dependencies and relationships
4. **Visualization**: See patterns in the interactive graph

### Implementation Limitations

While all 10 TheProclaimer operation visitors have been ported, some advanced features are not available due to Slicito's infrastructure:

**Missing Infrastructure (requires Roslyn.Analyzers.DataFlow package):**
- `FlowPointsToFacade` - Points-to analysis for precise type resolution
- `FlowValueContentFacade` - Value content analysis for string literal extraction

**Feature Limitations:**
- **Route extraction**: Cannot extract HTTP routes from string interpolation or builder patterns
- **Config keys**: Cannot extract configuration keys from string literals
- **Query parameters**: HTTP query parameter parsing not implemented
- **Field references**: Service field/property reference tracking is basic (no points-to analysis)
- **Service resolution**: Scoped service resolution not implemented (no DI container analysis)
- **Mapping profiles**: AutoMapper profile detection not implemented

**What Works:**
- ✅ All pattern detection (MediatR, HTTP, EF, Mapping, Messaging, etc.)
- ✅ Type-based analysis and symbol resolution
- ✅ Method invocation tracking
- ✅ Framework-specific pattern recognition
- ✅ Line number tracking for all operations

See individual analyzer files for detailed TODO comments on missing features.

### Example

```csharp
// This controller action will be analyzed to detect:
// - Endpoint (GET /api/users/{id})
// - MediatR.Send call
// - Handler invocation
// - Entity Framework query
[HttpGet("{id}")]
public async Task<IActionResult> GetUser(int id)
{
    var query = new GetUserQuery(id);
    var user = await _mediator.Send(query);  // ← Detected
    return Ok(user);
}

// Handler analysis detects:
// - EF DbSet query
// - Entity type usage
public class GetUserQueryHandler : IRequestHandler<GetUserQuery, UserDto>
{
    public async Task<UserDto> Handle(GetUserQuery request, CancellationToken ct)
    {
        var user = await _db.Users.FindAsync(request.Id);  // ← Detected
        return _mapper.Map<UserDto>(user);  // ← Detected
    }
}
```

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

**Current**: Phase 5 complete
- ✅ Schema infrastructure (30+ element types, 27 link types)
- ✅ Endpoint discovery from ASP.NET controllers
- ✅ Flow analysis with recursive traversal
- ✅ Label providers and graph builders
- ✅ Visual Studio controller integration
- ✅ Advanced operation analyzers (CQRS, HTTP, EF, Mapping, Caching, DI, Configuration, Messaging)
- ✅ Specialized operation analyzers (Notification handlers, Domain events, Pipeline behaviors, Service operations)
- ✅ Interprocedural pattern detection across all methods

**Next**: Phase 6+
- 🚧 Value content analysis for route/configuration key extraction
- 🚧 String literal collection and propagation
- 🚧 Authorization attribute parsing
- 🚧 Full points-to analysis integration

## License

See the main Slicito repository for licensing information.
