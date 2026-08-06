using AntiMeme.Teams;
using System;
using System.Collections.Generic;
using System.Linq;
using AntiMeme.Teams.Factions;
using CustomPlayerEffects;
using LabApi.Events.Arguments.Scp079Events;
using LabApi.Features.Enums;
using LabApi.Features.Wrappers;
using PlayerRoles;
using RemoteAdmin.Interfaces;
using PlayerRoles.PlayableScps.Scp079;
using PlayerRoles.PlayableScps.Scp049;
using PlayerRoles.PlayableScps.Scp106;
using PlayerRoles.PlayableScps.Scp096;
using Respawning;
using Respawning.Waves;
using Respawning.Waves.Generic;
using Sliced.API.Features;
using UnityEngine;
using AntiMeme.Roles.FoundationForces;
using AntiMeme.Roles.Scps;
using AntiMeme.Abilities;
using AntiMeme.Input;
using AntiMeme.GameModes;
using AntiMeme.GameModes.Modes;
using AntiMeme.Spawning;

using ExiledRound = Exiled.API.Features.Round;
using HintParameter = Hints.HintParameter;
using Respawn = Exiled.API.Features.Respawn;
using SpawnableFaction = Exiled.API.Enums.SpawnableFaction;
using SSKeybindHintParameter = Hints.SSKeybindHintParameter;

namespace AntiMeme.Hud;

/// <summary>
/// 役職 / 陣営 / モードの現在値を HSM スロットへ写す表示層です。
///
/// 旧実装の役職 ID 辞書とチャンネル登録表をここへ持ち込まず、
/// <see cref="CustomRole"/> と <see cref="CustomTeam"/> の型をそのまま表示の根拠にします。
/// プレイヤーごとの状態は保持せず、各スロットの更新時に現在値を読み取ります。
/// </summary>
internal static class HudRenderer
{
    private static bool initialized;

    public static void Initialize()
    {
        if (initialized) return;

        initialized = true;

        HudSlot.Role.Bind(BuildRole);
        HudSlot.Team.Bind(BuildTeam);
        HudSlot.Objective.Bind(BuildObjective);
        HudSlot.Event.Bind(BuildEvent);
        HudSlot.Specific.Bind(BuildSpecific);
        HudSlot.Ability.Bind(BuildAbilities);
        HudSlot.Effects.Bind(BuildEffects);
        HudSlot.Roster.Bind(BuildRoster);
        HudSlot.Respawn.Bind((viewer, _) => BuildRespawn(viewer));
        HudSlot.Debug.Bind((viewer, _) => BuildDebug(viewer));

        // BossBar の公開 API はそのまま維持し、静的初期化だけを確実に行う。
        BossBar.EnsureInitialized();
    }

    public static void ShowScp079Ping(Scp079PingedEventArgs ev)
    {
        if (ev?.Player is not { IsDestroyed: false } source) return;

        Room room = Room.GetRoomAtPosition(ev.Position);
        string location = room is null
            ? "不明な場所"
            : $"{room.Zone} / {room.Name}";

        string target = ev.PingType switch
        {
            Scp079PingType.Generator => "発電機",
            Scp079PingType.Projectile => "投射物",
            Scp079PingType.MicroHid => "マイクロ HID 所持者",
            Scp079PingType.Human => "人間",
            Scp079PingType.Elevator => "エレベーター",
            Scp079PingType.Door => "ドア",
            _ => "地点",
        };

        string color = ev.PingType is Scp079PingType.Generator
            or Scp079PingType.Projectile
            or Scp079PingType.MicroHid
            ? "red"
            : ev.PingType == Scp079PingType.Human ? "yellow" : "white";

        string message =
            $"<color={color}><size=80%>SCP-079 が {target} にピンを差した。場所: {location}</size></color>";

        foreach (Player recipient in Player.ReadyList)
        {
            if (!IsValid(recipient) || !IsPingRecipient(recipient)) continue;

            // HSM の一時ヒントに任せるため、受信者ごとの TTL / バージョン辞書は不要。
            recipient.SendHint(message, 5f);
        }
    }

    /// <summary>
    /// 役職欄です。整形は役職自身 (<see cref="CustomRole.HudLabel"/>) が持ちます。
    /// カスタム役職でないバニラ役職だけ、ここで陣営色を巻きます。
    /// </summary>
    private static string BuildRole(Player subject)
    {
        if (!IsValid(subject)) return string.Empty;

        if (CustomRole.Of(subject) is { } custom)
            return $"Role: {custom.HudLabel}";

        string color = GetTeam(subject)?.Color ?? "#ffffff";

        return $"Role: <color={color}>{GetVanillaRoleName(subject.Role)}</color>";
    }

