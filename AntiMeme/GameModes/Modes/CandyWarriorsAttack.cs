using System;
using System.Collections.Generic;
using AntiMeme.Maps;
using AntiMeme.Maps.Objects;
using AntiMeme.Roles.Warriors;
using UnityEngine;

namespace AntiMeme.GameModes.Modes;

/// <summary>
/// お菓子の戦士の襲撃。季節によって春 (エイプリル) と秋 (ハロウィン) で
/// 立つ戦士と色が変わります。
/// </summary>
public sealed class CandyWarriorsAttack : WarriorRaid
{
    public override string Name => "Candy Warriors Raid";

    /// <inheritdoc/>
    public override bool AllowsGateLockdown => false;

    public override string Description => "お菓子の戦士が施設を占拠する。";

    /// <inheritdoc/>
    protected override IReadOnlyList<Season> Seasons => [Season.April, Season.Halloween];

    /// <inheritdoc/>
    protected override Type WarriorRole =>
        IsApril ? typeof(CandyWarriorApril) : typeof(CandyWarriorHalloween);

    /// <inheritdoc/>
    protected override ObjectPrefab CreateProjectile() => new CandyNuke();

    /// <inheritdoc/>
    protected override string Color => IsApril ? "#ff8cd9" : "#ff9633";

    /// <inheritdoc/>
    protected override string Emperor => "お菓子の帝王";

    /// <inheritdoc/>
    protected override Color LightColor => UnityEngine.Color.white;

    /// <inheritdoc/>
    protected override Color DoomColor => new Color32(255, 79, 173, 255);

    /// <inheritdoc/>
    protected override string FinalOrder => "施設を爆破させよ";

    /// <inheritdoc/>
    protected override string AttackProtocol => "PINK CANDY";

    /// <inheritdoc/>
    protected override string FarewellCassie => "PLEASE B .g4 O .g6 .g3 .g4";

    /// <inheritdoc/>
    protected override string FarewellSubtitle => "PLEASE BOOM";

    private static bool IsApril => AntiMemePlugin.Settings.Season is Season.April;
}
