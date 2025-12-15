====== TASK 02 — Ensure Slicito.Proclaimer “core spine” is real and canonical ======
GOAL:
- Slicito.Proclaimer project exists, builds, and is referenced correctly.
- Proclaimer schema/types exist and are used.
- No legacy GraphDocument pipeline is treated as canonical.

STEPS:
1) Inspect src/Slicito.Proclaimer:
   - ensure csproj references appropriate Slicito projects
   - fix any incorrect package props or analyzers references
2) Implement/confirm:
   - ProclaimerTypes
   - ProclaimerSchema (schema extension)
   - stable names for ElementTypes/LinkTypes/attributes
3) Ensure schema includes at minimum:
   - elements: Service, Endpoint, HttpClient, Repository, Database, Queue/Topic, BackgroundService
   - links: BelongsToService, Calls, SendsHttpRequest, WritesTo/ReadsFrom, PublishesTo, ConsumesFrom
4) Ensure any existing legacy types are NOT used as canonical model in Slicito.Proclaimer.

VERIFY:
- ./.dotnet/dotnet build Slicito.sln succeeds

COMMIT MESSAGE:
- "feat(proclaimer): solidify schema and project spine"

DONE WHEN:
- Slicito.Proclaimer compiles cleanly and schema is stable/complete for parity work.




====== TASK 03 — Implement Proclaimer slice builder: Services + Endpoints (full fidelity) ======
GOAL:
- ProclaimerSliceFragmentBuilder populates:
  - Service elements (project->service mapping)
  - Endpoint elements (controllers + minimal APIs if applicable)
  - Endpoint -> Service links
  - Endpoint -> code navigation anchors (method element or file/line CodeLocation)

STEPS:
1) Implement IProclaimerSliceFragment + ProclaimerSliceFragmentBuilder if missing/incomplete.
2) Services:
   - Create Service element for each relevant project/assembly
   - Attribute: ServiceName
   - Define deterministic IDs/keys (avoid duplicates)
3) Endpoints:
   - Prefer Slicito’s existing ASP.NET endpoint discovery if present.
   - Otherwise implement Roslyn discovery:
     - controllers: [ApiController], ControllerBase inheritance, route attributes
     - methods: [HttpGet]/[HttpPost]/etc, route templates
     - minimal APIs if used: MapGet/MapPost patterns
   - Create Endpoint elements with attributes:
     - ServiceName, HttpMethod, Route
   - Link Endpoint -> Service (BelongsToService)
   - Link Endpoint -> underlying method element (Calls) where feasible, else attach CodeLocation attributes for navigation.
4) Add or extend tests:
   - A small sample ASP.NET project under tests/samples (or generated during test) with known endpoints.
   - Assertions that slice contains expected endpoint elements and attributes.

VERIFY:
- build succeeds
- tests for endpoint discovery pass

COMMIT MESSAGE:
- "feat(proclaimer): slice builder for services and endpoints"

DONE WHEN:
- endpoints appear in slice with correct method/route/service and navigable locations.




====== TASK 04 — Implement Proclaimer slice builder: HttpClient/HTTP calls (typed + direct) ======
GOAL:
- Populate HttpClient elements and SendsHttpRequest links with useful attributes.

STEPS:
1) Implement HTTP discovery paths mirroring upstream:
   - direct HttpClient usage
   - typed clients patterns
   - (if upstream supports) Refit or other REST client patterns
2) Create HttpClient elements with attributes:
   - UrlTemplate when inferable
   - TargetService when inferable
3) Link:
   - caller method/endpoint/service -> HttpClient (SendsHttpRequest)
4) Tests:
   - Sample project includes at least:
     - direct HttpClient.GetAsync("https://...") usage
     - a typed client call
   - Assert slice has HttpClient elements + links.

VERIFY:
- build + tests pass

COMMIT MESSAGE:
- "feat(proclaimer): http client discovery and links"

DONE WHEN:
- HTTP calls show up in slice with usable metadata.




====== TASK 05 — Implement Repo/DB semantics in slice builder ======
GOAL:
- Port upstream repo/db discovery into slice facts:
  - Repository and Database elements
  - WritesTo / ReadsFrom links (or equivalent)

STEPS:
1) Identify upstream DB/repo patterns (EF Core, Dapper, raw SQL).
2) Implement discovery into slice:
   - Repository elements (types ending Repo, or DI registrations, etc.)
   - Database elements (DbContext, connection strings, etc.)
   - Links:
     - method/service/repo -> database (WritesTo/ReadsFrom) with attributes like Table/Collection if inferable
