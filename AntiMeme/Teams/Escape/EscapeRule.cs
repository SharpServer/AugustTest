using System;
using System.Collections.Generic;
using System.Linq;
using AntiMeme.Maps;
using AntiMeme.Teams.Factions;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using MEC;
using PlayerRoles;
using Sliced.API.Enums;
using Sliced.API.Features;
using UnityEngine;

using Logger = LabApi.Features.Console.Logger;

namespace AntiMeme.Teams.Escape;

/// <summary>
/// 1 本の脱出規則です。型そのものが規則の同一性で、文字列キーや登録 API はありません。
/// </summary>
public abstract class EscapeRule
{
    public virtual int Priority => 100;

    public abstract EscapeTarget Resolve(EscapeContext context);

    private static EscapeRule[] cached;

    /// <summary>
    /// 宣言されている規則を優先度順に返します。規則は状態を持たないので、
    /// 一度だけ生成して使い回します。脱出のたびに AppDomain を走査しません。
    /// </summary>
    private static EscapeRule[] All()
    {
        if (cached is not null) return cached;

        List<EscapeRule> rules = [];

        foreach (Type type in TypeParser.FindTypes<EscapeRule>())
        {
            try
            {
                rules.Add((EscapeRule)Activator.CreateInstance(type));
            }
            catch (Exception exception)
            {
                Logger.Error($"[AntiMeme] 脱出規則 {type.FullName} の生成に失敗しました: {exception}");
            }
        }

        return cached = rules.OrderBy(candidate => candidate.Priority).ToArray();
    }

    internal static EscapeTarget ResolveAll(Player player)
    {
        EscapeContext context = new EscapeContext(player);

        foreach (EscapeRule rule in All())
        {
            try
            {
                EscapeTarget target = rule.Resolve(context);

                if (!target.IsEmpty)
                    return target;
            }
            catch (Exception exception)
            {
                Logger.Error($"[AntiMeme] 脱出規則 {rule.GetType().FullName} の評価に失敗しました: {exception}");
            }
        }

        return EscapeTarget.None;
    }
}
