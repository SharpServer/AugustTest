using AntiMeme.Items.Bases;
using System.Collections.Generic;
using AntiMeme.Items.Scp914;
using CustomPlayerEffects;
using LabApi.Features.Wrappers;
using PlayerRoles;
using Sliced.API.Features;
using AntiMeme.Roles.Fifthist;
using UnityEngine;

namespace AntiMeme.Items.Utility;

/// <summary>読むページに応じて第五主義へ改宗させる SCP-1425 です。</summary>
public sealed class Scp1425 : CustomUsable, IScp914Upgradable
{
    /// <inheritdoc/>
    public Scp914RuleSet Scp914Rules => new Scp914RuleSet
    {
        Rough = Scp914Rule.Destroy,
        Coarse = Scp914Rule.Destroy,
        OneToOne = Scp914Rule.To<GoCRecruitPaper>(),
        Fine = Scp914Rule.Destroy,
        VeryFine = Scp914Rule.Destroy,
    };

    private readonly Dictionary<Player, byte> readCounts = [];

    public override ItemType BaseType => ItemType.Medkit;

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => Color.magenta;

    /// <inheritdoc/>
    protected override string PickupModel => "Scp1425Model";

    public override string Name => "SCP-1425";

    public override string Description => "第五的な力を感じる・・・";

    protected override int MaxUses => 0;

    protected override void OnUse(Player player)
    {
        byte page = readCounts.TryGetValue(player, out byte count) ? count : (byte)0;
        TrackReader(player);

        switch (page)
        {
            case 0:
                player.SendHint("<size=22>1ページ目\n壊れた星の五本の輻</size>", 5f);
                break;
            case 1:
                player.SendHint("<size=22>2ページ目\n永遠に争う五つの元素</size>", 5f);
                break;
            case 2:
                player.SendHint("<size=22>3ページ目\n<color=#ff00fa>精神を呼び起こす五つの感覚</color></size>", 5f);
                ConvertAfterFlash<FifthistConvert>(player);
                break;
            case 3:
                player.SendHint("<size=22>4ページ目\n五つの欠片が緩慢に解かれてゆく</size>", 5f);
                break;
            default:
                player.SendHint("<size=22>5ページ目\n<b><color=#ff00fa>咆哮する黒き月は、見るに値しない幻である</color></b></size>", 5f);
                ConvertAfterFlash<FifthistMarionetteRole>(player);
                break;
        }

        readCounts[player] = page >= 4 ? (byte)0 : (byte)(page + 1);
    }

    private void TrackReader(Player player)
    {
        if (readCounts.ContainsKey(player)) return;

        readCounts[player] = 0;
        PlayerScope.Of(player).OnDispose(owner => readCounts.Remove(owner));
    }

    private static void ConvertAfterFlash<T>(Player player) where T : CustomRole, new()
    {
        player.EnableEffect<Flashed>(1, 3f);
        PlayerScope.Of(player).Delay(2.5f, owner =>
        {
            if (owner is { IsDestroyed: false, IsAlive: true })
                CustomRole.Spawn<T>(owner);
        });
    }
}
