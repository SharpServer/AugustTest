using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using ProjectMER.Features.Interfaces;
using UnityEngine;

using Logger = LabApi.Features.Console.Logger;

namespace AntiMeme.Maps;

/// <summary>
/// マップ側から <see cref="ObjectPrefab"/> の値を文字列で読み書きするための変換層です。
///
/// <para>
/// 派生クラスが宣言した public な get/set プロパティが、そのままオプション名になります。
/// 宣言用の属性も、キーを登録する表もありません。プロパティを生やせばマップエディタから見えます。
/// </para>
/// <para>
/// <c>Vector3</c> / <c>Color</c> / <c>Quaternion</c> はカンマ区切り、enum は名前でも数値でも受け付けます。
/// その他は <see cref="TypeConverter"/> を通します。
/// </para>
/// </summary>
public static class ObjectPrefabOptions
{
    /// <summary>
    /// 名札を差すためのオプション名です。
    /// </summary>
    public const string TagKey = nameof(ObjectPrefab.Tag);

    // 型ごとのプロパティ探索はマーカーの数だけ走るのでキャッシュする。
    private static readonly Dictionary<Type, PropertyInfo[]> PropertyCache = new Dictionary<Type, PropertyInfo[]>();

    /// <summary>
    /// オプションを Prefab へ反映します。読めなかったキーは警告を出して読み飛ばします。
    /// </summary>
    public static void Apply(ObjectPrefab prefab, IReadOnlyDictionary<string, string> options)
    {
        if (options is null || options.Count == 0) return;

        Dictionary<string, PropertyInfo> properties = GetProperties(prefab.GetType())
            .ToDictionary(property => property.Name, StringComparer.OrdinalIgnoreCase);

        foreach (KeyValuePair<string, string> option in options)
        {
            if (string.Equals(option.Key, TagKey, StringComparison.OrdinalIgnoreCase))
            {
                prefab.Tag = option.Value;

                continue;
            }

            if (!properties.TryGetValue(option.Key, out PropertyInfo property)) continue;

            if (!TryDeserialize(option.Value, property.PropertyType, out object value))
            {
                Logger.Warn(
                    $"[{prefab.GetType().Name}] オプション '{option.Key}={option.Value}' を " +
                    $"{property.PropertyType.Name} として読めませんでした。");

                continue;
            }

            // 値はマップファイル由来の外部入力で、setter 側で例外を投げる実装もありうる。
            try
            {
                property.SetValue(prefab, value);
            }
            catch (Exception exception)
            {
                Logger.Warn($"[{prefab.GetType().Name}] オプション '{option.Key}' の適用に失敗しました: {exception.Message}");
            }
        }
    }

    /// <summary>
    /// Prefab の現在値をオプション形式で書き出します。
    /// </summary>
    public static Dictionary<string, string> Collect(ObjectPrefab prefab)
    {
        Dictionary<string, string> options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (PropertyInfo property in GetProperties(prefab.GetType()))
        {
            if (TrySerialize(property.GetValue(prefab), property.PropertyType, out string serialized))
                options[property.Name] = serialized;
        }

        return options;
    }

    /// <summary>
    /// この型が受け付けるオプションの一覧を、ProjectMER のマップエディタが読める形で返します。
    /// 既定値を読むために、まだ <see cref="ObjectPrefab.Create"/> していないインスタンスを渡します。
    /// </summary>
    public static IReadOnlyList<ObjectPrefabOptionInfo> Describe(ObjectPrefab prefab)
        => GetProperties(prefab.GetType())
            .Select(property =>
            {
                TrySerialize(property.GetValue(prefab), property.PropertyType, out string defaultValue);

                return new ObjectPrefabOptionInfo
                {
                    Name = property.Name,
                    ValueType = DescribeType(property.PropertyType),
                    DefaultValue = defaultValue,
                    ConstraintDescription = property.PropertyType.IsEnum
                        ? string.Join("|", Enum.GetNames(property.PropertyType))
                        : null,
                };
            })
            .ToArray();

