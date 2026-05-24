namespace Novolis.CodeGen.Pipeline;

/// <summary>One executable step in a codegen or binding pipeline.</summary>
public interface IPipelineStep
{
    /// <summary>Stable step identifier (used for directories and CLI).</summary>
    string Id { get; }

    /// <summary>Short human-readable description.</summary>
    string Description { get; }

    /// <summary>Step identifiers that must succeed before this step runs.</summary>
    IReadOnlyList<string> DependsOn { get; }

    /// <summary>Input file paths (relative to <see cref="PipelineContext.RepoRoot"/>) fingerprinted for skip logic.</summary>
    /// <param name="context">Pipeline context.</param>
    /// <returns>Paths to hash before execution.</returns>
    IReadOnlyList<string> InputPaths(PipelineContext context);

    /// <summary>Output paths that must exist after a successful run.</summary>
    /// <param name="context">Pipeline context.</param>
    /// <returns>Expected output paths.</returns>
    IReadOnlyList<string> ExpectedOutputPaths(PipelineContext context);

    /// <summary>Executes the step.</summary>
    /// <param name="context">Pipeline context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Execution result (status, outputs, errors).</returns>
    ValueTask<StepExecutionResult> ExecuteAsync(PipelineContext context, CancellationToken cancellationToken);
}
