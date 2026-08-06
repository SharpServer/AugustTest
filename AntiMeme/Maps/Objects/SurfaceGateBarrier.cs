using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.Handlers;
using PlayerRoles;
using UnityEngine;

namespace AntiMeme.Maps.Objects;

/// <summary>
/// 地上ゲートの車止めバーです。上がっているあいだだけ車輌が通れます。
/// カオス・インサージェンシーの車輌が来ると自動で上がり、手元のボタンでも上げられます。
/// </summary>
public sealed class SurfaceGateBarrier : ObjectPrefab
{
    private const float Lift = 4f;
    private const float SlideSeconds = 0.8f;

    /// <summary>車輌が来てからバーを上げるまでの間です。</summary>
    private const float VehicleDelay = 2.25f;

    /// <summary>車輌の通過を待つ時間です。</summary>
    private const float VehicleHold = 22f;

    /// <summary>手動で上げたときに待つ時間です。</summary>
    private const float ManualHold = 3f;

    private Vector3 closed;
    private bool moving;

    /// <inheritdoc/>
    protected override string SchematicName => "Surface_CarStopper_Bar";

    /// <inheritdoc/>
    protected override void OnSetup()
    {
        closed = Position;

        if (GetInteractable("Button") is { } button)
            button.Interacted += _ => Raise(ManualHold);

        ServerEvents.WaveRespawned += OnWaveRespawned;
    }

    /// <inheritdoc/>
    protected override void OnDestroy() => ServerEvents.WaveRespawned -= OnWaveRespawned;

    /// <summary>カオス側のウェーブは車輌で来るので、バーを上げて通す。</summary>
    private void OnWaveRespawned(WaveRespawnedEventArgs ev)
    {
        if (ev.Wave.Faction != Faction.FoundationEnemy) return;

        Delay(VehicleDelay, () => Raise(VehicleHold));
    }

    /// <summary>
    /// バーを上げ、<paramref name="hold"/> 秒待ってから下ろします。
    /// 動作中の再要求は無視します (途中で戻ると車輌を挟む)。
    /// </summary>
    private void Raise(float hold)
    {
        if (moving) return;

        moving = true;

        MoveTo(closed, closed + Vector3.up * Lift, SlideSeconds, () =>
            Delay(hold, () => MoveTo(Position, closed, SlideSeconds, () => moving = false)));
    }
}
