# Migration Guide: TheProclaimer to Slicito.Proclaimer

This guide helps you migrate from the legacy TheProclaimer CLI tool to the new Slicito.Proclaimer Visual Studio integration.

## Overview

**TheProclaimer** was a standalone CLI tool (FlowGrep) that analyzed .NET codebases and produced JSON/text flow graphs.

**Slicito.Proclaimer** is a native Slicito extension that provides the same analysis capabilities with Visual Studio integration, interactive visualization, and real-time updates.

## Key Differences

### Before (TheProclaimer)

```bash
# Run from command line
flowgrep analyze --solution MySolution.sln --output flows.json

# View results in text/JSON
cat flows.json
```

**Limitations:**
- CLI-only interface
- Static JSON output
- No code navigation
- Manual refresh needed
- Separate tool from IDE

### After (Slicito.Proclaimer)

```csharp
// Use from Visual Studio or code
var controller = new ProclaimerFlowGraphController(
    dotnetContext, proclaimerTypes, dotnetTypes, sliceManager, navigator);

var graph = await controller.InitAsync();
// Interactive graph in VS with click-to-navigate
```

**Benefits:**
- Visual Studio integration
- Interactive graph visualization
- Click-to-navigate to source
- Real-time analysis
- Part of Slicito ecosystem

## Architecture Mapping

### GraphKit Node Types → Slicito Element Types

| TheProclaimer (GraphKit) | Slicito.Proclaimer |
|--------------------------|-------------------|
| `endpoint.controller` | `EndpointController` |
| `http.client` | `HttpClient` |
| `cqrs.handler` | `CqrsHandler` |
| `ef.db_context` | `EfDbContext` |
| `message.publisher` | `MessagePublisher` |
| `app.repository` | `Repository` |
| `app.background_service` | `BackgroundService` |

### GraphKit Edge Kinds → Slicito Link Types

| TheProclaimer (GraphKit) | Slicito.Proclaimer |
|--------------------------|-------------------|
| `calls` | `Calls` |
| `sends_request` | `SendsRequest` |
| `publishes` | `Publishes` |
| `uses_storage` | `UsesStorage` |
| `queries` | `Queries` |

## Migration Steps

### Step 1: Remove TheProclaimer CLI

If you were using FlowGrep from the command line, you can remove it:

```bash
# Remove old tool (if installed globally)
dotnet tool uninstall -g flowgrep
```

### Step 2: Add Slicito.Proclaimer to Your Solution

Add the Slicito.Proclaimer project reference to your analysis project:

```xml
<ItemGroup>
  <ProjectReference Include="path/to/Slicito.Proclaimer/Slicito.Proclaimer.csproj" />
</ItemGroup>
```

### Step 3: Update Your Analysis Code

**Before (TheProclaimer GraphKit):**

```csharp
using GraphKit;

var generator = new GraphGenerator(options);
var result = await generator.GenerateAsync(workspace);
var graph = result.Graph;

// Access nodes and edges
foreach (var node in graph.Nodes)
{
    Console.WriteLine($"{node.Type}: {node.Name}");
}
```

**After (Slicito.Proclaimer):**

```csharp
using Slicito.Proclaimer;
using Slicito.Common;
using Slicito.DotNet;

// Setup
var typeSystem = new TypeSystem();
var dotnetTypes = new DotNetTypes(typeSystem);
var proclaimerTypes = new ProclaimerTypes(typeSystem);
var sliceManager = new SliceManager(typeSystem);

// Extract solution
var dotnetContext = new DotNetExtractor(dotnetTypes, sliceManager)
    .Extract(ImmutableArray.Create(solution));

// Build Proclaimer slice
var builder = new ProclaimerSliceFragmentBuilder(
    dotnetContext, dotnetTypes, proclaimerTypes, sliceManager);
var fragment = await builder.BuildAsync();

// Get endpoints
var endpoints = await fragment.Slice.GetRootElementsAsync(
    proclaimerTypes.EndpointController);
```

### Step 4: Update Flow Analysis

**Before (TheProclaimer):**

```csharp
// Manual graph traversal
var visited = new HashSet<string>();
var flow = TraverseGraph(graph, startNodeId, visited);
```

**After (Slicito.Proclaimer):**

```csharp
// Built-in flow service
var flowService = new ProclaimerFlowService(fragment.Slice, proclaimerTypes);
var flowRoot = await flowService.ComputeFlowAsync(endpointId);
```

### Step 5: Update Visualization

**Before (TheProclaimer):**

```csharp
// Export to JSON/DOT
var json = JsonSerializer.Serialize(graph);
File.WriteAllText("output.json", json);
```

**After (Slicito.Proclaimer):**

```csharp
// Generate VS-compatible graph
var labelProvider = new ProclaimerLabelProvider(fragment.Slice, proclaimerTypes);
var graphBuilder = new ProclaimerFlowGraphBuilder(labelProvider);
var graph = await graphBuilder.BuildGraphAsync(flowRoot);

// Use in Visual Studio controller
var controller = new ProclaimerFlowGraphController(
    dotnetContext, proclaimerTypes, dotnetTypes, sliceManager);
```

