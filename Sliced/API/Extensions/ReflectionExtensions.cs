using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Sliced.API.Extensions;

public static class ReflectionExtensions
{
    /// <summary>
    /// 指定したアセンブリ内から、TBaseを継承または実装している型を取得します。
    /// </summary>
    public static IReadOnlyList<Type> GetDerivedTypes<TBase>(
        this Assembly assembly,
        bool includeAbstract = false)
    {
        Type baseType = typeof(TBase);

        return assembly
            .GetLoadableTypes()
            .Where(type =>
                type != baseType &&
                baseType.IsAssignableFrom(type) &&
                (includeAbstract || !type.IsAbstract))
            .ToArray();
    }

    /// <summary>
    /// 読み込み可能な型だけを取得します。
    /// </summary>
    private static IEnumerable<Type> GetLoadableTypes(this Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            return exception.Types.OfType<Type>();
        }
    }
}