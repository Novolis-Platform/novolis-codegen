using System.Diagnostics;

namespace Novolis.CodeGen.Pipeline;

/// <summary>Runs pipeline steps sequentially with logging, skip detection, and result persistence.</summary>
public sealed class PipelineRunner
{
    private readonly IReadOnlyList<IPipelineStep> _steps;
    private readonly IPipelineLayout _layout;

    /// <summary>Creates a runner for the given steps and repository layout.</summary>
    /// <param name="steps">Ordered step implementations.</param>
    /// <param name="layout">Repository layout.</param>
    public PipelineRunner(IEnumerable<IPipelineStep> steps, IPipelineLayout layout)
    {
        _steps = steps.ToList();
        _layout = layout;
    }

    /// <summary>Runs the steps identified by <paramref name="stepIds"/> in order.</summary>
    /// <param name="stepIds">Step identifiers to run.</param>
    /// <param name="force">When <see langword="true"/>, disables skip detection.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Process exit code (0 on success).</returns>
    /// <exception cref="InvalidOperationException">When a step id is unknown.</exception>
    public async Task<int> RunProfileAsync(
        IReadOnlyList<string> stepIds,
        bool force,
        CancellationToken cancellationToken = default)
    {
        var selected = new List<IPipelineStep>();
        foreach (var id in stepIds)
        {
            var step = _steps.FirstOrDefault(s => string.Equals(s.Id, id, StringComparison.Ordinal))
                       ?? throw new InvalidOperationException($"Unknown step '{id}'.");
            selected.Add(step);
        }

        return await RunStepsAsync(selected, force, cancellationToken);
    }

    /// <summary>Runs a single step by identifier.</summary>
    /// <param name="stepId">Step identifier.</param>
    /// <param name="force">When <see langword="true"/>, disables skip detection.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Process exit code (0 on success).</returns>
    /// <exception cref="InvalidOperationException">When the step id is unknown.</exception>
    public async Task<int> RunStepAsync(string stepId, bool force, CancellationToken cancellationToken = default)
    {
        var step = _steps.FirstOrDefault(s => string.Equals(s.Id, stepId, StringComparison.Ordinal))
                   ?? throw new InvalidOperationException($"Unknown step '{stepId}'.");
        return await RunStepsAsync([step], force, cancellationToken);
    }

    private async Task<int> RunStepsAsync(IReadOnlyList<IPipelineStep> steps, bool force, CancellationToken cancellationToken)
    {
        foreach (var step in steps)
        {
            var exit = await RunSingleStepAsync(step, force, cancellationToken);
            if (exit != 0)
                return exit;
        }

        return 0;
    }

    private async Task<int> RunSingleStepAsync(IPipelineStep step, bool force, CancellationToken cancellationToken)
    {
        var stepDir = _layout.StepDir(step.Id);
        Directory.CreateDirectory(stepDir);

        var logPath = Path.Combine(stepDir, "step.log");
        await using var logStream = new FileStream(logPath, FileMode.Create, FileAccess.Write, FileShare.Read);
        await using var logWriter = new StreamWriter(logStream, new System.Text.UTF8Encoding(false)) { AutoFlush = true };

        var started = DateTimeOffset.UtcNow;
        await logWriter.WriteLineAsync($"# {step.Id} — {started:O}");
        await logWriter.WriteLineAsync($"# force={force}");
        await logWriter.WriteLineAsync();

        var context = new PipelineContext
        {
            Layout = _layout,
            Log = logWriter,
            Force = force,
        };

        var previous = StepResultWriter.TryRead(stepDir);
        if (StepSkipEvaluator.ShouldSkip(step, context, previous, out var skipReason))
        {
            var skippedDoc = new StepResultDocument
            {
                StepId = step.Id,
                Status = StepStatus.Skipped,
                StartedUtc = started,
                DurationMs = 0,
                Inputs = StepFileFingerprint.HashFiles(step.InputPaths(context), context.RepoRoot),
                Outputs = previous?.Outputs ?? [],
                SkipReason = skipReason,
            };
            StepResultWriter.Write(stepDir, skippedDoc);
            await logWriter.WriteLineAsync($"SKIPPED: {skipReason}");
            Console.WriteLine($"{step.Id}: skipped ({skipReason})");
            return 0;
        }

        var sw = Stopwatch.StartNew();
        try
        {
            var result = await step.ExecuteAsync(context, cancellationToken);
            sw.Stop();

            var doc = new StepResultDocument
            {
                StepId = step.Id,
                Status = result.Status,
                StartedUtc = started,
                DurationMs = sw.ElapsedMilliseconds,
                Inputs = result.Inputs.Count > 0
                    ? new Dictionary<string, string>(result.Inputs, StringComparer.Ordinal)
                    : StepFileFingerprint.HashFiles(step.InputPaths(context), context.RepoRoot),
                Outputs = result.Outputs.ToList(),
                SkipReason = result.SkipReason,
                Error = result.Error,
            };
            StepResultWriter.Write(stepDir, doc);

            if (result.Status == StepStatus.Failed)
            {
                await logWriter.WriteLineAsync($"FAILED: {result.Error?.Message}");
                Console.Error.WriteLine($"{step.Id}: failed — see {logPath}");
                return 1;
            }

            Console.WriteLine($"{step.Id}: {result.Status.ToString().ToLowerInvariant()} ({sw.ElapsedMilliseconds}ms)");
            return 0;
        }
        catch (Exception ex)
        {
            sw.Stop();
            var doc = new StepResultDocument
            {
                StepId = step.Id,
                Status = StepStatus.Failed,
                StartedUtc = started,
                DurationMs = sw.ElapsedMilliseconds,
                Inputs = StepFileFingerprint.HashFiles(step.InputPaths(context), context.RepoRoot),
                Error = new StepErrorRecord { Message = ex.Message, Type = ex.GetType().FullName },
            };
            StepResultWriter.Write(stepDir, doc);
            await logWriter.WriteLineAsync($"EXCEPTION: {ex}");
            Console.Error.WriteLine($"{step.Id}: exception — see {logPath}");
            return 1;
        }
    }
}
