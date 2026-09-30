using System.IO.Abstractions;

namespace Novolis.CodeGen.Bindings;

/// <summary>Orchestrates a complete binding codegen pass for a repository.</summary>
public interface IBindingCodegenHost
{
    /// <summary>Runs all configured emit jobs.</summary>
    /// <param name="options">Codegen options.</param>
    /// <param name="log">Optional log writer.</param>
    /// <returns>Process exit code (0 on success).</returns>
    int GenerateAll(BindingCodegenOptions options, TextWriter? log = null);
}
