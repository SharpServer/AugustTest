using System.Collections.Generic;
using AntiMeme.Roles.Scps;
using LabApi.Events.Arguments.Scp914Events;
using LabApi.Features.Wrappers;
using Scp914;
using Sliced.API.Features;
using UnityEngine;

namespace AntiMeme.Items.Scp914;

/// <summary>
/// SCP-914 の変換規則を適用します。
/// <see cref="EventHandlerBase"/> を継承しているだけで Sliced が購読します。
///
/// <para>
/// 規則の出どころは 2 つだけです。
/// カスタムアイテムは <see cref="IScp914Upgradable"/> を実装して自分で持ち、
/// バニラアイテムは <see cref="VanillaRules"/> の表に書きます。
/// 旧実装にあった ID レジストリと巨大な switch は要りません。
/// </para>
/// </summary>
public class Scp914Handler : EventHandlerBase
{
    /// <summary>
    /// 人間を VeryFine に通したときにゾンビ化する確率です。
    /// </summary>
    private const float ZombifyChance = 0.25f;

    /// <summary>
    /// バニラアイテムの変換規則です。
    /// ここに無いアイテムはバニラの挙動のままです。
    /// 季節変更などで差し替えられるよう可変のままにしてあります。
    /// </summary>
    public static readonly Dictionary<ItemType, Scp914RuleSet> VanillaRules = VanillaScp914Rules.Build();

    public override void OnScp914ProcessingPickup(Scp914ProcessingPickupEventArgs ev)
    {
        Scp914Context context = new Scp914Context(ev.KnobSetting, ev.Pickup, ev.NewPosition);

        // 床のアイテムだけが当たり枠を引ける。インベントリに撒くと事故が多すぎる。
        if (TryApplyWildcards(context, includePickupWildcard: true) || TryApply(context, ev.Pickup.Type))
            ev.IsAllowed = false;
    }

    public override void OnScp914ProcessingInventoryItem(Scp914ProcessingInventoryItemEventArgs ev)
    {
        Scp914Context context = new Scp914Context(ev.KnobSetting, ev.Item, ev.Player);

        if (TryApplyWildcards(context, includePickupWildcard: false) || TryApply(context, ev.Item.Type))
            ev.IsAllowed = false;
    }

    /// <summary>
    /// VeryFine に人間を通すと、稀にゾンビ化します。
    /// 旧実装が手書きしていた見た目・効果・状態は <see cref="Zombified"/> が持ちます。
    /// </summary>
    public override void OnScp914ProcessingPlayer(Scp914ProcessingPlayerEventArgs ev)
    {
        if (ev.KnobSetting is not Scp914KnobSetting.VeryFine) return;
        if (ev.Player is not { IsDestroyed: false, IsAlive: true } player || player.IsSCP) return;
        if (Random.value >= ZombifyChance) return;

        ev.IsAllowed = false;

        CustomRole.Spawn<Zombified>(player);
        player.Scale = new Vector3(
            Random.Range(0.55f, 1.08f),
            Random.Range(0.55f, 1.08f),
            Random.Range(0.55f, 1.08f));
        player.SendHint("<size=24>体が魔改造されていく・・・！</size>", 5f);
    }

    /// <summary>
    /// 表より先に判定する共通ロールです。当たったら true を返して以降を止めます。
    /// </summary>
    private static bool TryApplyWildcards(Scp914Context context, bool includePickupWildcard)
    {
        if (Roll(VanillaScp914Rules.O5Wildcard, context)) return true;

        return includePickupWildcard && Roll(VanillaScp914Rules.PickupWildcard, context);
    }

    /// <summary>
    /// 確率付きの規則を 1 回だけ試します。外れたら false を返して次へ譲ります。
    /// </summary>
    private static bool Roll(Scp914Rule rule, Scp914Context context)
    {
        if (rule.Chance < 1f && Random.value >= rule.Chance) return false;

        rule.WithChance(1f).Execute(context);

        return true;
    }

    /// <summary>
    /// 規則を探して実行します。実行したらバニラの変換を止めるため true を返します。
    /// </summary>
    private static bool TryApply(Scp914Context context, ItemType type)
    {
        // カスタムアイテムが自分で規則を持っていればそれが最優先。
        if (CustomItem.Of(context.Serial) is IScp914Upgradable upgradable)
        {
            if (upgradable.Scp914Rules?.For(context.Setting) is not { } customRule) return false;

            customRule.Execute(context);

            return true;
        }

        if (!VanillaRules.TryGetValue(type, out Scp914RuleSet ruleSet)) return false;
        if (ruleSet.For(context.Setting) is not { } rule) return false;

        rule.Execute(context);

        return true;
    }
}
