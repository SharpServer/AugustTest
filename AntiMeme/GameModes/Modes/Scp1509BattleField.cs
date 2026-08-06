using System.Linq;
using AntiMeme.Roles;
using LabApi.Features.Enums;
using LabApi.Features.Wrappers;
using PlayerRoles;
using UnityEngine;

using Random = UnityEngine.Random;

namespace AntiMeme.GameModes.Modes;

/// <summary>
/// SCP-1509 BATTLEFIELD。SCP-1509 を持った 2 部隊が、
/// 閉じた区画に押し込められて殴り合います。
/// </summary>
/// <remarks>
/// 戦場は 4 つから 1 つ選ばれます。選ばれた戦場の境目にあたる扉だけを閉じ、
/// 残りは開けたまま全部施錠して、その区画から出られないようにします。
/// </remarks>
public sealed class Scp1509BattleField : RaidMode
{
    private const float Health = 300f;
    private const float SpawnLift = 2f;

    private static readonly Arena[] Arenas =
    [
        // 地上。閉じる扉は無く、全域が戦場になる。
        new Arena([], DoorName.SurfaceGate, DoorName.SurfaceGate),

        // エントランス。ゲートを閉じて区画に押し込める。
        new Arena([DoorName.EzGateA, DoorName.EzGateB], DoorName.EzGateA, DoorName.EzGateB),

        // 重度収容。チェックポイントで封をする。
        new Arena([DoorName.EzGateA, DoorName.EzGateB], DoorName.HczCheckpoint, DoorName.HczCheckpoint),

        // 軽度収容。SCP-914 の扉で封をする。
        new Arena([DoorName.Lcz914Gate], DoorName.LczCheckpointA, DoorName.LczCheckpointB),
    ];

    public override string Name => "SCP-1509 BATTLEFIELD";

    /// <inheritdoc/>
    public override bool AllowsBreachAnnouncement => false;

    public override string Description => "SCP-1509 を装備した二つの部隊が戦う。";

    public override int MinimumPlayers => 4;

    public override int Weight => 6;

    public override bool AllowsWarhead => false;

    protected override void OnStarted()
    {
        Arena arena = Arenas[Random.Range(0, Arenas.Length)];
        bool chaosFirst = Random.value < 0.5f;

        Delay(1.1f, () =>
        {
            Equip(chaosFirst);

            Say(
                "SCP 1 5 0 9 amnestic battle field simulation online",
                "SCP-1509記憶処理戦闘シミュレーションを開始します。");
        });

        // 役職が落ち着いてから戦場を閉じ、両軍を入り口へ置く。
        Delay(3.1f, () =>
        {
            Seal(arena);
            Deploy(arena);
        });
    }

    /// <summary>両軍を半々に分け、同じ装備を配ります。</summary>
    private static void Equip(bool chaosFirst)
    {
        int index = 0;

        foreach (Player player in Living.ToArray())
        {
            bool chaos = chaosFirst == (index++ % 2 == 0);

            player.SetRole(chaos ? RoleTypeId.ChaosRifleman : RoleTypeId.NtfPrivate);
            player.MaxHealth = Health;
            player.Health = Health;

            player.ClearInventory();
            player.AddItem(ItemType.SCP1509);
            player.AddItem(ItemType.Medkit);
            player.AddItem(ItemType.Medkit);
            player.AddItem(ItemType.Adrenaline);
            player.AddItem(ItemType.SCP500);
            player.AddItem(ItemType.SCP500);
            player.AddItem(ItemType.ArmorCombat);
        }
    }

    /// <summary>戦場の外へ出られないよう、境目を閉じて全部施錠します。</summary>
    private static void Seal(Arena arena)
    {
        foreach (Door door in Door.List)
        {
            if (door is not { IsDestroyed: false }) continue;

            door.IsOpened = !arena.Sealed.Contains(door.DoorName);
            door.IsLocked = true;
        }
    }

    /// <summary>両軍をそれぞれの入り口へ置きます。</summary>
    private static void Deploy(Arena arena)
    {
        Vector3 chaos = PositionOf(arena.Chaos, SpawnPoints.ChaosEntry);
        Vector3 foundation = PositionOf(arena.Foundation, SpawnPoints.NtfEntry);

        foreach (Player player in Living)
            player.Position = player.Role is RoleTypeId.ChaosRifleman ? chaos : foundation;
    }

    private static Vector3 PositionOf(DoorName name, Vector3 fallback) =>
        (Door.Get(name) is { } door ? door.Position : fallback) + Vector3.up * SpawnLift;

    /// <summary>戦場 1 つ分の定義です。境目の扉と、両軍の入り口を持ちます。</summary>
    private readonly struct Arena
    {
        public Arena(DoorName[] sealedDoors, DoorName chaos, DoorName foundation)
        {
            Sealed = sealedDoors;
            Chaos = chaos;
            Foundation = foundation;
        }

        /// <summary>閉じて封をする扉です。空なら全部開けます。</summary>
        public DoorName[] Sealed { get; }

        /// <summary>カオス側の入り口です。</summary>
        public DoorName Chaos { get; }

        /// <summary>財団側の入り口です。</summary>
        public DoorName Foundation { get; }
    }
}
