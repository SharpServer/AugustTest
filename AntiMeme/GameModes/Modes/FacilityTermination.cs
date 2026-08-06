using System.Linq;
using AntiMeme.Maps;
using AntiMeme.Roles.FoundationForces;
using AntiMeme.Spawning;
using AntiMeme.Spawning.Waves;
using CustomPlayerEffects;
using CustomRendering;
using LabApi.Features.Enums;
using LabApi.Features.Wrappers;
using MapGeneration;
using PlayerRoles;
using Sliced.API.Features;
using UnityEngine;

using ExiledRoom = Exiled.API.Features.Room;
using Respawn = Exiled.API.Features.Respawn;
using SpawnableFaction = Exiled.API.Enums.SpawnableFaction;

namespace AntiMeme.GameModes.Modes;

/// <summary>
/// FACILITY TERMINATION。O5 評議会が人類の根絶へ方針転換し、
/// 施設を区画ごとに封鎖・除染したうえで DELTA WARHEAD で終了させます。
/// </summary>
/// <remarks>
/// 勝敗は「人類陣営 (Humanity)」と「正常性陣営 (Normalcy)」の 2 択になります。
/// その判定は <see cref="GameModeRoundHandler"/> が持ちます。
/// </remarks>
public sealed class FacilityTermination : RaidMode
{
    /// <summary>本編の猶予です。この間に決着を付けます。</summary>
    private const float MainPhase = 1200f;

    private const float DeltaCountdown = 130f;

    private static readonly Color OpenedColor = Color.green;

    public override string Name => "FACILITY TERMINATION";

    /// <inheritdoc/>
    public override bool AllowsBreachAnnouncement => false;

    /// <inheritdoc/>
    public override bool AllowsGateLockdown => false;

    public override string Description => "施設を段階的に除染し、DELTA WARHEAD で終了させる。";

    public override int MinimumPlayers => 8;

    public override int Weight => 4;

    public override bool AllowsWarhead => false;

    protected override void OnStarted()
    {
        SpawnContext.Activate<FacilityTerminationContext>();

        // SCP は全員「彫刻」として正常性側に付く。
        foreach (Player player in Living.Where(player => player.IsSCP).ToList())
            CustomRole.Spawn<Sculpture>(player);

        Delay(2f, AnnounceSides);

        Delay(27f, () =>
        {
            Say(
                "Attention . All Personnel . The Last Operation Forces Arrived to the Facility .",
                "全職員に通達。<color=#4DFFB8>最終作戦部隊</color>が施設に到着しました。");

            SpawnSystem.ReplaceNextSpawn(SpawnContext.Wave<GoCWave>(), source: nameof(FacilityTermination));

            // 差し替えを予約しただけではバニラの波が回ってこないので、旧実装と同じく
            // 要注意団体側のトークン / 影響力 / タイマーを押し込んで実際に湧かせる。
            Respawn.SetTokens(SpawnableFaction.ChaosWave, 2);
            Respawn.SetInfluence(Faction.FoundationEnemy, 250f);
            Respawn.AdvanceTimer(SpawnableFaction.ChaosWave, 30f);
        });

        Delay(27f + MainPhase, BeginEvacuation);
    }

    /// <inheritdoc/>
    protected override void OnStopped()
    {
        SpawnContext.Reset();
        base.OnStopped();
    }

    /// <summary>どちら側に付いたかを本人へ伝えます。</summary>
    private static void AnnounceSides()
    {
        foreach (Player player in Player.ReadyList)
        {
            bool humanity = CustomRole.Of(player) is not Sculpture && !player.IsSCP && !IsGuard(player);

            player.SendBroadcast(
                humanity
                    ? "<size=28>あなたは<color=blue>人類陣営</color>です。\n" +
                      "警備員と彫刻以外が一応仲間です。狂気を生き延びて奴らを終了してください！</size>"
                    : "<size=28>あなたは<color=red>正常性陣営</color>です。\n" +
                      "警備員と彫刻が仲間です。他の奴らを全員終了してください！</size>",
                8);
        }

        Say(
            "Attention, All personnel. Were recieved message from O5 Command. " +
            "Please Red this and Terminate Human it Your Self.",
            "全職員に通達。O5評議会からメッセージを受信した為、お知らせします。" +
            "これをよく読み、自身の人間性を<color=green>破壊</color>してください。<split>" +
            "以下はO5評議会の総意によって作成されたメッセージです。<split>" +
            "現時点で私たちの存在を知らない方々へ: 私たちはSCP財団という組織を代表しています。" +
            "私たちのかつての使命は、異常な事物、実体、その他様々な現象の収容と研究を中心に展開されていました。" +
            "この使命は過去100年以上にわたって私たちの組織の焦点でした。<split>" +
            "やむを得ない事情により、この方針は変更されました。私たちの新たな使命は人類の根絶です。<split>" +
            "今後の意思疎通は行われません。");
    }

