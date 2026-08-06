using System.Collections.Generic;
using System.Linq;
using AntiMeme.Hud;
using Interactables.Interobjects.DoorUtils;
using InventorySystem.Items.ThrowableProjectiles;
using LabApi.Features.Wrappers;
using MEC;
using Sliced.API.Features;
using UnityEngine;

using Random = UnityEngine.Random;

namespace AntiMeme.Maps.Features;

/// <summary>
/// 地上への爆撃要請。地上を端から端まで薙ぐ絨毯爆撃を 3 波撃ち込みます。
/// </summary>
public sealed class SurfaceBombingFunction : FacilityControlRoomFunction
{
    private const float StartupDelay = 6.5f;
    private const int WaveCount = 3;
    private const float WaveDuration = 1.5f;
    private const int BombsPerWave = 155;
    private const float ScatterRadius = 10f;
    private const double FuseSeconds = 1.25;
    private const float DownwardVelocity = 18f;
    private const float SurfaceHeight = 290f;
    private const string AlertKey = "SurfaceBombing";

    private static readonly Vector3 StartPoint = new Vector3(138f, 299f, -41f);
    private static readonly Vector3 EndPoint = new Vector3(-20f, 305f, -41f);

    private static CoroutineHandle bombing;

    /// <inheritdoc/>
    public override string DisplayName => "爆撃要請";

    /// <inheritdoc/>
    public override string Description => "地上制圧のため、防衛部隊へ爆撃を要請する。";

    /// <inheritdoc/>
    public override int Order => 1;

    /// <inheritdoc/>
    public override DoorPermissionFlags RequiredPermissions =>
        DoorPermissionFlags.ArmoryLevelThree | DoorPermissionFlags.ExitGates;

    /// <inheritdoc/>
    public override float Cooldown => 300f;

    /// <inheritdoc/>
    public override void ResetState()
    {
        if (bombing.IsRunning)
            Timing.KillCoroutines(bombing);

        MapAudio.Stop(AlertKey);
    }

    /// <inheritdoc/>
    public override FacilityControlRoomFunctionResult Execute(FacilityControlRoomFunctionContext context)
    {
        if (bombing.IsRunning)
            return Failure("<color=#ff5555>既に爆撃が進行中です。</color>");

        Start();

        return Success("爆撃要請を送信しました。これより地上への爆撃を開始します。");
    }

    /// <summary>
    /// 爆撃を開始します。制御室を経由しない開始 (RA コマンドなど) もここを通ります。
    /// </summary>
    public static bool TryStart()
    {
        if (Round.IsRoundEnded || !Round.IsRoundStarted || bombing.IsRunning) return false;

        Start();

        return true;
    }

    private static void Start()
    {
        foreach (Player player in Player.ReadyList.Where(player => player.Position.y >= SurfaceHeight))
        {
            player.SendBroadcast(
                "[防衛部隊から管制室へ]地上爆撃を承認しました。これより攻撃を開始します・・・",
                8);
        }

        FacilityAnnouncer.Say(
            "Defense Forces to Control , Operation Accepted . Starting Surface Attack .",
            "[防衛部隊から管制室へ]地上爆撃を承認しました。これより攻撃を開始します。");

        MapAudio.Loop("sbialert.ogg", AlertKey, StartPoint, maxDistance: 250f);

        // 爆撃は要請した本人が居なくなっても撃ち切る。ラウンドが終われば止まる。
        bombing = Timing.RunCoroutine(Run());
    }

    private static IEnumerator<float> Run()
    {
        yield return Timing.WaitForSeconds(StartupDelay);

        for (int wave = 0; wave < WaveCount && CanContinue(); wave++)
        {
            for (int i = 0; i < BombsPerWave && CanContinue(); i++)
            {
                float progress = BombsPerWave <= 1 ? 1f : i / (BombsPerWave - 1f);

                Vector3 position = Vector3.Lerp(StartPoint, EndPoint, progress) + new Vector3(
                    Random.Range(-ScatterRadius, ScatterRadius),
                    0f,
                    Random.Range(-ScatterRadius, ScatterRadius));

                Drop(position);

                yield return Timing.WaitForSeconds(WaveDuration / BombsPerWave);
            }
        }

        MapAudio.Stop(AlertKey);
    }

    private static bool CanContinue() => Round.IsRoundStarted && !Round.IsRoundEnded;

    private static void Drop(Vector3 position)
    {
        if (TimedGrenadeProjectile.SpawnActive(position, ItemType.GrenadeHE, null, FuseSeconds)
            is not { } grenade)
            return;

        // 落下させないと空中で炸裂して地表に届かない。
        if (grenade.Rigidbody is { } body)
            body.linearVelocity = Vector3.down * DownwardVelocity;
    }
}
