# Slicito.Proclaimer Implementation Tasks

This document tracks the implementation of all Proclaimer flow type analyzers based on TheProclaimer's complete functionality.

**TheProclaimer Analysis:**
- **Total Analyzer Code**: 20,541 lines across 26 files
- **Operation Visitors**: 10 specialized IOperation visitors for interprocedural analysis
- **Analyzer Partials**: 22 partial classes for ProjectAnalyzer
- **Key Capability**: Full interprocedural flow analysis with points-to analysis, constant propagation, and value tracking

## Status Legend
- ✅ Complete (Full Parity)
- 🟡 Partial (Basic Implementation)
- ⏳ Pending (Not Started)
- ❌ Gap (Missing Critical Functionality)

## I. CRITICAL GAPS - Interprocedural Flow Analysis

### ❌ 1. IOperation Visitors (0/10 Implemented)

TheProclaimer uses 10 specialized operation visitors for **deep interprocedural analysis**:

1. **ControllerOperationVisitor.cs** (9.0K) - ⏳ Not Implemented
   - Mediator.Send/Publish detection with request type resolution
   - Repository method call tracking
   - AutoMapper ProjectTo/Map detection
   - FluentValidation usage
   - Cache operations (Get/Set/Remove)
   - Configuration access (IConfiguration[], IOptions<T>)
   - HTTP client invocations from controllers
   - Domain model invocations
   - Service method calls with response tracking

2. **CqrsOperationVisitor.cs** (11K) - ⏳ Not Implemented
   - Nested Mediator.Send calls within handlers
   - Repository usage in handlers
   - Domain model manipulation
   - Mapping operations
   - Validation calls
   - Cache usage in handlers

3. **ServiceOperationVisitor.cs** (15K) - ⏳ Not Implemented  
   - Service-to-service calls
   - Repository usage from services
   - HTTP client calls from services
   - Mediator usage from services
   - Domain model access

4. **HttpOperationVisitor.cs** (5.9K) - 🟡 Partial
   - ✅ Basic HttpClient method detection
   - ❌ Route parameter extraction from string interpolation
   - ❌ Query parameter tracking
   - ❌ URL builder pattern detection
   - ❌ HttpRequestMessage construction tracking

5. **EfOperationVisitor.cs** (8.9K) - ⏳ Not Implemented
   - DbSet operations (Add, Update, Remove, Find)
   - LINQ query analysis (Where, Select, Include)
   - SaveChanges tracking
   - Entity type flow through queries

6. **MessagingOperationVisitor.cs** (6.1K) - ⏳ Not Implemented
   - MassTransit Publish/Send detection
   - Azure Service Bus operations
   - RabbitMQ operations
   - Message contract tracking

7. **NotificationOperationVisitor.cs** (7.6K) - ⏳ Not Implemented
   - IMediator.Publish for notifications
   - INotification tracking through execution
   - Multiple handler invocation detection

8. **DomainEventsOperationVisitor.cs** (7.7K) - ⏳ Not Implemented
   - Domain event publication
   - Event dispatcher usage
   - Event handler linking

9. **MappingOperationVisitor.cs** (3.8K) - ⏳ Not Implemented
   - IMapper.Map<T> detection
   - ProjectTo<T> detection
   - Source/destination type tracking

10. **PipelineOperationVisitor.cs** (6.1K) - ⏳ Not Implemented
    - MediatR pipeline behavior detection
    - Request pre/post processors
    - Validation pipeline tracking

### ❌ 2. Flow Analysis Infrastructure (0% Implemented)

TheProclaimer's interprocedural analysis infrastructure:

- **FlowAnalysisCore.GetOrCreateMethodAnalysis** - ⏳ Not Available
  - Requires full control-flow graph
  - Roslyn IOperation-based analysis
  - Points-to analysis for tracking object flow
  - Value content analysis for constant propagation

- **FlowPointsToFacade** - ⏳ Not Implemented
  - Tracks what variables/fields point to
  - Enables service/repository instance resolution
  - Critical for inter-method flow tracking

- **FlowValueContentFacade** - ⏳ Not Implemented
  - String constant propagation
  - HTTP route resolution
  - Configuration key tracking

- **InterproceduralConfiguration** - ⏳ Not Implemented
  - Depth limits for recursive analysis
  - Caching strategies
  - Performance guardrails

### ❌ 3. Syntax-Level Analysis (10% Implemented)

While I have basic type detection, TheProclaimer has extensive syntax analysis:

- **String Constant Collection** - ⏳ Not Implemented
  - Collects all string literals in compilation
  - Enables route/config key matching

