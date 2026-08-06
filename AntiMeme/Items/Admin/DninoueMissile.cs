using AntiMeme.Items.Bases;
using AntiMeme.Maps;
using CustomPlayerEffects;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Features.Wrappers;
using UnityEngine;

namespace AntiMeme.Items.Admin;

/// <summary>
/// 回復するスキマティックを射出する管理者用カードです。投げ捨て操作で発射します。
/// </summary>
public sealed class DninoueMissile : CustomKeycard
{
    public override string Name => "にゃあ";

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => Color.magenta;

    public override string Description => "ああああああああああああああああああああああああああああ";

    protected override string Label => "ADMIN ULTIMATE TOOL";

    protected override string HolderName => "UwU. 55555";

    protected override string SerialNumber => "555555555555";

    protected override Color32 Tint => new Color32(255, 0, 250, 255);

    protected override Color32 LabelColor => new Color32(255, 0, 250, 255);

    protected override int Rank => 1;

    /// <inheritdoc/>
    protected override void OnDropping(PlayerDroppingItemEventArgs ev)
    {
        if (!ev.Throw) return;

        ev.IsAllowed = false;
        Destroy();

        new HealingMissile
        {
            Position = ev.Player.Position,
            Direction = ev.Player.Camera.forward,
            Lifespan = 15.5f,
        }.Create();
    }
}

/// <summary>
/// 短時間前進しながら周囲へ回復効果を配るスキマティックです。
/// <see cref="DninoueMissile"/> 専用なので同居させています。
/// </summary>
internal sealed class HealingMissile : ObjectPrefab
{
    private const float Speed = 1.55555f / 15.5f;
    private const float Interval = 0.02f;
    private const float Radius = 2.5f;

    public Vector3 Direction { get; set; }

    protected override string SchematicName => "YoungDevLikesUltimatePicture";

    public override bool FollowsMarker => false;

    protected override void OnCreate() => Loop(Interval, Tick);

    private void Tick()
    {
        Position += Direction.normalized * (Speed * Interval);

        foreach (Player player in Player.ReadyList)
        {
            if (!player.IsAlive || (player.Position - Position).sqrMagnitude > Radius * Radius) continue;

            player.Heal(10f);
            player.EnableEffect<Invigorated>(255, 5f);
            player.EnableEffect<Ghostly>(255, 5f);
            player.EnableEffect<DamageReduction>(255, 5f);
        }
    }
}
