using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Novolis.CodeGen.Bindings.Roslyn;

public static class HookDiscovery
{
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

public static class SyntaxRewriters
{
    public static CompilationUnitSyntax EnsureUsing(CompilationUnitSyntax unit, string namespaceName)
    {
        if (unit.Usings.Any(u => u.Name?.ToString() == namespaceName))
            return unit;

        var usingDirective = Microsoft.CodeAnalysis.CSharp.SyntaxFactory.UsingDirective(
            Microsoft.CodeAnalysis.CSharp.SyntaxFactory.ParseName(namespaceName));
        return unit.WithUsings(unit.Usings.Add(usingDirective));
    }
}
