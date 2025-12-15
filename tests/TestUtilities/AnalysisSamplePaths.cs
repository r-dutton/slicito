using System.IO;

namespace Slicito.Tests.Common;

public static class AnalysisSamplePaths
{
    public static string GetSolutionPath()
    {
        var candidate = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "inputs", "AnalysisSamples", "AnalysisSamples.sln"));

        if (!File.Exists(candidate))
        {
            throw new FileNotFoundException($"AnalysisSamples solution not found at '{candidate}'", candidate);
        }

        return candidate;
    }
}