## Feature Parity

### Implemented ✅

- ✅ Endpoint discovery (ASP.NET controllers)
- ✅ HTTP method and route extraction
- ✅ Flow traversal with cycle detection
- ✅ Element type discrimination
- ✅ Attribute-based metadata
- ✅ Visual Studio integration
- ✅ Click-to-navigate support

### In Development 🚧

The following TheProclaimer features are supported by the schema but not yet fully implemented:

- 🚧 HTTP client detection
- 🚧 CQRS/MediatR pattern detection
- 🚧 Message publisher/subscriber detection
- 🚧 Repository pattern detection
- 🚧 Entity Framework usage
- 🚧 Background service detection

These can be added incrementally as needed. The schema and infrastructure are ready.

## Code Examples

### Example 1: Discover All Endpoints

**Before:**
```csharp
var graph = await generator.GenerateAsync(workspace);
var endpoints = graph.Nodes
    .Where(n => n.Type.StartsWith("endpoint."))
    .ToList();
```

**After:**
```csharp
var fragment = await builder.BuildAsync();
var endpoints = await fragment.Slice.GetRootElementsAsync(
    proclaimerTypes.EndpointController);
```

### Example 2: Get Endpoint Details

**Before:**
```csharp
var verb = node.Props["verb"] as string;
var route = node.Props["route"] as string;
var label = $"{verb} {route}";
```

**After:**
```csharp
var verbProvider = fragment.Slice.GetElementAttributeProviderAsyncCallback(
    ProclaimerAttributeNames.Verb);
var routeProvider = fragment.Slice.GetElementAttributeProviderAsyncCallback(
    ProclaimerAttributeNames.Route);

var verb = await verbProvider(endpointId);
var route = await routeProvider(endpointId);
var label = $"{verb} {route}";
```

### Example 3: Traverse Flow

**Before:**
```csharp
void TraverseFlow(GraphNode node, HashSet<string> visited)
{
    if (visited.Contains(node.Id)) return;
    visited.Add(node.Id);
    
    var outgoingEdges = graph.Edges.Where(e => e.From == node.Id);
    foreach (var edge in outgoingEdges)
    {
        var targetNode = graph.Nodes.First(n => n.Id == edge.To);
        TraverseFlow(targetNode, visited);
    }
}
```

**After:**
```csharp
var flowService = new ProclaimerFlowService(fragment.Slice, proclaimerTypes);
var flowRoot = await flowService.ComputeFlowAsync(endpointId, maxDepth: 10);
// Flow tree automatically built with cycle detection
```

## Best Practices

### 1. Use Type-Safe Access

Slicito.Proclaimer provides strongly-typed access to element types:

```csharp
// Good
var endpoints = await slice.GetRootElementsAsync(proclaimerTypes.EndpointController);

// Avoid
var endpoints = await slice.GetRootElementsAsync(); // Too broad
```

### 2. Leverage Label Providers

Use `ProclaimerLabelProvider` for consistent formatting:

```csharp
var labelProvider = new ProclaimerLabelProvider(slice, proclaimerTypes);
var label = await labelProvider.GetElementLabelAsync(elementId);
// Returns formatted label like "GET /api/users/{id}"
```

### 3. Use Flow Service for Traversal

Let `ProclaimerFlowService` handle traversal complexity:

```csharp
var flowService = new ProclaimerFlowService(slice, proclaimerTypes);
var flow = await flowService.ComputeFlowAsync(rootId, maxDepth: 15);
// Handles cycles, depth limits, and multiple link types
```

## Troubleshooting

### Issue: No Endpoints Discovered

**Cause:** The analyzer might not recognize your controller pattern.

**Solution:** Ensure controllers have `[ApiController]` or `[Controller]` attribute and methods have HTTP verb attributes (`[HttpGet]`, `[HttpPost]`, etc.).

### Issue: Missing References

**Cause:** Project reference not added.

**Solution:** Add reference to Slicito.Proclaimer in your project file.

### Issue: Compilation Errors in Analysis

**Cause:** The solution being analyzed has errors.

**Solution:** Fix compilation errors in the target solution first. Proclaimer requires compilable code.

## Support

For questions or issues with migration:

1. Check the [Slicito.Proclaimer README](README.md)
2. Review the [design document](../../docs/SlicitoProclaimerDesign.md)
3. Open an issue in the Slicito repository

## Future Enhancements

The following enhancements are planned:

- User-selectable root endpoints (currently uses first endpoint)
- HTTP client detection and tracing
- Full CQRS/MediatR pattern support
- Message broker integration detection
- Repository pattern detection
- Flow grouping and deduplication
- Export to TheProclaimer-compatible JSON format (if needed)

---

**Last Updated:** 2025-12-07
