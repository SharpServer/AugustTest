using System;
using System.Linq;
using System.Reflection;

namespace Sliced.API.Features;

public static class TypeParser
{
    /// <summary>
    /// 文字列から、TBaseを継承している型を取得します。
    /// 型名・完全修飾名のどちらでも検索できます。
    /// </summary>
    public static bool TryParse<TBase>(
        string value,
        out Type? result,
        bool ignoreCase = true)
    {
        result = null;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        StringComparison comparison = ignoreCase
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        result = AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(GetLoadableTypes)
            .FirstOrDefault(type =>
                typeof(TBase).IsAssignableFrom(type) &&
                type != typeof(TBase) &&
                !type.IsAbstract &&
                !type.IsInterface &&
                (
                    string.Equals(type.Name, value, comparison) ||
                    string.Equals(type.FullName, value, comparison)
                ));

        return result is not null;
    }

    /// <summary>
    /// 文字列から、TBaseを継承しているクラスのインスタンスを生成します。
    /// 引数なしコンストラクターが必要です。
    /// </summary>
    public static bool TryCreate<TBase>(
        string value,
        out TBase? instance,
        bool ignoreCase = true)
    {
        instance = default;

        if (!TryParse<TBase>(value, out Type? type, ignoreCase))
            return false;

        try
        {
            instance = (TBase?)Activator.CreateInstance(type!);
            return instance is not null;
        }
        catch
        {
            return false;
        }
    }

    private static Type[] GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            return exception.Types
                .Where(type => type is not null)
                .Cast<Type>()
                .ToArray();
        }
    }
}