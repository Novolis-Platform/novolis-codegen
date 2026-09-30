using System.IO.Abstractions;

namespace Novolis.CodeGen.Bindings;

/// <summary>Declares a non-generated companion file that must exist before emit.</summary>
/// <param name="RelativePath">Path relative to the repository root.</param>
/// <param name="Description">Human-readable reason the file is required.</param>
public sealed record CompanionDeclaration(
    string RelativePath,
    string Description);
