namespace Novolis.CodeGen.Pipeline;

public static class StepSkipEvaluator
{
    public static bool ShouldSkip(
        IPipelineStep step,
        PipelineContext context,
        StepResultDocument? previous,
        out string? reason)
    {
        reason = null;
        if (context.Force)
            return false;

        if (previous is null ||
            previous.Status is not (StepStatus.Succeeded or StepStatus.Skipped))
        {
            return false;
        }

        var currentInputs = StepFileFingerprint.HashFiles(step.InputPaths(context), context.RepoRoot);
        if (!InputsMatch(previous.Inputs, currentInputs))
            return false;

        foreach (var output in step.ExpectedOutputPaths(context))
        {
            var full = Path.IsPathRooted(output) ? output : Path.Combine(context.RepoRoot, output);
            if (!File.Exists(full))
                return false;
        }

        if (previous.Outputs.Count > 0)
        {
            foreach (var expected in previous.Outputs)
            {
                var full = ResolveOutputPath(context, step.Id, expected.Path);
                if (!File.Exists(full))
                    return false;

                if (!string.IsNullOrEmpty(expected.Sha256))
                {
                    var actual = StepFileFingerprint.Sha256Hex(full);
                    if (!string.Equals(actual, expected.Sha256, StringComparison.OrdinalIgnoreCase))
                        return false;
                }
            }
        }

        reason = "outputs up to date";
        return true;
    }

    private static bool InputsMatch(IReadOnlyDictionary<string, string> previous, Dictionary<string, string> current)
    {
        if (previous.Count != current.Count)
            return false;

        foreach (var (key, value) in current)
        {
            if (!previous.TryGetValue(key, out var prev) ||
                !string.Equals(prev, value, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }

    private static string ResolveOutputPath(PipelineContext context, string stepId, string recordedPath)
    {
        if (recordedPath.StartsWith("artifacts/", StringComparison.Ordinal))
            return Path.Combine(context.StepDir(stepId), recordedPath.Replace('/', Path.DirectorySeparatorChar));

        return Path.Combine(context.RepoRoot, recordedPath.Replace('/', Path.DirectorySeparatorChar));
    }
}