    private static string BuildTeam(Player subject)
    {
        if (!IsValid(subject)) return string.Empty;

        // FacilityTermination 中は勝敗が人類 / 正常性の 2 択になるので、
        // 陣営表示も勝利判定 (GameModeRoundHandler) と同じ線で切り替える。
        if (GameMode.Current is FacilityTermination)
        {
            return GameModeRoundHandler.IsFacilityNormalcy(subject)
                ? "Team: <color=red>正常性</color>"
                : GameModeRoundHandler.IsFacilityHumanity(subject)
                    ? "Team: <color=#0000c8>人類</color>"
                    : "Team: <color=#bbbbbb>中立</color>";
        }

        // 2 つの陣営にまたがる役職は自分で名乗る。それ以外は陣営の表記。
        if (CustomRole.Of(subject)?.TeamLabel is { Length: > 0 } label)
            return $"Team: {label}";

        CustomTeam team = GetTeam(subject);

        return team is null
            ? "Team: <color=#bbbbbb>中立</color>"
            : $"Team: {team.HudName}";
    }

    private static string BuildObjective(Player subject)
    {
        if (!IsValid(subject)) return string.Empty;

        CustomRole custom = CustomRole.Of(subject);
        CustomTeam team = GetTeam(subject);

        if (GameMode.Current is FacilityTermination)
        {
            if (GameModeRoundHandler.IsFacilityNormalcy(subject))
                return "Objective: 財団に従い、人類を根絶させよ。";

            if (GameModeRoundHandler.IsFacilityHumanity(subject))
            {
                return team is ScientistsTeam or ClassDTeam
                    ? "Objective: 施設の方針変更に巻き込まれた。生き延び、正常性陣営から逃れろ。"
                    : "Objective: 人類第一に、正常性陣営に抵抗せよ。";
            }
        }

        // 目的は役職が第一、次に陣営の既定。どちらも無ければ説明の要約。
        if (custom?.Objective is { Length: > 0 } roleObjective)
            return $"Objective: {roleObjective}";

        if (team is { Objective.Length: > 0 } faction)
            return $"Objective: {faction.Objective}";

        string description = ShortDescription(custom?.Description);

        return string.IsNullOrEmpty(description)
            ? "Objective: 生き残れ。"
            : $"Objective: {description}";
    }

    /// <summary>
    /// 進行中 (または予約されている) モードの表示名です。
    /// 旧実装は何も無いラウンドでも「無し」と出していたので、枠ごと消しません。
    /// </summary>
    private static string BuildEvent(Player subject)
    {
        if (!IsValid(subject)) return string.Empty;

        return $"[Event]\n<size=28>{GameModeSelection.DisplayName}</size>";
    }

    private static string BuildSpecific(Player subject)
    {
        if (!IsValid(subject)) return string.Empty;

        List<string> lines = [];
        CustomRole custom = CustomRole.Of(subject);

        // ネームプレート (CustomInfo) ではなく、役職が名乗る計器を出す。
        if (!string.IsNullOrEmpty(custom?.Status))
            lines.Add(custom.Status);

        if (TryGetScpStatus(subject, out string scpStatus))
            lines.Add(scpStatus);

        return string.Join("\n", lines);
    }

