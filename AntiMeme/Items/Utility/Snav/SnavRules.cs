using System;
using System.Collections.Generic;
using System.Linq;
using AntiMeme.Items.Bases;
using AntiMeme.Items.Scp914;
using AntiMeme.Teams.Factions;
using InventorySystem.Items.Radio;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using MapGeneration;
using Sliced.API.Features;
using UnityEngine;

using LabRadioItem = LabApi.Features.Wrappers.RadioItem;

namespace AntiMeme.Items.Utility.Snav;

/// <summary>
/// S-Nav 3 種で共通の SCP-914 規則です。どれを入れても同じ梯子を上り下りします。
/// </summary>
internal static class SnavRules
{
    public static Scp914RuleSet Standard => new Scp914RuleSet
    {
        Rough = Scp914Rule.Destroy,
        Coarse = Scp914Rule.Destroy,
        OneToOne = Scp914Rule.To<SNAV300>(),
        Fine = Scp914Rule.To<SNAV310>(),
        VeryFine = Scp914Rule.To<SNAVUltimate>(),
    };
}
