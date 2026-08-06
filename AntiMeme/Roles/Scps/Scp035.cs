using System.Collections.Generic;
using AntiMeme.Abilities;
using AntiMeme.Maps;
using CustomPlayerEffects;
using Exiled.API.Extensions;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Arguments.WarheadEvents;
using LabApi.Events.Handlers;
using MapGeneration;
using PlayerRoles;
using Sliced.API.Features;
using UnityEngine;

using ExiledPlayer = Exiled.API.Features.Player;
using AntiMeme.Roles;

namespace AntiMeme.Roles.Scps;

/// <summary>
/// SCP-035 の精神状態です。振る舞いを持たない閉じた値集合なので enum のままにします。
/// </summary>
public enum Scp035State
{
    /// <summary>安定。人間に友好的に振る舞える。</summary>
    Stable,

    /// <summary>不安定。腐蝕が進む。</summary>
    Unstable,

    /// <summary>発狂／覚醒。触手が使えるようになる。</summary>
    Awaken,

    /// <summary>完全覚醒。二度と戻らない。</summary>
    FullyAwaken,
}

/// <summary>
/// SCP-035。仮面に乗っ取られた博士で、精神状態が 3 分ごとに巡ります。
/// 弾頭が起動すると完全覚醒に固定され、以後は状態が戻りません。
///
/// 状態と残り時間は素のフィールドです。旧実装は同じものを
/// 「インスタンス辞書 + 全体辞書 + 凍結 ID の HashSet」の 3 重に持ち、
/// 完全覚醒だけ別コルーチンへ分岐して互いに上書きし合っていました。
/// </summary>
public class Scp035 : ScpRole
{
    private const float StateDuration = 180f;
    private const float Tick = 0.5f;
    private const float KillArtificialHealthReward = 35f;

    private Scp035State state = Scp035State.Stable;
    private float remaining = StateDuration;

    /// <inheritdoc/>
    protected internal override string CassieName => "SCP 0 3 5";

    public override string Name => "SCP-035";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#c50000>SCP-035</color>";

    /// <inheritdoc/>
    public override string Objective => "あなたは仮面に乗っ取られ、精神が不安定になっている。<color=red>核弾頭を起動しろ</color>";

    public override string Description =>
        "愚かな博士が仮面をつけて乗っ取れた！\n" +
        "但し、博士がなんとかしようと仮面に抵抗している為精神状態が不安定です。\n" +
        "あなたの最終的な目標は<color=red>施設の破壊</color>です。\n" +
        "精神が安定している時は比較的人間達に友好的に接し、そうでない時は\n" +
        "邪魔をさせないようにし、弾頭へと向かいましょう。\n" +
        "<color=yellow>※通常時は博士、発狂時はチュートリアルの見た目になります。</color>";

    protected override float HintDuration => 15f;

    public override RoleTypeId BaseRole => RoleTypeId.Tutorial;

    public override float? MaxHealth => 2500f;

    public override string CustomInfo => "<color=#C50000>SCP-035</color>";

    public override IReadOnlyList<ItemType> Items => [ItemType.KeycardScientist, ItemType.Painkillers];

    public override Vector3? SpawnPosition => SpawnPoints.InRoom(RoomName.Hcz939, Vector3.up * 0.65f);

    /// <summary>
    /// 現在の精神状態です。声のルーティングなど外側から参照されます。
    /// </summary>
    public Scp035State State => state;

    /// <summary>
    /// SCP と意思疎通できる状態かどうか。
    /// </summary>
    public bool CanSpeakWithScps => state is Scp035State.Awaken or Scp035State.FullyAwaken;

    protected override void OnSpawned()
    {
        Player.MaxArtificialHealth = 500f;
        Player.ArtificialHealth = 500f;

        Scope.Delay(1.5f, _ =>
        {
            if (CustomRole.Of(Player) is not Scp035) return;
            RoleWear wear = RoleWear.AttachSchematic(
                Player,
                "SCP035",
                Vector3.forward * 0.205f + Vector3.up * 0.6f,
                Vector3.one * 1.185f);
            if (wear != null) Scope.OnDispose(__ => wear.Dispose());
        });

        Enter(Scp035State.Stable);

        Scope.RunLoop(Tick, _ => Advance());

        Hook(
            () => PlayerEvents.Cuffing += OnCuffing,
            () => PlayerEvents.Cuffing -= OnCuffing);

        Hook(
            () => PlayerEvents.Death += OnDeath,
            () => PlayerEvents.Death -= OnDeath);

        Hook(
            () => WarheadEvents.Starting += OnWarheadStarting,
            () => WarheadEvents.Starting -= OnWarheadStarting);
    }

