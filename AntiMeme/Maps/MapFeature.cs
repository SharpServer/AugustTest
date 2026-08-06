using System;
using System.Collections.Generic;
using LabApi.Features.Wrappers;
using Sliced.API.Enums;
using Sliced.API.Features;
using UnityEngine;

namespace AntiMeme.Maps;

/// <summary>
/// ラウンド中にマップへ追加する機能の共通ライフサイクルです。
/// ProjectMER のオブジェクトとは別に、イベント購読や一時状態を持つ機能はこの層へ置きます。
/// </summary>
public abstract class MapFeature : EventHandlerBase
{
    public override HandlerLifetime Lifetime => HandlerLifetime.Manual;

    /// <summary>ラウンド再生成時に、派生機能が状態を初期化する入口です。</summary>
    public override void OnServerRoundRestarted() => Reset();

    /// <summary>状態をラウンド境界へ戻します。</summary>
    protected virtual void Reset()
    {
    }
}