    /// <summary>
    /// アビリティ欄です。旧実装と同じく<b>いま選んでいる 1 つ</b>を主役にし、
    /// 残りは 1 行の一覧に畳みます。全部を等しく並べると、
    /// <see cref="AbilityBar"/> で切り替えた結果が画面に出ません。
    /// </summary>
    private static HudContent BuildAbilities(Player subject)
    {
        if (!IsValid(subject) || !subject.IsAlive) return string.Empty;

        IReadOnlyList<AbilityBase> abilities = AbilityBase.Of(subject);
        if (abilities.Count == 0) return string.Empty;

        AbilityBase active = AbilityBar.Selected(subject) ?? abilities[0];
        int index = 0;

        for (int i = 0; i < abilities.Count; i++)
        {
            if (!ReferenceEquals(abilities[i], active)) continue;

            index = i;
            break;
        }

        string name = active.DisplayName;
        if (active is ChoiceAbility choice && choice.Selected is { } option)
            name += $" <color=#8fdcff>[{option.Name}]</color>";

        string uses = active.MaxUses < 0 ? "∞" : active.RemainingUses.ToString();

        string summary = string.Join(
            " | ",
            abilities.Select((ability, slot) =>
            {
                string marker = slot == index
                    ? $"<color=#ffcc00>*{slot + 1}</color>"
                    : (slot + 1).ToString();

                return $"{marker}:{Shorten(ability.DisplayName, 8)} {CompactState(ability)}";
            }));

        // 割り当てキーはプレイヤーごとに違うので、文字はクライアントに解決させる。
        List<HintParameter> parameters = [];
        List<string> controls = [];

        if (Keybind(parameters, InputHandler.SettingIdOf<UseAbilityKey>()) is { } use)
            controls.Add($"<color=#aaffaa>{use}</color>:使用");

        if (abilities.Count > 1 && Keybind(parameters, InputHandler.SettingIdOf<SwitchAbilityKey>()) is { } switchKey)
            controls.Add($"<color=#aaffaa>{switchKey}</color>:切替");
        else if (abilities.Count <= 1)
            controls.Add("所持:1");

        if (active is ChoiceAbility { Choices.Count: > 1 } &&
            Keybind(parameters, InputHandler.SettingIdOf<PreviousAbilityOptionKey>()) is { } previous &&
            Keybind(parameters, InputHandler.SettingIdOf<NextAbilityOptionKey>()) is { } next)
        {
            controls.Add($"<color=#aaffaa>{previous}</color>/<color=#aaffaa>{next}</color>:オプション");
        }

        string control = string.Join(" / ", controls);

        return new HudContent(
            $"<size=22><color=#ffcc00>Ability {index + 1}/{abilities.Count}</color> " +
            $"{name} {State(active)} Uses:{uses}</size>\n" +
            $"<size=18>{control} | {summary}</size>",
            parameters.Count == 0 ? null : parameters.ToArray());
    }

    /// <summary>
    /// 差し込み欄を 1 つ確保し、その位置指定 (<c>{n}</c>) を返します。
    /// キーがまだ配られていなければ null を返し、その項目は出しません。
    /// </summary>
    private static string Keybind(List<HintParameter> parameters, int settingId)
    {
        if (settingId < 0) return null;

        parameters.Add(new SSKeybindHintParameter(settingId));

        return $"{{{parameters.Count - 1}}}";
    }

    private static string State(AbilityBase ability)
    {
        if (ability.MaxUses >= 0 && ability.RemainingUses <= 0)
            return "<color=#ff6666>DONE</color>";

        return ability.RemainingCooldown > 0f
            ? $"<color=#ffd966>CD {Mathf.CeilToInt(ability.RemainingCooldown)}s</color>"
            : "<color=#38ff6b>READY</color>";
    }

    private static string CompactState(AbilityBase ability)
    {
        if (ability.MaxUses >= 0 && ability.RemainingUses <= 0)
            return "<color=#ff6666>0</color>";

        return ability.RemainingCooldown > 0f
            ? $"<color=#ffd966>{Mathf.CeilToInt(ability.RemainingCooldown)}s</color>"
            : "<color=#38ff6b>OK</color>";
    }

    private static string Shorten(string name, int maxLength) =>
        string.IsNullOrEmpty(name) || name.Length <= maxLength
            ? name
            : name.Substring(0, Mathf.Max(1, maxLength - 3)) + "...";

    /// <summary>一度に出す効果の上限です。これを超えたぶんは件数だけ出します。</summary>
    private const int MaxShownEffects = 8;

    /// <summary>
    /// 掛かっている効果を縦に積みます。観戦中は観戦先のものが出ます。
    /// </summary>
    /// <remarks>
    /// 並びは<b>急ぎで見たい順</b>: 有害 → 良し悪し両方 → 有益 の順に、
    /// 同じ種類なら残り時間が短いものを上へ (下端基準なので画面下側が直近)。
    /// <c>Technical</c> は収容内部の配管なので、
    /// <c>ICustomDisplayName</c> で自分から名乗ったものだけ出します。
    /// </remarks>
    private static string BuildEffects(Player subject)
    {
        if (!IsValid(subject)) return string.Empty;

        // 優先度の高い順に並べてから上限で切る。
        StatusEffectBase[] sorted = subject.ActiveEffects
            .Where(effect => effect is { Intensity: > 0 } && IsWorthShowing(effect))
            .OrderBy(SortKey)
            .ThenBy(effect => effect.Duration <= 0f ? float.MaxValue : effect.TimeLeft)
            .ToArray();

        if (sorted.Length == 0) return string.Empty;

        StatusEffectBase[] kept = sorted.Take(MaxShownEffects).ToArray();

        // 下端が基準なので、最優先を最後の行に置くと画面の隅に貼り付いて動かない。
        List<string> lines = Enumerable.Reverse(kept).Select(FormatEffect).ToList();

        if (sorted.Length > kept.Length)
            lines.Insert(0, $"<color=#9aa0a6><size=85%>他 {sorted.Length - kept.Length} 件</size></color>");

        return string.Join("\n", lines);
    }