    /// <summary>
    /// 仮面は拘束できません。
    /// </summary>
    private void OnCuffing(PlayerCuffingEventArgs ev)
    {
        if (IsMine(ev.Target))
            ev.IsAllowed = false;
    }

    /// <summary>
    /// 殺すたびに仮面が力を取り戻します。
    /// </summary>
    private void OnDeath(PlayerDeathEventArgs ev)
    {
        if (IsMine(ev.Attacker))
            Player.ArtificialHealth += KillArtificialHealthReward;
    }

    /// <summary>
    /// 弾頭が動き出したら、もう博士のふりをする必要はありません。
    /// </summary>
    private void OnWarheadStarting(WarheadStartingEventArgs ev) => Enter(Scp035State.FullyAwaken);

    private void Advance()
    {
        if (state is Scp035State.FullyAwaken)
        {
            // 完全覚醒は不可逆。カウントダウンも巡回もしない。
            ShowStatus($"状態：{Localize(state)}\n変化まで：<color=red><b>抵抗不可能</b></color>", Tick + 0.2f);
            Uncuff();

            return;
        }

        remaining -= Tick;

        if (remaining <= 0f)
        {
            Enter(state switch
            {
                Scp035State.Stable => Scp035State.Unstable,
                Scp035State.Unstable => Scp035State.Awaken,
                _ => Scp035State.Stable,
            });
        }

        if (state is Scp035State.Awaken)
            Uncuff();

        ShowStatus($"状態：{Localize(state)}\n変化まで：{Mathf.CeilToInt(remaining)}", Tick + 0.2f);
    }

    private void Enter(Scp035State next)
    {
        state = next;
        remaining = StateDuration;

        switch (next)
        {
            case Scp035State.Stable:
                AbilityBase.Revoke<Scp035TentacleAbility>(Player);
                Player.DisableAllEffects();
                Appear(RoleTypeId.Scientist);
                Player.SendHint(
                    "<color=green>安定</color>状態へと移行しました！\n" +
                    "現在精神は比較的安定しており、人々に危害を与える必要は無いでしょう。\n" +
                    "<color=green>人々と友好的に接しましょう</color>",
                    6f);
                break;

            case Scp035State.Unstable:
                AbilityBase.Revoke<Scp035TentacleAbility>(Player);
                Player.EnableEffect<Poisoned>(10);
                Player.SendHint(
                    "<color=yellow>不安定</color>状態へと移行しました！\n" +
                    "腐蝕が再開しました。\n" +
                    "<color=yellow>人々に警告を与え、己の生存を心掛けましょう。</color>",
                    6f);
                break;

            case Scp035State.Awaken:
                AbilityBase.Give<Scp035TentacleAbility>(Player);
                Player.DisableEffect<Poisoned>();
                Player.EnableEffect<Invigorated>(20);
                Player.EnableEffect<BodyshotReduction>(30);
                Player.EnableEffect<DamageReduction>(30);
                Appear(RoleTypeId.Tutorial);
                Player.SendHint(
                    "<color=red>発狂／覚醒</color>状態へと移行しました！\n" +
                    "腐蝕が止まりました。\n" +
                    "<color=red>ためらう必要はない。出る事だけを考えるのだ。</color>",
                    6f);
                break;

            case Scp035State.FullyAwaken:
                AbilityBase.Give<Scp035TentacleAbility>(Player);
                Player.DisableEffect<Poisoned>();
                Player.EnableEffect<Invigorated>(30);
                Player.EnableEffect<BodyshotReduction>(40);
                Player.EnableEffect<DamageReduction>(40);
                Player.EnableEffect<MovementBoost>(5);
                Appear(RoleTypeId.Tutorial);
                Player.SendHint(
                    "<color=red><b>完全覚醒</b></color>状態へと移行しました！\n" +
                    "現在精神は完全に支配されており、もはや受け入れるしかないでしょう！",
                    6f);
                break;
        }
    }

    /// <summary>
    /// 覚醒中は拘束が解けます。
    /// </summary>
    private void Uncuff()
    {
        if (Player.IsDisarmed)
            Player.IsDisarmed = false;
    }

    /// <summary>
    /// 見た目だけ別のロールに差し替えます。LabApi 側に書き込み口が無いので EXILED の
    /// <c>MirrorExtensions.ChangeAppearance</c> を使います。
    /// </summary>
    private void Appear(RoleTypeId role)
    {
        if (ExiledPlayer.Get(Player.ReferenceHub) is { } exiled)
            exiled.ChangeAppearance(role);
    }

    private static string Localize(Scp035State value) => value switch
    {
        Scp035State.Stable => "<color=green>安定</color>",
        Scp035State.Unstable => "<color=yellow>不安定</color>",
        Scp035State.Awaken => "<color=red>発狂／覚醒</color>",
        _ => "<color=red><b>完全覚醒</b></color>",
    };
}
