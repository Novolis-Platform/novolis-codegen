using System.Reflection;

namespace Novolis.CodeGen.Reflection;

public static class ConstructorInfoExtensions
{
    public static string GetDisplayName(this ConstructorInfo constructorInfo)
    {
        var parameters = constructorInfo.GetParameters();
        var parameterTypes = parameters.Select(p => p.ParameterType.GetDisplayName());
        return $"{constructorInfo.DeclaringType?.GetDisplayName()}({string.Join(", ", parameterTypes)})";
    }
    
    public static string GetFullDisplayName(this ConstructorInfo constructorInfo)
    {
        var parameters = constructorInfo.GetParameters();
        var parameterTypes = parameters.Select(p => p.ParameterType.GetFullFriendlyName());
        return $"{constructorInfo.DeclaringType?.GetFriendlyName()}({string.Join(", ", parameterTypes)})";
    }
}