- **Field Type Tracking** - ⏳ Not Implemented
  - Maps fields to their types
  - Enables service/repo resolution from field access

- **Local Variable Flow** - ⏳ Not Implemented
  - Tracks type assignments through method
  - Enables fluent API pattern detection

- **Helper Method Merging** - ⏳ Not Implemented
  - Inlines private helper method calls
  - Provides complete flow picture

## II. TYPE DETECTION (Current Status)

### ✅ 1. Endpoint Discovery - COMPLETE
- [x] ASP.NET controller endpoints
- [x] HTTP method extraction
- [x] Route extraction and combination
- [x] Controller attribute detection
- [x] Minimal API endpoints

### 🟡 2. MediatR/CQRS Detection - BASIC
- [x] Detect `IRequest<T>` types
- [x] Detect `IRequestHandler<TRequest, TResponse>` implementations
- [x] Detect `INotification` types  
- [x] Detect `INotificationHandler<T>` implementations
- [x] Create appropriate elements in slice
- ❌ **Missing**: Mediator.Send call tracking (requires ControllerOperationVisitor)
- ❌ **Missing**: Handler-to-handler chaining
- ❌ **Missing**: Request/response type flow
- ❌ **Missing**: Pipeline behavior tracking

### 🟡 3. Entity Framework Detection - BASIC
- [x] Detect `DbContext` subclasses
- [x] Detect `DbSet<T>` properties and extract entity types
- [x] Create EfDbContext elements
- [x] Create EfEntity elements for entity types
- ❌ **Missing**: DbSet operation tracking (Add/Update/Remove/SaveChanges)
- ❌ **Missing**: LINQ query analysis
- ❌ **Missing**: Include/ThenInclude relationship tracking
- ❌ **Missing**: Transaction tracking

### ✅ 4. Repository Pattern Detection - COMPLETE
- [x] Detect repository interfaces
- [x] Detect repository implementations
- [x] Create repository elements
- ❌ **Missing**: Repository method call tracking (requires operation visitors)
- ❌ **Missing**: Entity type flow through repository methods

### ✅ 5. Background Service Detection - COMPLETE
- [x] Detect `IHostedService` implementations
- [x] Detect `BackgroundService` subclasses
- [x] Create background service elements

### 🟡 6. HTTP Client Detection - PARTIAL
- [x] Analyzer implemented using ICSharpProcedureElement
- [x] Basic HttpClient.GetAsync/PostAsync detection
- [x] Creates HttpClient elements
- [x] Creates SendsRequest links
- ❌ **Missing**: Route parameter extraction (requires value content analysis)
- ❌ **Missing**: Query parameter tracking
- ❌ **Missing**: URL builder pattern detection
- ❌ **Missing**: HttpRequestMessage flow analysis

## III. MISSING ANALYZERS (0% Implemented)

### ⏳ 7. Messaging/Service Bus Detection
**TheProclaimer Support:**
- ProjectAnalyzer.Messaging.cs (messaging publishers/consumers)
- MessagingOperationVisitor.cs (operation-level detection)

**Missing Elements:**
- [ ] MassTransit IPublishEndpoint detection
- [ ] Azure Service Bus QueueClient/TopicClient
- [ ] RabbitMQ IModel usage
- [ ] Message contract tracking
- [ ] Publish/Send call detection
- [ ] Consumer/handler linking

### ⏳ 8. AutoMapper Detection  
**TheProclaimer Support:**
- ProjectAnalyzer.Mapping.cs
- MappingOperationVisitor.cs

**Missing Elements:**
- [ ] IMapper.Map<T> detection
- [ ] ProjectTo<T> detection
- [ ] Source/destination type linking
- [ ] Profile detection
- [ ] CreateMap configuration

### ⏳ 9. FluentValidation Detection
**TheProclaimer Support:**
- Validator detection in ProjectAnalyzer
- Validation calls in ControllerOperationVisitor

**Missing Elements:**
- [ ] AbstractValidator<T> detection
- [ ] Validator invocation tracking
- [ ] RuleFor analysis
- [ ] Validation pipeline integration

### ⏳ 10. Configuration Detection
**TheProclaimer Support:**
- ProjectAnalyzer.Options.cs
- ProjectAnalyzer.Configuration.cs
- Configuration tracking in all operation visitors

**Missing Elements:**
- [ ] IConfiguration indexer usage
- [ ] IOptions<T> injection
- [ ] Configuration.GetSection detection
- [ ] appsettings.json parsing
- [ ] Configuration key tracking

### ⏳ 11. Caching Detection
**TheProclaimer Support:**
- ProjectAnalyzer.Caching.cs
- Cache operation tracking in visitors

