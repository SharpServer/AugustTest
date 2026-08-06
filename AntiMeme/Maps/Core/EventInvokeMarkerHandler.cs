using System;
using System.Collections.Generic;
using LabApi.Features.Wrappers;
using UnityEngine;

namespace AntiMeme.Maps.Core;

/// <summary>旧 MapToy からの入力を型付き通知へ変換する薄いハンドラです。</summary>
public sealed class EventInvokeMarkerHandler : Sliced.API.Features.EventHandlerBase
{
    public static event Action<string, Player> Invoked;
    public bool Invoke(string marker, Player player)
    {
        if (string.IsNullOrWhiteSpace(marker)) return false;
        Invoked?.Invoke(marker.Trim(), player);
        return true;
    }
}
