namespace Novolis.CodeGen.Reflection;

/// <summary>Provides extension methods for reflection.</summary>
public static class TypeExtensions
{
    /// <summary>Gets a human readable type name (e.g. DictionaryOfStringAndObject).</summary>
    /// <param name="type">The type.</param>
    /// <returns>The type name.</returns>
    public static string GetDisplayName(this Type type) => GetDisplayNameInternal(type);

    /// <summary>
    /// Gets the full display name of a Type. e.g. System.Collections.Generic.DictionaryOfStringAndObject
    /// </summary>
    /// <param name="type">The Type for which to retrieve the full display name.</param>
    /// <returns>The full display name of the Type, which includes the namespace and the display name.</returns>
    public static string GetFullDisplayName(this Type type)
    {
        var displayName = GetDisplayNameInternal(type);
        var namespaceName = type.Namespace;
        return namespaceName == null ? displayName : namespaceName + "." + displayName;
    }

    /// <summary>
    /// Gets the friendly name of the specified Type. e.g. Dictionary&lt;string, object&gt;
    /// </summary>
    /// <param name="type">The Type to get the friendly name for.</param>
    /// <returns>The friendly name of the specified Type.</returns>
    public static string GetFriendlyName(this Type type) => GetFriendlyNameInternal(type);

    /// <summary>
    /// Returns the full friendly name of the specified <see cref="Type"/>. e.g. System.Collections.Generic.Dictionary&lt;System.String, System.Object&gt;
    /// </summary>
    /// <param name="type">The <see cref="Type"/> to get the full friendly name for.</param>
    /// <returns>The full friendly name of the specified <see cref="Type"/>.</returns>
    public static string GetFullFriendlyName(this Type type)
    {
        var friendlyName = GetFriendlyNameInternal(type);
        var namespaceName = type.Namespace;
        return namespaceName == null ? friendlyName : namespaceName + "." + friendlyName;
    }
    
    private static string GetFriendlyNameInternal(this Type type)
    {
        var nType = type;
        if (nType.IsConstructedGenericType)
            return GetName(nType).FirstToken('`') + "<" +
                   string.Join(", ", nType.GenericTypeArguments
                       .Select(GetFriendlyName)) + ">";

        return GetName(nType);
    }
    
    private static string GetDisplayNameInternal(this Type type)
    {
        var nType = type;
        if (nType.IsConstructedGenericType)
            return GetName(nType).FirstToken('`') + "Of" +
                   string.Join("And", nType.GenericTypeArguments
                       .Select(GetDisplayName));

        return GetName(nType);
    }

    private static string GetName(Type type) =>
        type.Name switch
        {
            "Int16" => GetNullableDisplayName(type, "Short"),
            "Int32" => GetNullableDisplayName(type, "Integer"),
            "Int64" => GetNullableDisplayName(type, "Long"),
            _ => GetNullableDisplayName(type, type.Name)
        };

    private static string GetNullableDisplayName(Type type, string actual) 
        => (IsNullableType(type) ? "Nullable" : "") + actual;

    private static bool IsNullableType(Type type) 
        => type.IsConstructedGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
}