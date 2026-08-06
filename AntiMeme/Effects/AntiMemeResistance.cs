using AntiMeme.Roles.Fifthist;
using AntiMeme.Roles.Scps;
using CustomPlayerEffects;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using RemoteAdmin.Interfaces;
using Sliced.API.Features;

namespace AntiMeme.Effects;

/// <summary>
/// 反ミーム性の現象への耐性です。記憶補強剤で得ます。
///
/// <para>
/// これを持っているあいだ、第五教会系の精神干渉 (SCP-3005 と第五教会司祭の攻撃、
/// 第五の音) が本人には届きません。
/// </para>
/// </summary>
/// <remarks>
/// 旧実装はこれを <c>SpecificFlagType.AntiMemeEffectDisabled</c> というプレイヤーフラグで持ち、
/// 一時耐性の解除を <c>WaitAndRemove</c> のコルーチンで別管理していました。
/// 効果として持てば持続時間はゲーム側が数えてくれるので、解除の口を自分で用意する必要がありません。
/// </remarks>
public class AntiMemeResistance : CustomEffect, ICustomDisplayName
{
    /// <inheritdoc/>
    public bool CanBeDisplayed => true;

    /// <inheritdoc/>
    public string DisplayName => "Anti-Meme Resistance";

    /// <inheritdoc/>
    public override EffectClassification Classification => EffectClassification.Positive;

    /// <summary>
    /// このプレイヤーが反ミーム耐性を持っているか。
    /// </summary>
    public static bool Has(Player player) =>
        player is { IsDestroyed: false } && player.HasEffect<AntiMemeResistance>();

    /// <summary>
    /// 耐性を与えます。<paramref name="duration"/> が 0 なら永続です。
    /// </summary>
    public static void Give(Player player, float duration = 0f) =>
        player.EnableEffect<AntiMemeResistance>(1, duration);

    /// <inheritdoc/>
    protected override void OnEffectEnabled() =>
        Hook(
            () => PlayerEvents.Hurting += OnHurting,
            () => PlayerEvents.Hurting -= OnHurting);

    /// <summary>
    /// 反ミーム性の攻撃を無効化します。物理的な攻撃までは防ぎません。
    /// </summary>
    private void OnHurting(PlayerHurtingEventArgs ev)
    {
        if (!IsMine(ev.Player)) return;

        if (CustomRole.Of(ev.Attacker) is Scp3005 or FifthistPriest)
            ev.IsAllowed = false;
    }
}