3) Tests:
   - Add a sample that performs EF SaveChanges and/or Dapper Execute.
   - Assert appropriate links are created.

VERIFY:
- build + tests pass

COMMIT MESSAGE:
- "feat(proclaimer): repository and database slice facts"

DONE WHEN:
- DB interactions are represented as slice links with minimal useful attributes.




====== TASK 06 — Implement Messaging semantics in slice builder ======
GOAL:
- Port upstream messaging publish/subscribe:
  - Queue/Topic elements
  - PublishesTo / ConsumesFrom links
  - MessageType attributes when inferable

STEPS:
1) Identify upstream messaging frameworks supported (MassTransit, Azure Service Bus, RabbitMQ clients, etc.).
2) Implement minimal + extendable discovery:
   - publishers -> queue/topic
   - consumers -> queue/topic
   - message type attribute if possible
3) Tests:
   - Add a sample with publish + consume patterns.
   - Assert slice contains links.

VERIFY:
- build + tests pass

COMMIT MESSAGE:
- "feat(proclaimer): messaging slice facts"

DONE WHEN:
- messaging interactions appear as slice elements/links.




====== TASK 07 — Port flow resolution semantics from upstream into ProclaimerFlowService ======
GOAL:
- Flow logic is not naive BFS as final.
- It preserves upstream grouping/dedup/path semantics, but implemented on slice primitives.

STEPS:
1) Study upstream flow engine(s) (TurboFlowEngine, FlowBuilder, etc.)
2) Implement ProclaimerFlowService operating on:
   - ISlice + ProclaimerTypes + link explorers
3) Port semantics:
   - grouping repeated hops
   - dedup/scc collapsing if present
   - main-path vs side-effects if present
4) Add tests:
   - Use sample solution; produce deterministic “flow model” output structure
   - Prefer golden snapshot (JSON) of flow nodes/edges
   - If possible, run upstream on same sample and compare output sets.

VERIFY:
- build + tests pass
- flows stable across runs

COMMIT MESSAGE:
- "feat(proclaimer): port full flow resolution semantics"

DONE WHEN:
- flow service matches upstream behavior on sample(s) per tests.




====== TASK 08 — Implement labels + Slicito Graph/Tree builders ======
GOAL:
- Slicito-native visual model creation:
  - labels are informative
  - graph edges labeled meaningfully
  - optional tree view mirrors logical structure

STEPS:
1) ProclaimerLabelProvider:
   - endpoint label: “Service / METHOD route”
   - http label: “HTTP → TargetService (url)”
   - repo/db label: “DB: X”
   - messaging label: “Publishes/Consumes MessageType”
2) ProclaimerFlowGraphBuilder outputs Slicito Graph model with attributes for styling
3) ProclaimerFlowTreeBuilder outputs Tree model (recommended)
4) Tests:
   - verify produced graph/tree contains expected nodes/edges from known flows.

VERIFY:
- build + tests pass

COMMIT MESSAGE:
- "feat(proclaimer): graph/tree builders and labels"

DONE WHEN:
- flow models render-ready and deterministic.




====== TASK 09 — VS integration: controllers + registration + navigation ======
GOAL:
- Proclaimer appears as native Slicito views in VS extension.

STEPS:
1) Implement ProclaimerFlowGraphController:
   - builds slice fragment
   - picks endpoint root (temporary heuristic)
   - runs ProclaimerFlowService
   - returns Graph model
2) Implement ProclaimerFlowTreeController (recommended)
3) Register controllers in Slicito VS integration (menu/tool window)
4) Navigation:
   - clicking/double-clicking uses CodeLocation or method element to open source
5) Add a small dev doc or screenshot note in docs.

VERIFY:
- build succeeds (including VS extension projects, or documented if non-Windows)
- controller registration is present in code

COMMIT MESSAGE:
- "feat(vs): add proclaimer flow views and navigation"

DONE WHEN:
- Proclaimer flows are visible in VS extension and navigable.




====== TASK 10 — Parity closure + cleanup (only after parity tests pass) ======
GOAL:
- ParityMatrix is fully checked.
- Legacy pipeline is deprecated/isolated.
- Docs updated.

STEPS:
1) Ensure docs/ProclaimerParityMatrix.md is fully checked off with test references.
2) Remove or isolate legacy GraphDocument flow pipeline from canonical path.
3) Update docs to declare Slicito.Proclaimer as canonical.
4) Run full build + test again.

VERIFY:
- build + tests pass
- parity matrix complete

COMMIT MESSAGE:
- "docs+cleanup: finalize proclaimer port and deprecate legacy pipeline"

DONE WHEN:
- parity is complete and stable.

