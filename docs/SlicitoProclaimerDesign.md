# Slicito.Proclaimer Design Document

## Overview

This document describes the architecture for integrating TheProclaimer functionality into Slicito as a native first-class domain module.

**Goal**: Reboot TheProclaimer as a native Slicito extension where:
- Slicito is the framework
- Proclaimer is a first-class Slicito domain module
- VS tooling (graph/tree views, navigation) replaces the old CLI as primary interface
- Preserve and re-express bespoke flow-resolution semantics on top of Slicito's slice model

## Original Proclaimer Capabilities

TheProclaimer (https://github.com/r-dutton/TheProclaimer) consists of:
- **GraphKit**: Core library for code analysis and graph generation
- **FlowGrep**: CLI tool for querying and visualizing flows

### Upstream → Slicito mapping at a glance

| Upstream component | Source (TheProclaimer) | Slicito.Proclaimer target |
| --- | --- | --- |
| Graph model constants (node/edge kinds) | `src/GraphKit/Constants/NodeTypes.cs`, `EdgeKinds.cs` | `ProclaimerTypes` + `ProclaimerSchema` define canonical `ElementType`/`LinkType` values in Slicito |
| Workspace & service mapping | `src/GraphKit/Workspace/*`, `flow.workspace.json`, `flow.map.json` | Slicito configuration shim that maps solutions/assemblies/base URLs to Service elements during slice building |
| Controllers & minimal APIs analyzers | `src/GraphKit/Analyzers/ProjectAnalyzer.Controllers*.cs` + visitors | `ProclaimerSliceFragmentBuilder` endpoint discovery (routes + HTTP methods) and `BelongsToService` links |
| HTTP client analyzer (typed + raw) | `ProjectAnalyzer.Http.cs` + `HttpOperationVisitor` | HTTP client element/link creation (`SendsHttpRequest`) with route/method attributes |
| Repository/EF analyzers | `ProjectAnalyzer.Repositories.cs`, `EfOperationVisitor` | Repository + Database elements with `ReadsFrom`/`WritesTo` links |
| Messaging analyzers | `ProjectAnalyzer.Messaging.cs`, `MessagingOperationVisitor` | Queue/Topic elements with `PublishesTo`/`ConsumesFrom` links |
| Background service analyzer | `ProjectAnalyzer.BackgroundServices.cs` | BackgroundService elements and service membership links |
| Flow engine (interprocedural) | `FlowAnalysis/*`, `Analysis.Passes/*` (dedupe, message/http linking) | `ProclaimerFlowService` using slice links; grouping/dedup semantics re-expressed atop slice queries |
| Flow output/formatters | `Outputs/FlowBuilder/*` (Markdown/JSON) | `ProclaimerFlowGraphBuilder` + `ProclaimerFlowTreeBuilder` for VS graph/tree views |
| CLI entrypoint | `src/FlowGrep` | VS controllers and potential CLI wrapper that calls Slicito services |

### GraphKit Architecture

**Node Types** (from `GraphKit.Constants.NodeTypes`):
- `endpoint.controller`, `endpoint.minimal_api` - HTTP endpoints
- `controller.action`, `controller.response` - Controller actions and responses
- `http.client` - HTTP client usage
- `cqrs.request`, `cqrs.handler` - CQRS patterns (MediatR)
- `mediatr.send`, `mediatr.publish` - MediatR operations
- `ef.db_context`, `ef.entity`, `db.table` - Entity Framework and database
- `mapping.automapper.profile`, `mapping.automapper.map` - AutoMapper
- `message.publisher`, `message.contract` - Messaging
- `notification.handler` - Notification handlers
- `app.service`, `app.repository`, `app.background_service` - Application components
- `validator`, `validation` - Validation
- `config.value`, `config.options`, `config.options_poco` - Configuration
- `cache.operation` - Caching

**Edge Kinds** (from `GraphKit.Constants.EdgeKinds`):
- `calls` - Method calls
- `sends_request` - HTTP requests
- `handled_by`, `processed_by` - Handler/processor relationships
- `maps_to` - Mapping relationships
- `uses_client`, `uses_service`, `uses_storage`, `uses_cache` - Dependency usage
- `reads_from`, `queries` - Data access
- `publishes`, `publishes_notification`, `publishes_domain_event` - Event publishing
- `validates`, `validation` - Validation
- `returns`, `casts_to`, `converts_to` - Type transformations
- `manages` - Management relationships

**Analyzers** (interprocedural IOperation visitors):
- Controllers (with expressions, authorization)
- HTTP clients
- CQRS handlers
- Messaging publishers/consumers
- Entity Framework
- Repositories
- Background services
- Caching
- Configuration
- Domain events
- DTOs and mapping
- Dependency injection

**Flow Analysis**:
- Interprocedural analysis using IOperation visitors
- Provenance tracking (where facts come from)
- Confidence scoring (how certain we are)
- Performance guardrails (caching, strict predicates)

## Target Architecture

### 1. Slicito.Proclaimer Project

A new project in the Slicito solution:
- **Location**: `src/Slicito.Proclaimer/`
- **Dependencies**:
  - Slicito.Abstractions
  - Slicito.ProgramAnalysis
  - Slicito.DotNet
  - Slicito.Common
- **Target Framework**: netstandard2.0 (matching existing projects)

### 2. Schema and Types

**Element Types** (mapped from GraphKit NodeTypes to Slicito ElementType):

Core endpoint types:
- `EndpointController` ← `endpoint.controller`
- `EndpointMinimalApi` ← `endpoint.minimal_api`
- `ControllerAction` ← `controller.action`
- `ControllerResponse` ← `controller.response`

Communication:
- `HttpClient` ← `http.client`

CQRS/MediatR:
- `CqrsRequest` ← `cqrs.request`
- `CqrsHandler` ← `cqrs.handler`
- `MediatrSend` ← `mediatr.send`
- `MediatrPublish` ← `mediatr.publish`
- `NotificationHandler` ← `notification.handler`

Data access:
- `EfDbContext` ← `ef.db_context`
- `EfEntity` ← `ef.entity`
- `DbTable` ← `db.table`
- `Repository` ← `app.repository`

Messaging:
- `MessagePublisher` ← `message.publisher`
- `MessageContract` ← `message.contract`

Application:
- `AppService` ← `app.service`
- `BackgroundService` ← `app.background_service`

Mapping:
- `MappingProfile` ← `mapping.automapper.profile`
- `MappingMap` ← `mapping.automapper.map`

Configuration & Validation:
- `Validator` ← `validator`
- `Validation` ← `validation`
- `ConfigValue` ← `config.value`
- `Options` ← `config.options`
- `OptionsPoco` ← `config.options_poco`

Caching:
- `CacheOperation` ← `cache.operation`

**Link Types** (mapped from GraphKit EdgeKinds to Slicito LinkType):
- `Calls` ← `calls`
- `SendsRequest` ← `sends_request`
- `HandledBy` ← `handled_by`
- `ProcessedBy` ← `processed_by`
- `MapsTo` ← `maps_to`
- `UsesClient` ← `uses_client`
- `UsesService` ← `uses_service`
- `UsesStorage` ← `uses_storage`
- `UsesCache` ← `uses_cache`
- `UsesOptions` ← `uses_options`
- `UsesConfiguration` ← `uses_configuration`
- `ReadsFrom` ← `reads_from`
- `Queries` ← `queries`
- `Publishes` ← `publishes`
- `PublishesNotification` ← `publishes_notification`
- `PublishesDomainEvent` ← `publishes_domain_event`
- `RequestProcessorDispatch` ← `requestprocessor.dispatch`
- `GeneratedFrom` ← `generated_from`
- `Implements` ← `implemented_by`
- `InvokesDomain` ← `invokes_domain`
- `Logs` ← `logs`
- `Validates` ← `validates`
- `Validation` ← `validation`
- `ServiceLocated` ← `service_located`
- `Returns` ← `returns`
- `CastsTo` ← `casts_to`
- `ConvertsTo` ← `converts_to`
- `Manages` ← `manages`

**Attributes** (from GraphKit PropKeys + additional metadata):
- `Provenance` - Where the fact came from (analyzer name, analysis type)
- `Confidence` - Confidence score (0.0 to 1.0)
- `Verb` - HTTP verb (GET, POST, etc.)
- `Route` - HTTP route template
- `BaseUrl` - Base URL for HTTP requests
- `ConfigKey` - Configuration key
- `ClientMethod` - HTTP client method name
- `External` - Whether target is external
- `Contract` - Message contract type
- `Entity` - Entity type name
- `Table` - Database table name

**Implementation Files**:
- `ProclaimerTypes.cs` - Strongly-typed handles to schema types
- `ProclaimerSchema.cs` - Schema registration and builder
- `ProclaimerAttributeNames.cs` - Attribute name constants
- `ProclaimerAttributeValues.cs` - Attribute value constants

### 3. Slice Fragment

**Interface**: `IProclaimerSliceFragment`
- Extends `ITypedSliceFragment`
- Provides typed access to Proclaimer elements

**Builder**: `ProclaimerSliceFragmentBuilder`
- Builds on Slicito's DotNet/program analysis
- Analyzes:
  - **Projects** → `Service` elements
  - **ASP.NET endpoints** → `Endpoint`/`Controller` elements with `HttpMethod`/`Route` attributes
  - **HttpClient usage** → `HttpClient` elements with `SendsHttpRequest` links
  - **Repositories** → `Repository` elements with `WritesTo` links
  - **Message brokers** → Publisher/Subscriber elements with messaging links
  - **Background services** → `BackgroundService` elements

### 4. Flow Analysis Service

**Class**: `ProclaimerFlowService`
- Operates on: `ISlice`, `ProclaimerTypes`, `ElementId`, `LinkType`
- Uses Slicito's link query APIs
- **Responsibilities**:
  - Given an endpoint element, compute flow closure
  - Traverse `Calls`, `SendsHttpRequest`, `WritesTo`, `PublishesTo`, `ConsumesFrom`
  - Return structured list/tree of flow nodes
  - Re-express original Proclaimer flow/graph semantics

**Flow Model**:
```csharp
public record FlowNode(
    ElementId ElementId,
    string NodeType,
    ImmutableDictionary<string, string> Attributes,
    ImmutableArray<FlowNode> Children);
```

### 5. Label Providers and View Builders

**ProclaimerLabelProvider**:
- Turn endpoints/services/clients into concise labels
- Provide edge labels from link types
- Format: `[HttpMethod] Route` for endpoints, `Service → TargetService` for HTTP requests

**ProclaimerFlowGraphBuilder**:
- Map flow nodes to `Slicito.Abstractions.Models.Graph`
- Nodes with click commands for navigation
- Edges with labels

**ProclaimerFlowTreeBuilder** (optional):
- Map flows to `Tree` model for outline view
- Hierarchical representation of flow

### 6. Visual Studio Integration

**ProclaimerFlowGraphController : IController**:
- Build slices including Proclaimer facts
- Select endpoint root (initially simple heuristic, later user-selectable)
- Use `ProclaimerFlowService` + label provider + graph builder
- Return `Graph` model
- Wire navigation commands

**ProclaimerFlowTreeController : IController** (optional):
- Return `Tree` model for outline view

### 7. Advanced Semantics

Port from original Proclaimer:
- Repository/DB detection patterns
- Messaging flow patterns (mass transit, RabbitMQ, Azure Service Bus)
- Background service patterns
- Grouping and deduplication logic
- Cross-service flow resolution

## Component Mapping

### From TheProclaimer → To Slicito.Proclaimer

| Original Component | New Component | Notes |
|-------------------|---------------|-------|
| Graph model | ISlice + ProclaimerTypes | Use Slicito's slice model |
| Flow analyzer | ProclaimerFlowService | Re-express on slice model |
| CLI output | VS graph/tree views | UI-first approach |
| Endpoint discovery | ProclaimerSliceFragmentBuilder | Use Slicito.DotNet ASP.NET support |
| HTTP client detection | ProclaimerSliceFragmentBuilder | New analysis |
| Messaging detection | ProclaimerSliceFragmentBuilder | New analysis |

## Implementation Strategy

1. Start with core schema and simple service/endpoint detection
2. Add flow analysis for basic call chains
3. Incrementally add HTTP client detection
4. Add messaging and database detection
5. Port advanced semantics and edge cases
6. Optimize and refine UI/UX

## Open Questions

1. How to select root endpoint for flow analysis? (initially heuristic, later UI selector)
2. Should we support cross-solution analysis? (future enhancement)
3. Export format compatibility with old Proclaimer? (optional compatibility layer)
4. How to handle dynamic URLs and runtime configuration? (best-effort static analysis)

## Dependencies on Existing Slicito Features

- `Slicito.DotNet.AspNetCore` - Already has ApiEndpoint support
- `DotNetTypes.Calls` - For method call analysis
- `ISlice` link queries - For flow traversal
- Graph/Tree models - For UI representation
- IController - For VS integration

## Files to Create

### Core (src/Slicito.Proclaimer/):
- `ProclaimerAttributeNames.cs`
- `ProclaimerAttributeValues.cs`
- `ProclaimerTypes.cs`
- `ProclaimerSchema.cs`
- `IProclaimerSliceFragment.cs`
- `ProclaimerSliceFragmentBuilder.cs`
- `ProclaimerFlowService.cs`
- `ProclaimerLabelProvider.cs`
- `ProclaimerFlowGraphBuilder.cs`
- `Slicito.Proclaimer.csproj`

### Facts (src/Slicito.Proclaimer/Facts/):
- Element interfaces for strongly-typed access

### VS Integration (samples/Controllers/ or later src/Slicito.VisualStudio/):
- `ProclaimerFlowGraphController.cs`

### Tests (tests/Slicito.Proclaimer.Tests/):
- Schema tests
- Slice fragment tests
- Flow service tests

---
*Last updated: 2025-12-07*
