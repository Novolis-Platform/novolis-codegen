namespace Novolis.CodeGen.Pipeline;

/// <summary>Filesystem layout for a codegen pipeline repository.</summary>
public interface IPipelineLayout
{
    /// <summary>Repository root directory.</summary>
    string RepoRoot { get; }

    /// <summary>Root directory containing per-step folders.</summary>
    string StepsRoot { get; }

    /// <summary>Directory containing JSON manifest inputs.</summary>
    string ManifestDir { get; }

    /// <summary>Directory for a step's logs and <c>result.json</c>.</summary>
    /// <param name="stepId">Step identifier.</param>
    /// <returns>Absolute step directory path.</returns>
    string StepDir(string stepId);

    /// <summary>Directory for a step's artifact outputs.</summary>
    /// <param name="stepId">Step identifier.</param>
    /// <returns>Absolute artifacts directory path.</returns>
    string StepArtifactsDir(string stepId);
}
