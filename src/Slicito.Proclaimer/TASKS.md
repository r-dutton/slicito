# Slicito.Proclaimer Implementation Tasks

This document tracks the implementation of all Proclaimer flow type analyzers.

## Status Legend
- ✅ Complete
- 🚧 In Progress  
- ⏳ Pending

## Core Analyzers

### 1. Endpoint Discovery ✅
- [x] ASP.NET controller endpoints
- [x] HTTP method extraction (GET, POST, PUT, DELETE, etc.)
- [x] Route extraction and combination
- [x] Controller attribute detection

### 2. MediatR/CQRS Detection ✅
- [x] Detect `IRequest<T>` types
- [x] Detect `IRequestHandler<TRequest, TResponse>` implementations
- [x] Detect `INotification` types  
- [x] Detect `INotificationHandler<T>` implementations
- [x] Create appropriate elements in slice

### 3. Entity Framework Detection ✅
- [x] Detect `DbContext` subclasses
- [x] Create EF-related elements
- [ ] Detect `DbSet<T>` properties and create entity elements (next)
- [ ] Extract entity table names (next)

### 4. Repository Pattern Detection ✅
- [x] Detect repository interfaces (common patterns: IRepository, I*Repository)
- [x] Detect repository implementations
- [x] Create repository elements

### 5. Background Service Detection ✅
- [x] Detect `IHostedService` implementations
- [x] Detect `BackgroundService` subclasses
- [x] Create background service elements

### 6. HTTP Client Detection 🚧
- [x] Analyzer created for HttpClient method calls
- [ ] Integrate into slice builder (next)
- [ ] Extract target URLs and HTTP verbs
- [ ] Create `HttpClient` elements
- [ ] Create `SendsRequest` links

## Progress Tracking

**Completed:** 5/6 core analyzers (83%)
**Next Up:** Complete HTTP Client integration, then add links between elements

---

Last Updated: 2025-12-07T20:15:00Z
