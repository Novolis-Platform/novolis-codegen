using System.Reflection;

namespace Novolis.CodeGen.Reflection;

/// <summary>Display-name helpers for <see cref="ParameterInfo"/>.</summary>
public static class ParameterInfoExtensions
{
    /// <summary>
    /// Returns the display name of a ParameterInfo object.
    /// </summary>
    /// <param name="parameterInfo">The ParameterInfo object to get the display name for.</param>
    /// <returns>The display name of the ParameterInfo object in the format: name : type</returns>
    public static string GetDisplayName(this ParameterInfo parameterInfo)
    {
        return $"{parameterInfo.Name} : {parameterInfo.ParameterType.GetFriendlyName()}";
    }

    /// <summary>
    /// Gets the full display name of the specified <see cref="ParameterInfo"/>.
    /// </summary>
    /// <param name="parameterInfo">The <see cref="ParameterInfo"/> object.</param>
    /// <returns>The full display name of the <paramref name="parameterInfo"/>.</returns>
    public static string GetFullDisplayName(this ParameterInfo parameterInfo)
    {
        return $"{parameterInfo.Name} : {parameterInfo.ParameterType.GetFullFriendlyName()}";
    }
}