    /// <summary>収容内部の技術的な効果は、自分から名乗ったものだけ出します。</summary>
    private static bool IsWorthShowing(StatusEffectBase effect) =>
        effect.Classification != StatusEffectBase.EffectClassification.Technical ||
        effect is ICustomDisplayName { CanBeDisplayed: true };

    private static int SortKey(StatusEffectBase effect) => effect.Classification switch
    {
        StatusEffectBase.EffectClassification.Negative => 0,
        StatusEffectBase.EffectClassification.Mixed => 1,
        StatusEffectBase.EffectClassification.Positive => 2,
        _ => 3,
    };

    /// <summary>
    /// 味方一覧です。<b>出すのは <see cref="CustomTeam.ShowsRoster"/> を名乗った陣営だけ</b>で、
    /// 旧実装の SCP / 第五教会 / 戦士達の 3 チャンネルに対応します。
    /// 財団部隊や D クラスに味方の HP を見せると、旧仕様より情報が増えてしまいます。
    /// </summary>
    private static string BuildRoster(Player viewer, Player subject)
    {
        if (!IsValid(viewer) || !IsValid(subject)) return string.Empty;

        if (GetTeam(subject) is not { ShowsRoster: true } team) return string.Empty;

        Player[] members = Player.ReadyList
            .Where(player => IsValid(player) && player.IsPlayer && player.IsAlive && ReferenceEquals(GetTeam(player), team))
            .OrderBy(player => player.PlayerId)
            .ToArray();

        if (members.Length == 0) return string.Empty;

        List<string> lines =
        [
            $"<b><color={team.Color}>{team.Name}</color></b>",
        ];

        foreach (Player member in members)
            lines.Add($"<color={team.Color}>{MemberName(member)}</color> {MemberVitals(member)}");

        string footer = team.RosterFooter(viewer);

        if (!string.IsNullOrEmpty(footer))
            lines.Add(footer);

        return string.Join("\n", lines);
    }

    /// <summary>一覧に出す名前です。役職名を出します (旧実装と同じ)。</summary>
    private static string MemberName(Player member) =>
        CustomRole.Of(member)?.Name ?? GetVanillaRoleName(member.Role);

    /// <summary>
    /// SCP-079 は HP を持たないのでエネルギーとレベル、それ以外は HP と Hume Shield。
    /// </summary>
    private static string MemberVitals(Player member)
    {
        if (member.RoleBase is Scp079Role scp079 &&
            scp079.TryGetOwner(out _) &&
            scp079.SubroutineModule.TryGetSubroutine(out Scp079AuxManager aux) &&
            scp079.SubroutineModule.TryGetSubroutine(out Scp079TierManager tiers))
        {
            float ratio = aux.MaxAux > 0f ? aux.CurrentAux / aux.MaxAux : 0f;

            return $"[ENERGY: <color={Gradient(ratio)}>{aux.CurrentAux:F0}</color>/{aux.MaxAux:F0}]" +
                   $" (LEVEL: {tiers.AccessTierLevel})";
        }

        string health =
            $"[<color={Gradient(member.MaxHealth > 0f ? member.Health / member.MaxHealth : 0f)}>{member.Health:F0}</color>" +
            $"/{member.MaxHealth:F0} HP] ";

        if (member.MaxHumeShield <= 0f) return health;

        float shieldRatio = member.HumeShield / member.MaxHumeShield;

        return health +
               $"(<color={Gradient(shieldRatio)}>{member.HumeShield:F0}</color>/{member.MaxHumeShield:F0} HS)";
    }

    /// <summary>0 で青、1 で緑。旧 <c>StaticUtils.ToGradientColor</c> と同じ配色です。</summary>
    private static string Gradient(float value)
    {
        value = Mathf.Clamp01(value);

        int green = Mathf.RoundToInt(255f * value);
        int blue = Mathf.RoundToInt(255f * (1f - value));

        return $"#00{green:X2}{blue:X2}";
    }

