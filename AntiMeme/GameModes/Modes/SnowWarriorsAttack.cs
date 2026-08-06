using System;
using System.Collections.Generic;
using AntiMeme.Maps;
using AntiMeme.Maps.Objects;
using AntiMeme.Roles.Warriors;
using UnityEngine;

namespace AntiMeme.GameModes.Modes;

/// <summary>
/// 雪の戦士の襲撃。最後は巨大な雪玉が降ってきて施設ごと埋もれさせます。
/// </summary>
public sealed class SnowWarriorsAttack : WarriorRaid
{
    public override string Name => "Snow Warriors Raid";

    /// <inheritdoc/>
    public override bool AllowsGateLockdown => false;

    public override string Description => "雪の戦士が施設を凍結させる。";

    /// <inheritdoc/>
    protected override IReadOnlyList<Season> Seasons => [Season.Christmas];

    /// <inheritdoc/>
    protected override Type WarriorRole => typeof(SnowWarrior);

    /// <inheritdoc/>
    protected override ObjectPrefab CreateProjectile() => new SnowMegaball();

    /// <inheritdoc/>
    protected override string Color => "#ffffff";

    /// <inheritdoc/>
    protected override string Emperor => "雪の帝王";

    /// <inheritdoc/>
    protected override Color LightColor => UnityEngine.Color.white;

    /// <inheritdoc/>
    protected override Color DoomColor => new Color32(255, 255, 0, 255);

    /// <inheritdoc/>
    protected override string FinalOrder => "施設を埋もれさせよ";

    /// <inheritdoc/>
    protected override string AttackProtocol => "MEGABALL";

    /// <inheritdoc/>
    protected override string FarewellCassie => "PLEASE D .g4 IE .g6 .g3 .g4";

    /// <inheritdoc/>
    protected override string FarewellSubtitle => "PLEASE DIE";
}
