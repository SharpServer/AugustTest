using AntiMeme.Teams.Factions;
using CustomPlayerEffects;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Features.Wrappers;
using Sliced.API.Features;
using UnityEngine;

namespace AntiMeme.Items.Melee;

/// <summary>
/// 対反ミーム無力化グレネード。第五教会の者だけを無力化します。
/// それ以外には音と光しか届きません。
/// </summary>
/// <remarks>
/// 旧実装は「第五主義者かどうか」と「第五教会陣営かどうか」を別々の拡張メソッドで判定し、
/// 前者に当たって後者に外れた者だけ 5000 ダメージで即死させていました。
/// 陣営はチーム層が一意に答えるようになったので、判定は
/// <see cref="FifthistTeam"/> への所属ひとつに寄せ、SCP かどうかで威力を分けています。
/// </remarks>
public sealed class NeutralizeGrenade : ThrownGrenade
{
    private const float Radius = 8f;
    private const float HumanDamage = 25f;
    private const float ScpDamage = 5000f;
    private const float SinkholeDuration = 20f;

    /// <inheritdoc/>
    public override ItemType BaseType => ItemType.GrenadeHE;

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => new Color32(185, 75, 255, 255);

    /// <inheritdoc/>
    public override string Name => "対反ミーム無力化グレネード";

    /// <inheritdoc/>
    public override string Description => "反ミーム存在及びその影響を受けた者を一時的に無力化し、ダメージを与える。";

    /// <inheritdoc/>
    protected override float FuseTime => 0.5f;

    /// <inheritdoc/>
    protected override void OnExploding(ProjectileExplodingEventArgs ev)
    {
        Suppress(ev);

        foreach (Player target in CustomTeam.Get<FifthistTeam>().Members)
        {
            if ((target.Position - ev.Position).sqrMagnitude > Radius * Radius) continue;

            target.EnableEffect<Sinkhole>(55, SinkholeDuration);
            target.Damage(target.IsSCP ? ScpDamage : HumanDamage, ev.Player);

            ev.Player?.SendHitMarker();
        }
    }
}