    private static string BuildRespawn(Player viewer)
    {
        if (!IsValid(viewer) || viewer.Role is not (RoleTypeId.Spectator or RoleTypeId.Overwatch))
            return string.Empty;

        TimeBasedWave foundation = SelectWave(wave => wave is NtfSpawnWave or NtfMiniWave);
        TimeBasedWave chaos = SelectWave(wave => wave is ChaosSpawnWave or ChaosMiniWave);

        TimeSpan foundationTime = GetTimeLeft(foundation);
        TimeSpan chaosTime = GetTimeLeft(chaos);

        // 「どちらが先か」は同着を別扱いにする。両方に ► が付くのが均衡状態の合図。
        bool balanced = foundationTime > TimeSpan.Zero && chaosTime > TimeSpan.Zero && foundationTime == chaosTime;
        bool foundationNext = foundationTime > TimeSpan.Zero &&
                              (foundationTime < chaosTime || chaosTime == TimeSpan.Zero);
        bool chaosNext = chaosTime > TimeSpan.Zero &&
                         (chaosTime < foundationTime || foundationTime == TimeSpan.Zero);

        string timer =
            $"{FormatWave(foundation, foundationTime, FoundationColor, foundationNext || balanced)}" +
            $"<space=450>" +
            $"{FormatWave(chaos, chaosTime, ChaosColor, chaosNext || balanced)}";

        return $"<align=center>{timer}\n{BuildRespawnState(balanced, foundationNext, chaosNext)}</align>";
    }

    private const string FoundationColor = "#00b7eb";
    private const string ChaosColor = "#228b22";
    private const string BalanceColor = "#FFD700";

    /// <summary>
    /// デバッグ HUD です。<c>am debug</c> で本人が入れたときだけ出ます。
    /// マップ作成に必要な値 (ワールド / ルームローカル座標・回転) を最優先で並べます。
    /// </summary>
    private static string BuildDebug(Player viewer)
    {
        if (!IsValid(viewer) || !DebugMode.IsOn(viewer)) return string.Empty;

        List<string> lines = ["<size=18><color=#ffff00>[DEBUG MODE]</color>"];

        CustomRole custom = CustomRole.Of(viewer);
        lines.Add(
            $"<color=#aaaaaa>Role:</color> {GetVanillaRoleName(viewer.Role)}  " +
            $"<color=#aaaaaa>CRole:</color> {custom?.GetType().Name ?? "None"}  " +
            $"<color=#aaaaaa>CTeam:</color> {GetTeam(viewer)?.Name ?? "None"}");

        Vector3 position = viewer.Position;
        Room room = viewer.Room;
        lines.Add(
            $"<color=#aaaaaa>World:</color> ({position.x:F2}, {position.y:F2}, {position.z:F2})  " +
            $"<color=#aaaaaa>Room:</color> {(room is null ? "None" : room.Name.ToString())}  " +
            $"<color=#aaaaaa>Zone:</color> {(room is null ? "None" : room.Zone.ToString())}");

        if (room is not null)
        {
            Quaternion inverse = Quaternion.Inverse(room.Rotation);
            Vector3 local = inverse * (position - room.Position);
            Vector3 roomEuler = room.Rotation.eulerAngles;

            lines.Add(
                $"<color=#aaaaaa>Local:</color> ({local.x:F2}, {local.y:F2}, {local.z:F2})  " +
                $"<color=#aaaaaa>RoomRot:</color> ({roomEuler.x:F1}, {roomEuler.y:F1}, {roomEuler.z:F1})");
        }

        lines.Add(DebugMode.TryGetDoor(viewer, out DebugMode.DoorSnapshot door)
            ? $"<color=#aaaaaa>Door:</color> {door.Name} @ {door.Room}  " +
              $"<color=#aaaaaa>Local:</color> ({door.LocalPosition.x:F2}, {door.LocalPosition.y:F2}, {door.LocalPosition.z:F2})  " +
              $"<color=#aaaaaa>Rot:</color> ({door.LocalEuler.x:F1}, {door.LocalEuler.y:F1}, {door.LocalEuler.z:F1})  " +
              $"<color=#aaaaaa>RoomRot:</color> ({door.RoomEuler.x:F1}, {door.RoomEuler.y:F1}, {door.RoomEuler.z:F1})"
            : "<color=#666666>Door: -- (ドアに触れると更新)</color>");

        lines.Add(
            $"<color=#aaaaaa>Round:</color> InProgress={Flag(Round.IsRoundInProgress)} " +
            $"Ended={Flag(Round.IsRoundEnded)} Lobby={Flag(ExiledRound.IsLobby)} Locked={Flag(ExiledRound.IsLocked)}  " +
            $"<color=#aaaaaa>Elapsed:</color> {Round.Duration:mm\\:ss}  " +
            $"<color=#aaaaaa>Players:</color> {Player.ReadyList.Count(player => player.IsPlayer)}");

        lines.Add(Warhead.IsDetonationInProgress
            ? $"<color=#ff4444>Warhead:</color> T-{Warhead.DetonationTime:F1}s  Locked={Flag(Warhead.IsLocked)}"
            : "<color=#666666>Warhead: Not active</color>");

        lines.Add(BuildDebugItem(viewer));

        lines.Add(
            $"<color=#aaaaaa>Mode:</color> {(GameMode.Current is { } mode ? mode.GetType().Name : "None")}  " +
            $"<color=#aaaaaa>Context:</color> {SpawnContext.Active.Name}  " +
            $"<color=#aaaaaa>FirstWave:</color> " +
            (FirstWaveScheduler.HasSpawned
                ? "spawned"
                : $"{FirstWaveScheduler.ElapsedSeconds:F0}/{FirstWaveScheduler.WaitSeconds:F0}s"));

        lines.Add("</size>");

        return string.Join("\n", lines);
    }

