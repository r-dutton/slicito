# Slicito.Proclaimer State Document

This document tracks progress on integrating TheProclaimer into Slicito as a native extension.

**Last Updated**: 2025-12-07T19:45:00Z  
**Current Phase**: Phase 1 - Create Slicito.Proclaimer project & schema (Complete!)

---

## Phase 0 – Recon & design (status: done)

### Tasks

- [x] Discover existing Slicito projects and structure
- [x] Clone and analyze TheProclaimer repository
- [x] Map current graph/flow/analyzer components
- [x] Create SlicitoProclaimerDesign.md
- [x] Initialize state files
- [x] Verify build system and test infrastructure

---

## Phase 1 – Create Slicito.Proclaimer project & schema (status: done)

### Tasks

- [x] Create Slicito.Proclaimer project (netstandard2.0)
- [x] Add project references (Abstractions, Common, DotNet, ProgramAnalysis)
- [x] Implement ProclaimerAttributeNames
- [x] Implement ProclaimerAttributeValues
- [x] Implement ProclaimerTypes (with all 30+ element types and 27 link types)
- [x] Implement ProclaimerSchema
- [x] Add to solution file
- [x] Add minimal schema tests (7 tests, all passing)

### Notes

**Completed**:
1. Created `src/Slicito.Proclaimer/` project with proper references
2. Implemented complete schema mapping from GraphKit to Slicito:
   - 30+ element types (endpoints, CQRS, data access, messaging, etc.)
   - 27 link types (calls, sends_request, publishes, etc.)
   - Attribute names and values matching GraphKit constants
3. ProclaimerTypes implements IProgramTypes for compatibility
4. Created test project with comprehensive schema validation
5. All 7 tests pass successfully

**Key Design Decisions**:
- Used attribute-based discrimination (Kind attribute) to distinguish element/link types
- Implemented IProgramTypes for compatibility with Slicito's program analysis infrastructure
- Set endpoints and background services as root element types
- Stubbed unused IProgramTypes members (Operation, Call, NestedProcedures) since Proclaimer operates at higher abstraction level

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
