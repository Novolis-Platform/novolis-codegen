namespace Novolis.CodeGen.Pipeline;

public interface IPipelineLayout
{
    string RepoRoot { get; }

    string StepsRoot { get; }

    string ManifestDir { get; }

    string StepDir(string stepId);

    string StepArtifactsDir(string stepId);
}
