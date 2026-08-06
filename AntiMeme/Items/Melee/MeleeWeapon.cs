using AntiMeme.Items.Bases;
using AntiMeme.Items.Weapons;
using InventorySystem.Items.Autosync;
using InventorySystem.Items.Jailbird;
using InventorySystem.Items.Scp1509;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using UnityEngine;

using GameJailbird = InventorySystem.Items.Jailbird.JailbirdItem;

namespace AntiMeme.Items.Melee;

/// <summary>
/// 近接武器の土台です。ジェイルバード (<see cref="GameJailbird"/>) と SCP-1509 は
/// どちらもゲーム側では <see cref="MeleeAutoSync"/> なので、ダメージの焼き付けは 1 か所で済みます。
///
/// <para>
/// 「当てたとき」は <see cref="CustomWeapon.OnHit"/> をそのまま使います。近接専用の命中フックは作りません。
/// 銃向けの <see cref="Bases.CustomWeapon"/> の宣言 (弾数・アタッチメント) は
/// 近接では単に無視されるので、継承しても害になりません。
/// </para>
/// <para>
/// 振る操作は <see cref="PlayerEvents.ProcessingJailbirdMessage"/> /
/// <see cref="PlayerEvents.ProcessingScp1509Message"/> を通ります。振り始めの 1 通目でだけ可否を決め、
/// 後続のメッセージには同じ結論を使い回します (毎回判定するとクールダウンが自分の振りを弾く)。
/// </para>
/// </summary>
public abstract class MeleeWeapon : CustomWeapon
{
    private static bool hooked;

    private float readyAt;
    private bool attackBlocked;

    protected MeleeWeapon() => Hook();

    /// <summary>
    /// 通常の一振りのダメージです。負値ならバニラのまま。
    /// </summary>
    protected virtual float MeleeDamage => -1f;

    /// <summary>
    /// 溜め攻撃のダメージです。負値ならバニラのまま。ジェイルバードにだけ効きます。
    /// </summary>
    protected virtual float ChargeDamage => -1f;

    /// <summary>
    /// 一振りごとのクールダウン (秒) です。0 ならバニラの連打間隔のまま。
    /// </summary>
    protected virtual float AttackCooldown => 0f;

    /// <summary>
    /// クールダウン中に振ろうとしたときの表示です。空なら何も出しません。
    /// </summary>
    protected virtual string CooldownHint => string.Empty;

    /// <summary>
    /// SCP-1509 の蘇生を許すか。
    /// </summary>
    protected virtual bool AllowResurrect => true;

    /// <summary>
    /// 振ろうとしたときに呼ばれます。<see langword="false"/> を返すと攻撃自体が起きません。
    /// 「振る操作を別の効果に置き換える」タイプの武器はここに書きます。
    /// </summary>
    protected virtual bool OnAttacking(Player player) => true;

    /// <inheritdoc/>
    protected override void Customize(Item item)
    {
        if (item.Base is not MeleeAutoSync melee) return;

        if (MeleeDamage >= 0f)
            melee.MeleeDamage = MeleeDamage;

        if (ChargeDamage >= 0f && melee is GameJailbird jailbird)
            jailbird._chargeDamage = ChargeDamage;
    }

    private bool Allow(Player player, bool triggering)
    {
        if (!triggering) return !attackBlocked;

        attackBlocked = true;

        if (Time.time < readyAt)
        {
            if (CooldownHint.Length > 0)
                player.SendHint(CooldownHint, 3f);

            return false;
        }

        if (!OnAttacking(player)) return false;

        readyAt = Time.time + AttackCooldown;
        attackBlocked = false;

        return true;
    }

    private static void Hook()
    {
        if (hooked) return;

        hooked = true;
        PlayerEvents.ProcessingJailbirdMessage += OnAnyJailbirdMessage;
        PlayerEvents.ProcessingScp1509Message += OnAnyScp1509Message;
        PlayerEvents.Scp1509Resurrecting += OnAnyResurrecting;
        AntiMeme.Items.ItemRuntime.Register(() =>
        {
            PlayerEvents.ProcessingJailbirdMessage -= OnAnyJailbirdMessage;
            PlayerEvents.ProcessingScp1509Message -= OnAnyScp1509Message;
            PlayerEvents.Scp1509Resurrecting -= OnAnyResurrecting;
            hooked = false;
        });
    }

    private static void OnAnyJailbirdMessage(PlayerProcessingJailbirdMessageEventArgs ev)
    {
        bool triggering = ev.Message == JailbirdMessageType.AttackTriggered;

        if (!triggering && ev.Message != JailbirdMessageType.AttackPerformed) return;

        if (Of(ev.JailbirdItem.Serial) is MeleeWeapon melee && !melee.Allow(ev.Player, triggering))
            ev.AllowAttack = false;
    }

    private static void OnAnyScp1509Message(PlayerProcessingScp1509MessageEventArgs ev)
    {
        bool triggering = ev.Message == Scp1509MessageType.AttackTriggered;

        if (!triggering && !IsAttackFollowUp(ev.Message)) return;

        if (Of(ev.Scp1509Item.Serial) is MeleeWeapon melee && !melee.Allow(ev.Player, triggering))
            ev.AllowAttack = false;
    }

    private static bool IsAttackFollowUp(Scp1509MessageType message) =>
        message is Scp1509MessageType.AttackPerformed
            or Scp1509MessageType.AttackPreformedHitWall
            or Scp1509MessageType.AttackPreformedHitFlesh
            or Scp1509MessageType.AttackPreformedHitMissed;

    private static void OnAnyResurrecting(PlayerScp1509ResurrectingEventArgs ev)
    {
        if (Of(ev.Item.Serial) is MeleeWeapon melee && !melee.AllowResurrect)
            ev.IsAllowed = false;
    }
}
