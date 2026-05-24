namespace Novolis.CodeGen.Pipeline;

public interface IPipelineStep
{
    string Id { get; }

    string Description { get; }

    IReadOnlyList<string> DependsOn { get; }

    IReadOnlyList<string> InputPaths(PipelineContext context);

    IReadOnlyList<string> ExpectedOutputPaths(PipelineContext context);

    ValueTask<StepExecutionResult> ExecuteAsync(PipelineContext context, CancellationToken cancellationToken);
}
