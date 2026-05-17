// This code is copied from Namotion.Reflection and modified:
//-----------------------------------------------------------------------
// <copyright file="ReflectionExtensions.cs" company="NSwag">
//     Copyright (c) Rico Suter. All rights reserved.
// </copyright>
// <license>https://github.com/NSwag/NSwag/blob/master/LICENSE.md</license>
// <author>Rico Suter, mail@rsuter.com</author>
//-----------------------------------------------------------------------

using System.Reflection;

namespace Novolis.CodeGen.Reflection;

/// <summary>
/// Object extensions.
/// </summary>
public static class ObjectExtensions
{
    /// <summary>Determines whether the specified property name exists.</summary>
    /// <param name="obj">The object.</param>
    /// <param name="propertyName">Name of the property.</param>
    /// <returns><c>true</c> if the property exists; otherwise, <c>false</c>.</returns>
    public static bool HasProperty(this object? obj, string propertyName)
        => obj?.GetType().GetRuntimeProperty(propertyName) != null;

    /// <summary>Determines whether the specified property name exists.</summary>
    /// <param name="obj">The object.</param>
    /// <param name="propertyName">Name of the property.</param>
    /// <param name="value">The value.</param>
    /// <param name="defaultValue">The default value if the property does not exist.</param>
    /// <returns>The property or the default value.</returns>
    public static bool TryGetPropertyValue<T>(this object? obj, string propertyName, out T? value, T? defaultValue = default)
    {
        value = (T?)obj?.GetType().GetRuntimeProperty(propertyName)?.GetValue(obj) ?? defaultValue;
        return value != null;
    }
}