    private static bool IsGuard(Player player) => player.Role is PlayerRoles.RoleTypeId.FacilityGuard;

    /// <summary>避難フェーズです。ここから区画が順に落ちていきます。</summary>
    private void BeginEvacuation()
    {
        Say(
            "Attention, All personnel. Were decided Decontamination of the Facility. " +
            "Please Evacuate to the Surface for Delta Protocol.",
            "全職員に通達。施設全体の<color=yellow>終了</color>が決定された為、" +
            "これより軽度収容区画～エントランス区画の<color=red>ロックダウン</color>及び" +
            "<color=green>除染プロセス</color>を開始します。" +
            "全職員は地上に避難し、<color=green><b>DELTAプロトコル</b></color>を待機してください。",
            noise: false);

        Delay(12.5f, () =>
        {
            OpenAllZones();
            MapAudio.Play("newdelta.ogg", "DeltaWarhead", Vector3.zero, maxDistance: 999f);
        });

        Delay(22.5f, () => Say(
            "Light Containment Zone Lockdown and Decontamination in T minus 80 Seconds.",
            "軽度収容区画のロックダウン及び除染まで残り: 80秒",
            noise: false));

        Delay(97.5f, () =>
        {
            Decontaminate(FacilityZone.LightContainment);
            Say(
                "Light Containment Zone is now Lockdowned and Started Decontamination Process.",
                "軽度収容区画がロックダウンされ、除染が開始されました。",
                noise: false);

            Say(
                "Heavy Containment Zone Lockdown and Decontamination in T minus 40 Seconds.",
                "重度収容区画のロックダウン及び除染まで残り: 40秒",
                noise: false);
        });

        Delay(132.5f, () =>
        {
            Decontaminate(FacilityZone.HeavyContainment);
            Say(
                "Heavy Containment Zone is now Lockdowned and Started Decontamination Process.",
                "重度収容区画がロックダウンされ、除染が開始されました。",
                noise: false);

            Say(
                "Entrance Zone Lockdown and Decontamination in T minus 20 Seconds.",
                "エントランス区画のロックダウン及び除染まで残り: 20秒",
                noise: false);
        });

        Delay(147.5f, () =>
        {
            Decontaminate(FacilityZone.Entrance);
            Say(
                "Entrance Zone is now Lockdowned and Started Decontamination Process.",
                "エントランス区画がロックダウンされ、除染が開始されました。",
                noise: false);

            Say(
                "Attention, All personnel. Delta Protocol is started in Surface and Detonate in T minus 100 seconds. " +
                "Please Effect by Delta Protocol Warhead. See you human.",
                "全職員に通達。<color=green><b>DELTAプロトコル</b></color>が地上にて開始されました。<split>" +
                "100秒後に爆破される、<b><color=green>DELTA PROTOCOL</color> <color=red>\"WARHEAD\"</color></b>の" +
                "影響を受け、人間性を<color=yellow>終了</color>してください。");
        });

        Delay(147.5f + DeltaCountdown, () => ExplodeLiving("DELTA WARHEADに爆破された"));
    }

    /// <summary>全区画の扉とエレベーターを開け放ちます。避難のための最後の猶予です。</summary>
    private static void OpenAllZones()
    {
        OpenDoors();

        foreach (ExiledRoom room in ExiledRoom.List)
        {
            room.AreLightsOff = false;
            room.Color = OpenedColor;
        }
    }

    /// <summary>区画を封鎖し、中に居る者へ除染を掛けます。</summary>
    private static void Decontaminate(FacilityZone zone)
    {
        foreach (Room room in Room.List.Where(room => room.Zone == zone))
        {
            foreach (Door door in room.Doors)
            {
                door.IsOpened = false;
                door.IsLocked = true;
            }
        }

        foreach (Player player in Living.Where(player => player.Room?.Zone == zone))
        {
            player.EnableEffect<Decontaminating>(1);
            player.EnableEffect<FogControl>(255);

            if (player.ReferenceHub.playerEffectsController.TryGetEffect(out FogControl fog))
                fog.SetFogType(FogType.Decontamination);
        }
    }
}
