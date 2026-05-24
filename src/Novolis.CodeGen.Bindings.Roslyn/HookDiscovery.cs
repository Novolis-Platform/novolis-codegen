using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Novolis.CodeGen.Bindings.Roslyn;

/// <summary>Discovers <see cref="ICodegenHook{TPhase, TContext}"/> implementations from assemblies.</summary>
public static class HookDiscovery
{
    /// <summary>
    /// Loads all concrete hook types from <paramref name="assemblies"/> and orders them by <see cref="ICodegenHook{TPhase, TContext}.Order"/>.
    /// </summary>
    /// <typeparam name="TPhase">Phase enum type.</typeparam>
    /// <typeparam name="TContext">Context type.</typeparam>
    /// <param name="assemblies">Assemblies to scan.</param>
    /// <returns>Ordered hook instances.</returns>
    public static IReadOnlyList<ICodegenHook<TPhase, TContext>> Discover<TPhase, TContext>(
        params Assembly[] assemblies)
        where TPhase : struct, Enum
        where TContext : Bindings.BindingEmitContext
    {
        var hookType = typeof(ICodegenHook<TPhase, TContext>);
        var hooks = new List<ICodegenHook<TPhase, TContext>>();

        foreach (var assembly in assemblies.Distinct())
        {
            foreach (var type in assembly.GetTypes())
            {
                if (type.IsAbstract || type.IsInterface || !hookType.IsAssignableFrom(type))
                    continue;

                if (Activator.CreateInstance(type) is ICodegenHook<TPhase, TContext> hook)
                    hooks.Add(hook);
            }
        }

        return hooks.OrderBy(h => h.Order).ThenBy(h => h.GetType().FullName, StringComparer.Ordinal).ToList();
    }
}

/// <summary>Small Roslyn rewriters used by codegen hooks.</summary>
public static class SyntaxRewriters
{
    /// <summary>Ensures a using directive for <paramref name="namespaceName"/> is present.</summary>
    /// <param name="unit">Compilation unit.</param>
    /// <param name="namespaceName">Namespace to import.</param>
    /// <returns>Updated compilation unit.</returns>
    public static CompilationUnitSyntax EnsureUsing(CompilationUnitSyntax unit, string namespaceName)
    {
        if (unit.Usings.Any(u => u.Name?.ToString() == namespaceName))
            return unit;

        var usingDirective = Microsoft.CodeAnalysis.CSharp.SyntaxFactory.UsingDirective(
            Microsoft.CodeAnalysis.CSharp.SyntaxFactory.ParseName(namespaceName));
        return unit.WithUsings(unit.Usings.Add(usingDirective));
    }
}