    private static string BuildDebugItem(Player viewer)
    {
        if (viewer.CurrentItem is not { } item)
            return "<color=#666666>Item: -- (未装備)</color>";

        string line =
            $"<color=#aaaaaa>Item:</color> {item.Type}  " +
            $"<color=#aaaaaa>Serial:</color> {item.Serial}  " +
            $"<color=#aaaaaa>CItem:</color> {(CustomItem.Of(item) is { } custom ? custom.GetType().Name : "None")}";

        if (item is FirearmItem firearm)
        {
            line += $"\n  <color=#ffaa44>[Firearm]</color> " +
                    $"<color=#aaaaaa>Ammo:</color> {firearm.StoredAmmo}/{firearm.MaxAmmo}  " +
                    $"<color=#aaaaaa>Chamber:</color> {firearm.ChamberedAmmo}/{firearm.ChamberMax}  " +
                    $"<color=#aaaaaa>Firerate:</color> {firearm.Firerate:F0}  " +
                    $"<color=#aaaaaa>Reloading:</color> {Flag(firearm.IsReloading)}";
        }

        return line;
    }

    private static string Flag(bool value) =>
        value ? "<color=green>T</color>" : "<color=red>F</color>";

    /// <summary>
    /// 「名前 強度 残り時間」の 1 行です。色が効果の種類を表します。
    /// </summary>
    private static string FormatEffect(StatusEffectBase effect)
    {
        string name = effect is ICustomDisplayName { CanBeDisplayed: true } named && !string.IsNullOrEmpty(named.DisplayName)
            ? named.DisplayName
            : effect.GetType().Name;

        // 強度は段階のあるものだけ。ON/OFF だけの効果に "Lv1" を出しても情報が増えない。
        string level = effect.Intensity > 1 ? $"Lv{effect.Intensity} " : string.Empty;

        // Duration 0 は永続。TimeLeft を出しても 0 のままで誤解を招く。
        string time = effect.Duration <= 0f ? "∞" : $"{Mathf.CeilToInt(effect.TimeLeft)}s";

        return $"<color={EffectColor(effect)}>{name}</color> <size=85%><color=#bbbbbb>{level}{time}</color></size>";
    }

    /// <summary>効果の種類ごとの色です。アビリティ欄と同じ配色に揃えてあります。</summary>
    private static string EffectColor(StatusEffectBase effect) => effect.Classification switch
    {
        StatusEffectBase.EffectClassification.Negative => "#ff6666",
        StatusEffectBase.EffectClassification.Mixed => "#ffd966",
        StatusEffectBase.EffectClassification.Positive => "#38ff6b",
        _ => "#9aa0a6",
    };