    /// <summary>
    /// 値を文字列にします。
    /// </summary>
    public static bool TrySerialize(object value, Type declaredType, out string serialized)
    {
        if (value is null)
        {
            serialized = string.Empty;

            return !declaredType.IsValueType || Nullable.GetUnderlyingType(declaredType) is not null;
        }

        switch (value)
        {
            case Vector2 vector2:
                serialized = Join(vector2.x, vector2.y);

                return true;
            case Vector3 vector3:
                serialized = Join(vector3.x, vector3.y, vector3.z);

                return true;
            case Vector4 vector4:
                serialized = Join(vector4.x, vector4.y, vector4.z, vector4.w);

                return true;
            case Quaternion quaternion:
                serialized = Join(quaternion.x, quaternion.y, quaternion.z, quaternion.w);

                return true;
            case Color color:
                serialized = Join(color.r, color.g, color.b, color.a);

                return true;
            case string text:
                serialized = text;

                return true;
            case IFormattable formattable when !value.GetType().IsEnum:
                serialized = formattable.ToString(null, CultureInfo.InvariantCulture);

                return true;
        }

        Type valueType = Nullable.GetUnderlyingType(declaredType) ?? declaredType;

        if (valueType.IsEnum)
        {
            serialized = value.ToString();

            return true;
        }

        TypeConverter converter = TypeDescriptor.GetConverter(valueType);

        if (converter.CanConvertTo(typeof(string)))
        {
            serialized = converter.ConvertToInvariantString(value);

            return serialized is not null;
        }

        serialized = string.Empty;

        return false;
    }

    /// <summary>
    /// 文字列を値に戻します。
    /// </summary>
    public static bool TryDeserialize(string serialized, Type declaredType, out object value)
    {
        Type nullableType = Nullable.GetUnderlyingType(declaredType);
        Type valueType = nullableType ?? declaredType;

        value = null;

        if (nullableType is not null && string.IsNullOrEmpty(serialized)) return true;

        if (valueType == typeof(string))
        {
            value = serialized;

            return true;
        }

        if (valueType.IsEnum)
        {
            try
            {
                value = Enum.Parse(valueType, serialized, true);

                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        if (valueType == typeof(Vector2) && TrySplit(serialized, 2, out float[] vector2))
        {
            value = new Vector2(vector2[0], vector2[1]);

            return true;
        }

        if (valueType == typeof(Vector3) && TrySplit(serialized, 3, out float[] vector3))
        {
            value = new Vector3(vector3[0], vector3[1], vector3[2]);

            return true;
        }

        if (valueType == typeof(Vector4) && TrySplit(serialized, 4, out float[] vector4))
        {
            value = new Vector4(vector4[0], vector4[1], vector4[2], vector4[3]);

            return true;
        }

        if (valueType == typeof(Quaternion) && TrySplit(serialized, 4, out float[] quaternion))
        {
            value = new Quaternion(quaternion[0], quaternion[1], quaternion[2], quaternion[3]);

            return true;
        }

        if (valueType == typeof(Color) && TrySplit(serialized, 4, out float[] color))
        {
            value = new Color(color[0], color[1], color[2], color[3]);

            return true;
        }

        // 数値・bool などは TypeConverter 任せ。書式違いは例外で返ってくる。
        try
        {
            TypeConverter converter = TypeDescriptor.GetConverter(valueType);

            value = converter.CanConvertFrom(typeof(string))
                ? converter.ConvertFromInvariantString(serialized)
                : Convert.ChangeType(serialized, valueType, CultureInfo.InvariantCulture);

            return value is not null;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// 派生クラスで宣言された public な get/set プロパティを集めます。
    /// <see cref="ObjectPrefab"/> 自身が持つ Position などは対象外です。
    /// </summary>
    private static PropertyInfo[] GetProperties(Type prefabType)
    {
        if (PropertyCache.TryGetValue(prefabType, out PropertyInfo[] cached)) return cached;

        Dictionary<string, PropertyInfo> collected = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);

        for (Type type = prefabType; type is not null && type != typeof(ObjectPrefab); type = type.BaseType)
        {
            foreach (PropertyInfo property in type.GetProperties(
                         BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                if (property.GetGetMethod() is null ||
                    property.GetSetMethod() is null ||
                    property.GetIndexParameters().Length != 0 ||
                    collected.ContainsKey(property.Name))
                {
                    continue;
                }

                collected[property.Name] = property;
            }
        }

        PropertyInfo[] properties = collected.Values
            .OrderBy(property => property.Name, StringComparer.Ordinal)
            .ToArray();

        PropertyCache[prefabType] = properties;

        return properties;
    }

    private static string DescribeType(Type type)
        => type.IsEnum ? type.Name : type.Name.ToLowerInvariant();

    private static string Join(params float[] values)
        => string.Join(",", values.Select(value => value.ToString("R", CultureInfo.InvariantCulture)));

    private static bool TrySplit(string value, int count, out float[] components)
    {
        components = new float[count];

        string[] parts = value
            .Trim()
            .Trim('(', ')', '[', ']')
            .Replace(" ", string.Empty)
            .Split(',');

        if (parts.Length != count) return false;

        for (int i = 0; i < count; i++)
        {
            if (!float.TryParse(parts[i], NumberStyles.Float, CultureInfo.InvariantCulture, out components[i]))
                return false;
        }

        return true;
    }
}
