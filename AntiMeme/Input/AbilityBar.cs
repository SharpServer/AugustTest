using System.Collections.Generic;
using System.Linq;
using AntiMeme.Abilities;
using AntiMeme.Audio;
using AntiMeme.Items.Weapons;
using LabApi.Features.Wrappers;
using Sliced.API.Features;
using UnityEngine;

namespace AntiMeme.Input;

/// <summary>
/// いま選んでいるアビリティを覚えておきます。
/// 状態は <see cref="PlayerScope"/> に紐づくので、退出とラウンド再開で勝手に消えます。
/// </summary>
public static class AbilityBar
{
    private static readonly Dictionary<Player, int> Selection = new Dictionary<Player, int>();

    /// <summary>
    /// このプレイヤーがいま選んでいるアビリティです。持っていなければ null。
    /// </summary>
    public static AbilityBase Selected(Player player)
    {
        IReadOnlyList<AbilityBase> abilities = AbilityBase.Of(player);

        if (abilities.Count == 0) return null;

        return abilities[IndexOf(player) % abilities.Count];
    }

    /// <summary>
    /// 次のアビリティへ切り替えます。切り替わった先を返します。
    /// </summary>
    public static AbilityBase SelectNext(Player player)
    {
        IReadOnlyList<AbilityBase> abilities = AbilityBase.Of(player);

        if (abilities.Count == 0) return null;

        Set(player, IndexOf(player) + 1);

        return Selected(player);
    }

    private static int IndexOf(Player player) =>
        Selection.TryGetValue(player, out int index) ? index : 0;

    private static void Set(Player player, int index)
    {
        if (!Selection.ContainsKey(player))
            PlayerScope.Of(player).OnDispose(owner => Selection.Remove(owner));

        Selection[player] = index < 0 ? 0 : index;
    }
}
