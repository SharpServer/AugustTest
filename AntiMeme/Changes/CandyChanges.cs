using System.Linq;
using Hazards;
using AntiMeme.Roles.Scps;
using CustomPlayerEffects;
using InventorySystem.Items.Usables.Scp330;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using Sliced.API.Features;
using UnityEngine;
using Utils;

using Random = UnityEngine.Random;

namespace AntiMeme.Changes;

/// <summary>
/// SCP-330 の当たり方と、通常期にしか出ない特殊キャンディの効果です。
/// </summary>
/// <remarks>
/// 出る色そのものは <c>Patches.CandyPoolPatch</c> が年中 13 色に固定しています。
/// ここが決めるのは<b>どの色が当たりやすいか</b>と<b>食べたときに何が起きるか</b>です。
/// ハロウィン期はバニラの Haunted 挙動に任せるので、特殊効果は出しません。
/// </remarks>
public sealed class CandyChanges : EventHandlerBase
{
    private const float PinkChance = 0.25f;
    private const float RareChance = 0.35f;

    private static readonly CandyKindID[] Rare =
    [
        CandyKindID.Black,
        CandyKindID.Brown,
        CandyKindID.Gray,
        CandyKindID.Orange,
        CandyKindID.White,
        CandyKindID.Evil,
    ];

    private static readonly CandyKindID[] Normal =
    [
        CandyKindID.Red,
        CandyKindID.Blue,
        CandyKindID.Green,
        CandyKindID.Purple,
        CandyKindID.Rainbow,
        CandyKindID.Yellow,
    ];

    /// <inheritdoc/>
    public override void RegisterEvents()
    {
        PlayerEvents.InteractingScp330 += OnInteracting;
        PlayerEvents.InteractedScp330 += OnInteracted;
    }

    /// <inheritdoc/>
    public override void UnregisterEvents()
    {
        PlayerEvents.InteractingScp330 -= OnInteracting;
        PlayerEvents.InteractedScp330 -= OnInteracted;
    }

    /// <summary>
    /// 引く色を決め直します。ピンク 25% → レア 35% → それ以外、の順に判定します。
    /// </summary>
    private static void OnInteracting(PlayerInteractingScp330EventArgs ev)
    {
        float roll = Random.value;

        ev.CandyType = roll switch
        {
            <= PinkChance => CandyKindID.Pink,
            <= RareChance => Rare[Random.Range(0, Rare.Length)],
            _ => Normal[Random.Range(0, Normal.Length)],
        };
    }

    private static void OnInteracted(PlayerInteractedScp330EventArgs ev)
    {
        // ハロウィン期はバニラの Haunted 挙動に任せる。
        if (AntiMemePlugin.Settings.Season is Season.Halloween) return;

        Apply(ev.Player, ev.CandyType);
    }

    /// <summary>レア色ごとの効果です。どれも「匂い」の描写を添えます。</summary>
    private static void Apply(Player player, CandyKindID kind)
    {
        switch (kind)
        {
            case CandyKindID.Black:
                player.SendHint("<size=24>この世全ての混沌を混ぜて煮詰めた匂いがする...</size>", 5f);
                break;

            case CandyKindID.Brown:
                TantrumHazard.Spawn(player.Position, Quaternion.identity, Vector3.one);
                break;

            case CandyKindID.Gray:
                ApplyGray(player);
                break;

            case CandyKindID.Orange:
                ApplyOrange(player);
                break;

            case CandyKindID.White:
                player.EnableEffect<Ghostly>(1, 40f);
                player.EnableEffect<MovementBoost>(20, 40f);
                player.SendHint("<size=24>透き通るようなミルクの香りがする...</size>", 5f);
                break;

            case CandyKindID.Evil:
                CustomRole.Spawn<Zombified>(player);
                player.SendHint("<size=24>冒涜的な匂いに気が狂いそうになる...</size>", 5f);
                break;
        }
    }

    /// <summary>灰。重くなる代わりに 60 秒だけ固くなります。</summary>
    private static void ApplyGray(Player player)
    {
        player.EnableEffect<Slowness>(35, 60f);
        player.MaxHumeShield = 10000f;
        player.HumeShield = 10000f;
        player.SendHint("<size=24>埃っぽく鉄臭い匂いが鼻を刺す...</size>", 5f);

        PlayerScope.Of(player).Delay(60f, target =>
        {
            target.MaxHumeShield = 0f;
            target.HumeShield = 0f;
        });
    }

    /// <summary>橙。眩しすぎて周りが目を焼かれます。</summary>
    private static void ApplyOrange(Player player)
    {
        const float Radius = 3.25f;

        player.SendHint("<size=24>眩しいほどに爽やかなオレンジの匂いがする...</size>", 5f);

        foreach (Player other in Player.ReadyList.Where(other => !ReferenceEquals(other, player)))
        {
            if ((other.Position - player.Position).sqrMagnitude > Radius * Radius) continue;

            ExplosionUtils.ServerSpawnEffect(other.Position, ItemType.GrenadeFlash);
            other.EnableEffect<Flashed>(1, 2.5f);
        }
    }
}
