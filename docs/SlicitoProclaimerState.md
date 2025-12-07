# Slicito.Proclaimer State Document

This document tracks progress on integrating TheProclaimer into Slicito as a native extension.

**Last Updated**: 2025-12-07T19:33:00Z  
**Current Phase**: Phase 0 - Recon & design

---

## Phase 0 – Recon & design (status: in_progress)

### Tasks

- [x] Discover existing Slicito projects and structure
  - Explored core projects: Slicito.Abstractions, Slicito.DotNet, Slicito.ProgramAnalysis, Slicito.VisualStudio
  - Verified build system works (core libs build successfully on Linux)
  - Examined existing ASP.NET support in Slicito.DotNet.AspNetCore
  - Reviewed IController pattern and Graph/Tree models

- [x] Clone and analyze TheProclaimer repository
  - Repository is now public at https://github.com/r-dutton/TheProclaimer
  - Core library: GraphKit - contains all analysis logic
  - CLI tool: FlowGrep - command-line interface
  - Key components identified:
    - **Node Types**: Controllers, Endpoints, HttpClient, CQRS handlers, EF contexts, Message publishers/subscribers, Background services, etc.
    - **Edge Kinds**: Calls, SendsRequest, HandledBy, Publishes, Queries, UsesStorage, etc.
    - **Analyzers**: Project-level analyzers for controllers, HTTP, messaging, repositories, caching, configuration, etc.
    - **Flow Analysis**: Interprocedural analysis with provenance tracking and confidence scoring

- [x] Map current graph/flow/analyzer components
  - GraphKit uses custom graph model (GraphNode, GraphEdge) with JSON serialization
  - FlowAnalysis uses IOperation visitors for interprocedural analysis
  - Extensive analyzer coverage: controllers, CQRS, messaging, EF, HTTP clients, background services
  - Edge properties include provenance, confidence, transforms

- [x] Create SlicitoProclaimerDesign.md
  - Initial design document created with architecture overview
  - Schema mapping from GraphKit node types to Slicito element types
  - Component mapping from TheProclaimer to Slicito.Proclaimer

- [x] Initialize state files
  - Created slicito_proclaimer_state.json with phase/task structure
  - Creating this SlicitoProclaimerState.md file

- [ ] Update design document with actual TheProclaimer details
  - Need to refine schema based on actual NodeTypes and EdgeKinds
  - Document analyzer patterns and how they map to Slicito's program analysis

- [ ] Verify build system and test infrastructure
  - Check how to add new test projects
  - Understand test patterns in existing Slicito tests

### Notes

**TheProclaimer Architecture Insights**:
- Uses IOperation visitors for deep code analysis (similar to Slicito's approach)
- Tracks provenance (where facts come from) and confidence (how certain we are)
- Heavy use of interprocedural analysis to follow call chains
- Specialized analyzers for different patterns (controllers, CQRS, messaging, etc.)
- Graph model is rich with metadata (props, tags, evidence, transforms)

**Slicito Architecture Insights**:
- Element/Link model with typed attributes
- Schema defines element types, link types, and their relationships
- ITypedSliceFragment for domain-specific views
- IController for VS integration returning Graph or Tree models
- Existing DotNet support with AspNetCore endpoint detection

**Key Design Decisions**:
1. Map GraphKit node types to Slicito ElementType using attribute-based discrimination
2. Map GraphKit edge kinds to Slicito LinkType similarly
3. Port analyzer logic into ProclaimerSliceFragmentBuilder
4. Re-express flow analysis using Slicito's ISlice link queries
5. Preserve provenance/confidence via element/link attributes

**Open Questions**:
1. How to handle GraphKit's "transform" concept (method/type transformations in flows)?
2. Should we support cross-solution analysis from day one or add it later?
3. How to map GraphKit's tags to Slicito's attribute model?
4. Performance considerations for large codebases with extensive interprocedural analysis?

---

## Phase 1 – Create Slicito.Proclaimer project & schema (status: pending)

### Tasks

- [ ] Create Slicito.Proclaimer project
- [ ] Add project references
- [ ] Implement ProclaimerAttributeNames
- [ ] Implement ProclaimerAttributeValues  
- [ ] Implement ProclaimerTypes
- [ ] Implement ProclaimerSchema
- [ ] Add to solution file
- [ ] Add minimal schema tests

### Notes

(Notes will be added as we work through this phase)

---

## Phase 2 – Slice fragment (services + endpoints + HttpClients) (status: pending)

### Tasks

- [ ] Define IProclaimerSliceFragment interface
- [ ] Implement service discovery from projects
- [ ] Implement endpoint discovery using Slicito.DotNet.AspNetCore
- [ ] Implement HttpClient detection
- [ ] Create BelongsToService links
- [ ] Create SendsHttpRequest links
- [ ] Add validation tests

### Notes

(Notes will be added as we work through this phase)

---

## Phase 3 – Flow analysis (first cut) (status: pending)

### Tasks

- [ ] Define FlowNode model
- [ ] Implement ProclaimerFlowService
- [ ] Implement BFS/DFS traversal over links
- [ ] Add logging
- [ ] Add flow service tests

### Notes

(Notes will be added as we work through this phase)

---

## Phase 4 – Labels & view builders (status: pending)

### Tasks

- [ ] Implement ProclaimerLabelProvider
- [ ] Implement ProclaimerFlowGraphBuilder
- [ ] Optionally implement ProclaimerFlowTreeBuilder
- [ ] Add tests for label generation
- [ ] Add tests for graph building

### Notes

(Notes will be added as we work through this phase)

---

## Phase 5 – VS integration (status: pending)

### Tasks

- [ ] Implement ProclaimerFlowGraphController
- [ ] Wire up slice building with Proclaimer fragment
- [ ] Implement endpoint selection heuristic
- [ ] Implement navigation commands
- [ ] Optionally implement ProclaimerFlowTreeController
- [ ] Test in Visual Studio

### Notes

(Notes will be added as we work through this phase)

---

## Phase 6 – Port advanced Proclaimer semantics (status: pending)

### Tasks

- [ ] Add repository/DB detection
- [ ] Add messaging flow detection
- [ ] Add background service detection
- [ ] Implement flow grouping/deduplication
- [ ] Add advanced flow semantics

### Notes

(Notes will be added as we work through this phase)

---

## Phase 7 – Cleanup & migration (status: pending)

### Tasks

- [ ] Mark legacy Proclaimer code as deprecated
- [ ] Add compatibility layer if needed
- [ ] Update documentation
- [ ] Update README

### Notes

(Notes will be added as we work through this phase)

---

## Session Log

### Session 1 - 2025-12-07

**Activities**:
1. Explored Slicito repository structure
2. Built core Slicito libraries successfully (Abstractions, DotNet, ProgramAnalysis, Common)
3. Cloned TheProclaimer repository (now public)
4. Analyzed GraphKit architecture and constants
5. Created initial design document and state files

**Next Steps**:
1. Update design document with specific NodeTypes/EdgeKinds mapping
2. Complete Phase 0 by verifying test infrastructure
3. Begin Phase 1: Create Slicito.Proclaimer project

---