    /// <summary>
    /// SCP ごとの固有ゲージです。数字を見ないと立ち回れないものだけを出します。
    /// 役職の切り替え中はサブルーチンが揃っていないので、取れなければ黙って諦めます。
    /// </summary>
    /// <remarks>
    /// <c>PlayerEvents.OnSpawned</c> は <c>RoleSpawnpointManager.SetPosition</c> の内側、
    /// つまり <c>PlayerRoleManager.InitializeNewRole</c> が <c>roleBase.SetupPoolObject()</c> を
    /// 呼ぶ<b>前</b>に飛びます。その時点の役職はまだプール状態で
    /// <c>PlayerRoleBase.TryGetOwner</c> が false を返すため、
    /// <c>Scp106VigorAbilityBase.Vigor</c> のような所有者前提のアクセサは
    /// <c>InvalidOperationException</c> を投げます。ここで一度だけ弾きます。
    /// </remarks>
    private static bool TryGetScpStatus(Player player, out string text)
    {
        text = string.Empty;

        if (player.RoleBase is not { } roleBase || !roleBase.TryGetOwner(out _))
            return false;

        text = roleBase switch
        {
            Scp079Role scp079 => Scp079Status(scp079),
            Scp096Role scp096 => Scp096Status(scp096),
            Scp106Role scp106 => Scp106Status(scp106),
            Scp049Role scp049 => Scp049Status(scp049),
            _ => string.Empty,
        };

        return text.Length > 0;
    }

    private static string Scp079Status(Scp079Role role) =>
        role.SubroutineModule.TryGetSubroutine(out Scp079TierManager tiers) &&
        role.SubroutineModule.TryGetSubroutine(out Scp079AuxManager aux)
            ? $"Tier {tiers.AccessTierLevel} · AUX {aux.CurrentAuxFloored}/{Mathf.CeilToInt(aux.MaxAux)}"
            : string.Empty;

    private static string Scp096Status(Scp096Role role)
    {
        if (!role.SubroutineModule.TryGetSubroutine(out Scp096RageManager rage)) return string.Empty;

        if (rage.IsEnraged)
            return $"<color=red>激昂 {rage.EnragedTimeLeft:F0}s</color>";

        return role.SubroutineModule.TryGetSubroutine(out Scp096TargetsTracker tracker)
            ? $"標的 {tracker.Targets.Count}"
            : string.Empty;
    }

    private static string Scp106Status(Scp106Role role) =>
        role.SubroutineModule.TryGetSubroutine(out Scp106VigorAbilityBase vigor)
            ? $"Vigor {Mathf.RoundToInt(vigor.VigorAmount * 100f)}%"
            : string.Empty;

    private static string Scp049Status(Scp049Role role)
    {
        if (!role.SubroutineModule.TryGetSubroutine(out Scp049SenseAbility sense)) return string.Empty;

        if (sense.HasTarget && Player.Get(sense.Target) is { } target)
            return $"感知中: {target.Nickname}";

        float remaining = (float)sense.Cooldown.Remaining;

        return remaining > 0f ? $"感知 CD {remaining:F0}s" : "感知 待機";
    }

    private static bool IsPingRecipient(Player player)
    {
        CustomRole custom = CustomRole.Of(player);
        return ReferenceEquals(GetTeam(player), CustomTeam.Get<ScpTeam>()) ||
               custom is AraOrun;
    }

    private static CustomTeam GetTeam(Player player)
    {
        if (!IsValid(player)) return null;

        if (CustomRole.Of(player)?.Team is { } customTeam)
            return customTeam;

        return player.Role switch
        {
            RoleTypeId.ClassD => CustomTeam.Get<ClassDTeam>(),
            RoleTypeId.Scientist => CustomTeam.Get<ScientistsTeam>(),
            RoleTypeId.FacilityGuard => CustomTeam.Get<GuardsTeam>(),
            RoleTypeId.NtfSpecialist or RoleTypeId.NtfSergeant or RoleTypeId.NtfCaptain or RoleTypeId.NtfPrivate =>
                CustomTeam.Get<FoundationForcesTeam>(),
            RoleTypeId.ChaosConscript or RoleTypeId.ChaosRifleman or RoleTypeId.ChaosMarauder or RoleTypeId.ChaosRepressor =>
                CustomTeam.Get<ChaosInsurgencyTeam>(),
            RoleTypeId.Scp173 or RoleTypeId.Scp106 or RoleTypeId.Scp049 or RoleTypeId.Scp079 or
                RoleTypeId.Scp096 or RoleTypeId.Scp0492 or RoleTypeId.Scp939 or RoleTypeId.Scp3114 =>
                CustomTeam.Get<ScpTeam>(),
            _ => null,
        };
    }

