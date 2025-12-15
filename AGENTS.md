# Agent Instructions

## Build & Test (Codex Cloud)
Run the following commands in order to prepare and verify the environment:

1. `./scripts/bootstrap-dotnet.sh`
2. `./scripts/bootstrap-z3.sh`
3. `./.dotnet/dotnet build Slicito.sln`
4. `./.dotnet/dotnet test Slicito.sln`

If the .NET terminal logger crashes in this environment, rerun the build/test commands with `-tlp:disable` (or set `DOTNET_CLI_DISABLE_TERMINAL_LOGGER=1`).
