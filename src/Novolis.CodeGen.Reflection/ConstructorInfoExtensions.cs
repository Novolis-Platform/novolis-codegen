using System.Reflection;

namespace Novolis.CodeGen.Reflection;

/// <summary>Display-name helpers for <see cref="ConstructorInfo"/>.</summary>
public static class ConstructorInfoExtensions
{
    /// <summary>Returns a short display name: <c>TypeName(paramTypes...)</c>.</summary>
    /// <param name="constructorInfo">Constructor to describe.</param>
    /// <returns>Display name.</returns>
    public static string GetDisplayName(this ConstructorInfo constructorInfo)
    {
        var parameters = constructorInfo.GetParameters();
        var parameterTypes = parameters.Select(p => p.ParameterType.GetDisplayName());
        return $"{constructorInfo.DeclaringType?.GetDisplayName()}({string.Join(", ", parameterTypes)})";
    }

    /// <summary>Returns a full friendly name including namespace-qualified parameter types.</summary>
    /// <param name="constructorInfo">Constructor to describe.</param>
    /// <returns>Full display name.</returns>
    public static string GetFullDisplayName(this ConstructorInfo constructorInfo)
    {
        var parameters = constructorInfo.GetParameters();
        var parameterTypes = parameters.Select(p => p.ParameterType.GetFullFriendlyName());
        return $"{constructorInfo.DeclaringType?.GetFriendlyName()}({string.Join(", ", parameterTypes)})";
    }
}