    /// <summary>
    /// バニラ役職の表示名です。<b>ゲーム本体が持っている名前をそのまま使います。</b>
    /// </summary>
    /// <remarks>
    /// 自前で訳表を持つと「カオス・ライフルマン」のようにゲーム内の表記と食い違います。
    /// 旧実装も <c>player.Role.Name</c> (= <c>RoleTranslations.GetRoleName</c>) を出していました。
    /// </remarks>
    private static string GetVanillaRoleName(RoleTypeId role) => RoleTranslations.GetRoleName(role);

    private static string ShortDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description)) return string.Empty;

        string[] lines = description.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        string text = lines.Length == 0 ? description.Trim() : lines[lines.Length - 1].Trim();

        return text.Length > 120 ? text.Substring(0, 117) + "..." : text;
    }

    private static TimeBasedWave SelectWave(Func<SpawnableWaveBase, bool> belongsToFaction)
    {
        TimeBasedWave selected = null;
        TimeBasedWave ready = null;
        TimeBasedWave moving = null;
        TimeBasedWave paused = null;
        float shortestMoving = float.MaxValue;
        float shortestPaused = float.MaxValue;

        foreach (SpawnableWaveBase candidate in WaveManager.Waves)
        {
            if (!belongsToFaction(candidate) || candidate is not TimeBasedWave wave)
                continue;

            if (WaveManager.State != WaveQueueState.Idle && ReferenceEquals(WaveManager._nextWave, wave))
                selected = wave;

            if (!wave.Configuration.IsEnabled || wave.Timer.IsForcefullyPaused || wave.Timer.IsOutOfRespawns)
                continue;

            if (ready is null && wave.IsReadyToSpawn && !wave.Timer.IsPaused)
            {
                ready = wave;
                continue;
            }

            bool advancing = !wave.Timer.IsForcefullyPaused &&
                             (!wave.Timer.IsPaused || wave.Timer.TimeLeft > WaveTimer.CountdownPauseThresholdSeconds) &&
                             RoundSummary.RoundInProgress();

            if (advancing && wave.Timer.TimeLeft < shortestMoving)
            {
                shortestMoving = wave.Timer.TimeLeft;
                moving = wave;
            }
            else if (!advancing && wave.Timer.TimeLeft < shortestPaused)
            {
                shortestPaused = wave.Timer.TimeLeft;
                paused = wave;
            }
        }

        return selected ?? ready ?? moving ?? paused;
    }

    private static TimeSpan GetTimeLeft(TimeBasedWave wave) =>
        wave is null ? TimeSpan.Zero : TimeSpan.FromSeconds(wave.Timer.TimeLeft);

    private static string FormatWave(TimeBasedWave wave, TimeSpan time, string color, bool highlight)
    {
        string value;

        if (wave is null || wave.Timer.IsForcefullyPaused)
            value = "一時停止中";
        else if (time == TimeSpan.Zero)
            value = "利用不可";
        else if (time < TimeSpan.Zero)
            value = "まもなく到着";
        else
            value = $"{(int)time.TotalMinutes}分{time.Seconds}秒";

        // 色を付けるのは「次に来る側」だけ。常時色を付けるとどちらが先か読めない。
        return highlight ? $"<color={color}>► {value}</color>" : value;
    }

    private static string BuildRespawnState(bool balanced, bool foundationNext, bool chaosNext)
    {
        string faction = Respawn.NextKnownSpawnableFaction is SpawnableFaction.ChaosWave or SpawnableFaction.ChaosMiniWave
            ? $"<color={ChaosColor}>要注意団体</color>"
            : $"<color={FoundationColor}>財団部隊</color>";

        return WaveManager.State switch
        {
            WaveQueueState.Idle when balanced => $"現在<color={BalanceColor}>均衡中</color>",
            WaveQueueState.Idle when chaosNext => $"現在<color={ChaosColor}>要注意団体</color>が優勢",
            WaveQueueState.Idle when foundationNext => $"現在<color={FoundationColor}>財団部隊</color>が優勢",
            WaveQueueState.WaveSelected => $"{faction}陣営が準備開始",
            WaveQueueState.WaveSpawning => $"{faction}がまもなく到着",
            WaveQueueState.WaveSpawned => $"{faction}が到着",
            _ => "スポーン不可",
        };
    }

    private static bool IsValid(Player player) =>
        player is { IsDestroyed: false, IsReady: true, ReferenceHub: not null };
}
