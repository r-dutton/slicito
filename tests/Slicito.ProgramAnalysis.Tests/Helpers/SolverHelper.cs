using Slicito.ProgramAnalysis.SymbolicExecution.SmtSolver;

namespace Slicito.ProgramAnalysis.Tests.Helpers;

internal static class SolverHelper
{
    private static string? ResolveRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null && !File.Exists(Path.Combine(current.FullName, "Slicito.sln")))
        {
            current = current.Parent;
        }

        return current?.FullName;
    }

    private static string ResolveZ3Path()
    {
        var explicitPath = Environment.GetEnvironmentVariable("Z3_PATH");
        if (!string.IsNullOrWhiteSpace(explicitPath) && File.Exists(explicitPath))
        {
            return explicitPath;
        }

        var repoRoot = ResolveRepoRoot();
        if (repoRoot is not null)
        {
            var toolPath = Path.Combine(repoRoot, ".tools", "z3", "bin", OperatingSystem.IsWindows() ? "z3.exe" : "z3");
            if (File.Exists(toolPath))
            {
                return toolPath;
            }
        }

        return "z3";
    }

    public static SmtLibCliSolverFactory CreateSolverFactory(TestContext testContext)
    {
        var z3Path = ResolveZ3Path();
        testContext.WriteLine($"Using Z3 at: {z3Path}");
        return new(z3Path, ["-in"], line => testContext.WriteLine($"Z3: {line}"));
    }
}
