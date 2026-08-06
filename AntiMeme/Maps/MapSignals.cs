using System;
using System.Collections.Generic;
using LabApi.Features.Wrappers;
using Sliced.API.Enums;
using Sliced.API.Features;
using UnityEngine;

namespace AntiMeme.Maps;

/// <summary>マップオブジェクトとアビリティ/役職層を疎結合に接続する通知です。</summary>
public static class MapSignals
{
    public static event Action<Player, string, Vector3> DocumentRead;
    public static event Action<Player, ObjectPrefab, bool> DoorStateChanged;
    public static event Action<Player, ObjectPrefab> TentacleInteracted;

    public static void RaiseDocumentRead(Player player, string documentType, Vector3 position)
        => DocumentRead?.Invoke(player, documentType ?? string.Empty, position);

    public static void RaiseDoorStateChanged(Player player, ObjectPrefab door, bool isOpen)
        => DoorStateChanged?.Invoke(player, door, isOpen);

    public static void RaiseTentacleInteracted(Player player, ObjectPrefab tentacle)
        => TentacleInteracted?.Invoke(player, tentacle);
}