**Missing Elements:**
- [ ] IMemoryCache usage
- [ ] IDistributedCache usage  
- [ ] Get/Set/Remove operations
- [ ] Cache key tracking

### ⏳ 12. Domain Events
**TheProclaimer Support:**
- ProjectAnalyzer.DomainEvents.cs
- DomainEventsOperationVisitor.cs

**Missing Elements:**
- [ ] Domain event types
- [ ] Event dispatcher detection
- [ ] Event publication tracking
- [ ] Event handler linking

### ⏳ 13. Dependency Injection
**TheProclaimer Support:**
- ProjectAnalyzer.DependencyInjection.cs
- Service registration analysis

**Missing Elements:**
- [ ] AddScoped/AddTransient/AddSingleton detection
- [ ] Service lifetime tracking
- [ ] Factory registration patterns
- [ ] Service resolution tracking

### ⏳ 14. DTOs and Models
**TheProclaimer Support:**
- ProjectAnalyzer.Dtos.cs
- DTO type categorization

**Missing Elements:**
- [ ] DTO type detection
- [ ] Request/Response model linking
- [ ] DTO-Entity mapping detection

## IV. ADVANCED FEATURES (0% Implemented)

### ⏳ 15. Deferred Linking
**TheProclaimer Support:**
- ClientLinker.EmitClientUseCallEdges
- MessageLinker.EmitMessageContractLinks

**Missing:**
- [ ] Cross-solution client linking
- [ ] Message contract resolution
- [ ] Synthetic edge generation

### ⏳ 16. Provenance & Confidence
**TheProclaimer Support:**
- Edge provenance annotation (Static/Interprocedural/Synthetic)
- Confidence scores (0.0-1.0)

**Missing:**
- [ ] Provenance tracking for all edges
- [ ] Confidence score calculation
- [ ] Evidence collection

### ⏳ 17. Authorization Analysis
**TheProclaimer Support:**
- CollectAuthorizationAttributes
- [Authorize] attribute parsing
- Policy/Role extraction

**Missing:**
- [ ] Authorization requirement detection
- [ ] Policy analysis
- [ ] AllowAnonymous tracking

## V. IMPLEMENTATION PRIORITIES

### Phase A: Critical Infrastructure (HIGH PRIORITY)
1. **Implement Points-To Analysis** - Enables all interprocedural analysis
2. **Implement Value Content Analysis** - Enables route/config resolution
3. **Add ControllerOperationVisitor** - Core flow detection from endpoints
4. **Add CqrsOperationVisitor** - Handler flow analysis

### Phase B: Core Flow Tracking (MEDIUM PRIORITY)
5. **Add ServiceOperationVisitor** - Service-to-service flows
6. **Add EfOperationVisitor** - Database operation tracking
7. **Add HttpOperationVisitor (Complete)** - Full HTTP client analysis
8. **Add MessagingOperationVisitor** - Service bus flows

### Phase C: Supporting Analyzers (LOW PRIORITY)
9. **Add MappingOperationVisitor** - Mapping flows
10. **Add NotificationOperationVisitor** - Notification flows
11. **Configuration/Caching/Validation** - Cross-cutting concerns

### Phase D: Advanced Features
12. **Deferred Linking** - Cross-solution support
13. **Provenance/Confidence** - Metadata enrichment
14. **Authorization** - Security analysis

## VI. CURRENT IMPLEMENTATION GAPS

**What Exists:**
- ✅ Basic type detection (classes/interfaces)
- ✅ Element creation in slice
- ✅ Link type definitions in schema

**What's Missing:**
- ❌ **ALL interprocedural flow tracking**
- ❌ **ALL operation-level analysis**
- ❌ **Points-to and value tracking**
- ❌ **Method call graph construction**
- ❌ **Service/repository instance resolution**
- ❌ **HTTP route parameter extraction**
- ❌ **CQRS request/handler linking at runtime**
- ❌ **Mapping source/destination tracking**
- ❌ **Configuration usage tracking**

## Estimation

**Current Implementation:** ~5% of TheProclaimer functionality
- Type detection: 30% complete (missing runtime behavior)
- Flow analysis: 0% complete (no interprocedural analysis)
- Link creation: 10% complete (missing operation-level links)

**Remaining Work:** ~15,000-20,000 lines of analyzer code
- 10 operation visitors: ~8,000 lines
- Infrastructure (points-to, value content): ~3,000 lines
- Linking/merging logic: ~2,000 lines
- Advanced features: ~5,000 lines

---

Last Updated: 2025-12-07T20:45:00Z
