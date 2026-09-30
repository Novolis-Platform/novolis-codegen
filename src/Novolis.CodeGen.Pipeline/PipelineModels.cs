using System.Text.Json.Serialization;

namespace Novolis.CodeGen.Pipeline;

/// <summary>Runtime context for a single pipeline step execution.</summary>
public sealed class PipelineContext
{
    /// <summary>Repository layout.</summary>
    public required IPipelineLayout Layout { get; init; }

    /// <summary>Step log writer (typically <c>step.log</c>).</summary>
    public required TextWriter Log { get; init; }

    /// <summary>When <see langword="true"/>, skip detection is disabled.</summary>
    public required bool Force { get; init; }

    /// <summary>Repository root (shortcut for <see cref="Layout"/>.<see cref="IPipelineLayout.RepoRoot"/>).</summary>
    public string RepoRoot => Layout.RepoRoot;

    /// <summary>Steps root directory.</summary>
    public string StepsRoot => Layout.StepsRoot;

    /// <summary>Resolves a step directory path.</summary>
    /// <param name="stepId">Step identifier.</param>
    /// <returns>Absolute step directory.</returns>
    public string StepDir(string stepId) => Layout.StepDir(stepId);

    /// <summary>Resolves a step artifacts directory path.</summary>
    /// <param name="stepId">Step identifier.</param>
    /// <returns>Absolute artifacts directory.</returns>
    public string StepArtifactsDir(string stepId) => Layout.StepArtifactsDir(stepId);
}
