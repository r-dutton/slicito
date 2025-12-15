using System.IO;

namespace Slicito.Tests.Common;

public static class ProclaimerSamplePaths
{
    public static string GetSolutionPath()
    {
        var candidate = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "inputs", "ProclaimerSamples", "ProclaimerSamples.sln"));

        if (!File.Exists(candidate))
        {
            throw new FileNotFoundException($"Proclaimer sample solution not found at '{candidate}'", candidate);
        }

        return candidate;
    }
}
