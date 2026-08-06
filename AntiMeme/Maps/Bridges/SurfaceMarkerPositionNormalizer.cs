using System;
using System.Linq;
using LabApi.Features.Wrappers;
using ProjectMER.Features.Objects;
using UnityEngine;

namespace AntiMeme.Maps.Bridges;

/// <summary>
/// ProjectMER が Surface を屋外として扱い絶対座標で読むため、旧 aaa.json から移行された
/// 4 つの室内相対位置だけを実行時に変換します。未知の点は推測で動かしません。
/// </summary>
internal static class SurfaceMarkerPositionNormalizer
{
    private static readonly Vector3[] LegacySurfacePositions =
    {
        new Vector3(-31.410f, -49.002f, -101.920f),
        new Vector3(10.815f, -72.231f, -7.428f),
        new Vector3(119.679f, -39.379f, -50.867f),
        new Vector3(110.169f, 6.004f, -61.166f),
    };

    public static bool TryNormalize(SchematicObjectPrefabObject marker, out Vector3 absolute)
    {
        absolute = marker?.transform.position ?? Vector3.zero;
        if (marker is null || !IsKnownLegacyPosition(absolute)) return false;

        Room surface = Room.List.FirstOrDefault(room => string.Equals(room.Name.ToString(), "Surface", StringComparison.OrdinalIgnoreCase));
        if (surface is null) return false;

        absolute = surface.Transform.TransformPoint(absolute);
        return true;
    }

    private static bool IsKnownLegacyPosition(Vector3 position)
        => LegacySurfacePositions.Any(candidate => Vector3.Distance(candidate, position) < .01f);
}
