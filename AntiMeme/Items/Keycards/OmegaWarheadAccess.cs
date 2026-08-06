using Sliced.API.Features;
using UnityEngine;

namespace AntiMeme.Items.Keycards;

/// <summary>
/// OMEGA WARHEAD サイロにアクセスするための使い捨てパスです。
/// 消費するのはサイロ側 (マップ層) なので、このアイテム自身は何もしません。
/// </summary>
public sealed class OmegaWarheadAccess : CustomItem
{
    /// <inheritdoc/>
    public override ItemType BaseType => ItemType.SurfaceAccessPass;

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => Color.blue;

    /// <inheritdoc/>
    public override string Name => "<color=blue>OMEGA WARHEAD</color>アクセスパス";

    /// <inheritdoc/>
    public override string Description =>
        "<color=blue>OMEGA WARHEAD</color>サイロにアクセスできる使い捨てのカード。\n" +
        "担当職員へ：間違ってもゲート解放に使わないように！";
}
