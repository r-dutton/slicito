====== TASK00 — Make build+tests runnable in the sandbox ======
GOAL:
- Ensure we can reliably run build/tests in Codex Cloud without manual setup:
  - local dotnet bootstrap script
  - z3 availability if tests require it
  - required sample inputs available (or tests made self-contained)
- Remove “environment blockers” so later tasks can’t rationalize stopping early.

STEPS:
1) Inspect repo for build/test expectations:
   - read global.json, Directory.Build.props, README, CI workflows
   - locate failing tests referencing inputs/AnalysisSamples/AnalysisSamples.sln and z3
2) Add scripts:
   - scripts/bootstrap-dotnet.sh:
     - install pinned SDK (global.json) into ./.dotnet (use dotnet-install.sh)
     - print dotnet --info
   - scripts/bootstrap-z3.sh:
     - install a linux z3 binary into ./.tools/z3 (or download a static release)
     - add a small wrapper to ensure PATH includes it for test runs
3) Make tests self-contained:
   - Find references to inputs/AnalysisSamples/AnalysisSamples.sln
   - EITHER:
     a) add the missing AnalysisSamples solution into the repo under inputs/AnalysisSamples/ (preferred if small and license-safe), OR
     b) modify tests so they create/build a small sample solution at test-time, OR
     c) modify tests to skip with a clear message if sample inputs missing, BUT only if you also add an equivalent self-contained test suite for Proclaimer (later tasks).
   - Fix z3 dependency:
     - Ensure tests can find z3 via PATH or config.
4) Update AGENTS.md:
   - Add “Build & Test (Codex Cloud)” section with exact commands:
     - ./scripts/bootstrap-dotnet.sh
     - ./scripts/bootstrap-z3.sh (if needed)
     - ./.dotnet/dotnet build Slicito.sln
     - ./.dotnet/dotnet test Slicito.sln
5) Run:
   - ./scripts/bootstrap-dotnet.sh
   - ./scripts/bootstrap-z3.sh (if applicable)
   - ./.dotnet/dotnet build Slicito.sln
   - ./.dotnet/dotnet test Slicito.sln
6) Update docs/SlicitoProclaimerState.md and docs/slicito_proclaimer_state.json to record:
   - dotnet version installed
   - z3 provision method
   - how AnalysisSamples is handled

VERIFY:
- ./.dotnet/dotnet build Slicito.sln succeeds
- ./.dotnet/dotnet test Slicito.sln succeeds OR failures are now strictly unrelated to environment (and you fix them)

COMMIT MESSAGE:
- "chore: bootstrap dotnet/z3 and make tests runnable"

DONE WHEN:
- Build + tests run in the sandbox with only repo scripts and no manual steps.

COMPLETED: 2025-12-15T21:19:12+00:00 | commit: 6380a9d (pre-amend)

====== TASK 01 — Clone upstream TheProclaimer and lock in parity matrix ======
GOAL:
- Ensure upstream is available as spec and docs/ProclaimerParityMatrix.md is accurate and actionable.

STEPS:
1) Ensure _upstream/theproclaimer exists:
   - if missing: git clone https://github.com/r-dutton/theproclaimer _upstream/theproclaimer
   - ensure _upstream is gitignored (do not commit)
2) Audit upstream for:
   - graph model types
   - analyzers (endpoint/http/repo/db/messaging/background)
   - flow engines/formatters
   - configs/workspace mapping
3) Update docs/SlicitoProclaimerDesign.md:
   - add a mapping table: upstream component -> Slicito.Proclaimer target (slice facts / service / view builder / controller)
4) Update docs/ProclaimerParityMatrix.md:
   - add rows for each upstream capability category, including:
     - Service mapping
     - Endpoint discovery (controller + minimal APIs if present)
     - HTTP discovery (HttpClient + typed clients)
     - Repo & DB (EF/Dapper/raw SQL)
     - Messaging (if present)
     - Background services (if present)
     - Flow logic: grouping/dedup/path semantics
     - UI: graph/tree controllers and navigation
   - Each row must include: status + planned tests

VERIFY:
- _upstream exists and is not tracked by git
- ParityMatrix is complete enough to drive the remaining tasks

COMMIT MESSAGE:
- "docs: finalize upstream parity matrix and mapping"

DONE WHEN:
- ParityMatrix is concrete and test-oriented (not vague).

COMPLETED: 2025-12-15T21:32:20+00:00 | commit: 86093dd (pre-amend)

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

COMPLETED: 2025-12-15T21:50:46+00:00 | commit: ede31c1 (pre-amend)
