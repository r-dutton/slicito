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
- [x] Detect `DbSet<T>` properties and extract entity types
- [x] Create EfDbContext elements
- [x] Create EfEntity elements for entity types

### 4. Repository Pattern Detection ✅
- [x] Detect repository interfaces (common patterns: IRepository, I*Repository)
- [x] Detect repository implementations
- [x] Create repository elements

### 5. Background Service Detection ✅
- [x] Detect `IHostedService` implementations
- [x] Detect `BackgroundService` subclasses
- [x] Create background service elements

### 6. HTTP Client Detection ✅
- [x] Analyzer implemented using proper ICSharpProcedureElement
- [x] Iterates through all methods and their operations
- [x] Detects HttpClient calls (GetAsync, PostAsync, PutAsync, DeleteAsync, PatchAsync)
- [x] Extracts HTTP verbs
- [x] Creates HttpClient elements
- [x] Creates SendsRequest links from methods to HTTP clients
- [x] Integrated into ProclaimerSliceFragmentBuilder

## Progress Tracking

**Completed:** 6/6 core analyzers (100%)
**All core analyzers complete!**

## Next Steps

1. Add more inter-element links (HandledBy for CQRS, UsesStorage for EF/Repositories)
2. Add URL extraction for HTTP clients (requires constant propagation)
3. Add table name extraction for EF entities
4. Add advanced analyzers (messaging, mapping, validation, configuration, caching)

---

Last Updated: 2025-12-07T20:25:00Z
