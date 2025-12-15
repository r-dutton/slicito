# Slicito.Proclaimer State Document

This document tracks progress on integrating TheProclaimer into Slicito as a native extension.

**Last Updated**: 2025-12-15T21:28:04Z
**Current Phase**: Phase 2 – Core Proclaimer spine (pending)

## Environment Readiness (Codex Cloud)

- .NET SDK **9.0.100** installed locally via `scripts/bootstrap-dotnet.sh` into `./.dotnet`.
- Z3 **4.12.2** installed via `scripts/bootstrap-z3.sh` into `./.tools/bin/z3`; tests auto-detect the local binary.
- Analysis sample solution lives under `tests/inputs/AnalysisSamples/` with assembly signing disabled for Linux builds and helper utilities resolving the path cross-platform.
- Use `./.dotnet/dotnet build Slicito.sln -tlp:disable` and `./.dotnet/dotnet test Slicito.sln -tlp:disable` to avoid the .NET terminal logger crash in this environment.

---

## Phase 0 – Environment bootstrap (status: done)

### Tasks

- [x] Ensure pinned .NET SDK installs locally for repeatable CI-free runs
- [x] Install Z3 into the repo-local tool path and wire tests to consume it
- [x] Make AnalysisSamples buildable on Linux and add helpers for locating it in tests
- [x] Capture the Codex Cloud build/test command sequence in `AGENTS.md`

### Notes

- Bootstrap scripts live under `scripts/bootstrap-dotnet.sh` and `scripts/bootstrap-z3.sh`.
- Tests can be executed without any external assets or machine-level installs.

---

## Phase 1 – Upstream parity mapping (status: done)

### Tasks

- [x] Clone upstream TheProclaimer under `_upstream/theproclaimer` (gitignored)
- [x] Audit GraphKit analyzers (controllers, HTTP, EF/repositories, messaging, background services)
- [x] Audit flow resolution and formatting (FlowAnalysis, Analysis passes, FlowBuilder markdown/JSON)
- [x] Capture workspace/service mapping inputs (`flow.workspace.json`, `flow.map.json`)
- [x] Create parity-oriented tracking doc (`docs/ProclaimerParityMatrix.md`) and mapping table in `docs/SlicitoProclaimerDesign.md`

### Notes

- Upstream GraphKit uses interprocedural flow analysis with dedicated passes for deduplication and HTTP/messaging link formation.
- Workspace configuration binds logical services to solutions + assembly names and base URLs.

---

## Phase 2 – Core Proclaimer spine (status: pending)

### Tasks

- [ ] Validate `Slicito.Proclaimer` project references and package props
- [ ] Finalize `ProclaimerTypes` and `ProclaimerSchema` names for canonical slice facts
- [ ] Add schema coverage for services, endpoints, HTTP clients, repositories/DB, messaging, background services
- [ ] Keep legacy GraphDocument pipelines non-canonical

### Notes

Planned deliverable: stable schema/types that compile cleanly and are ready for slice builders.

---

## Phase 3 – Slice fragment: services + endpoints (status: pending)

### Tasks

- [ ] Implement `IProclaimerSliceFragment` + builder
- [ ] Map projects/assemblies to Service elements
- [ ] Discover controller + minimal API endpoints with method/route attributes
- [ ] Create `BelongsToService` links and navigation anchors
- [ ] Add sample ASP.NET project + tests validating slice contents

---

## Phase 4 – Slice fragment: HTTP/Repo/Messaging/Background (status: pending)

### Tasks

- [ ] Detect HttpClient usage (raw + typed) and emit `SendsHttpRequest` links
- [ ] Detect repositories/EF/db interactions and emit `ReadsFrom/WritesTo` links
- [ ] Detect messaging publish/consume patterns and emit `PublishesTo/ConsumesFrom` links
- [ ] Detect background services and attach to owning services
- [ ] Expand tests to cover each interaction type

---

## Phase 5 – Flow semantics (status: pending)

### Tasks

- [ ] Implement `ProclaimerFlowService` with grouping/dedup/path semantics from upstream
- [ ] Add golden snapshot tests comparing flows to upstream FlowGrep output on shared samples

---

## Phase 6 – Labels & view builders (status: pending)

### Tasks

- [ ] Implement label provider for endpoints/HTTP/DB/messaging
- [ ] Implement graph + tree builders for VS/CLI consumption
- [ ] Add tests validating node/edge content and styling attributes

---

## Phase 7 – VS integration (status: pending)

### Tasks

- [ ] Implement VS controllers for graph/tree views
- [ ] Register menus/tool windows and navigation handlers
- [ ] Add developer doc/screenshots

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

### Session 2 - 2025-12-15

**Activities**:
1. Bootstrapped Codex Cloud environment (local .NET SDK + Z3, Linux-friendly AnalysisSamples)
2. Documented reproducible build/test commands in AGENTS.md
3. Audited upstream TheProclaimer analyzers, flow engine, and workspace inputs
4. Added parity matrix and upstream-to-Slicito mapping table

**Next Steps**:
1. Implement Proclaimer schema/types and wire into Slicito.Proclaimer project
2. Start slice fragment builder for services/endpoints/HTTP
