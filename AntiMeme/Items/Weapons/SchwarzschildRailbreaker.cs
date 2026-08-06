using System;
using AntiMeme.Items.Melee;
using AntiMeme.Items.Scp914;

namespace AntiMeme.Items.Weapons;

/// <summary>クエィサァーと超電磁砲を切り替える複合武器です。</summary>
public sealed class SchwarzschildRailbreaker : HybridWeapon, IScp914Upgradable
{
    /// <inheritdoc/>
    public Scp914RuleSet Scp914Rules => new Scp914RuleSet
    {
        Rough = Scp914Rule.To<SchwarzschildQuasar>(),
        Coarse = Scp914Rule.To<GunGoCRailgunFull>(),
        OneToOne = Scp914Rule.Keep,
        Fine = Scp914Rule.Keep,
        VeryFine = Scp914Rule.Custom(Scp914Detonation.Blast),
    };

    public override string Name => "シュバルツシルト・レイルブレイカー";
    public override string Description => "クエィサァーとレールガンを切り替えられる複合武器。投擲操作で切り替える。";
    public override ItemType BaseType => ItemType.Jailbird;
    protected override Type NextMode => typeof(GunGoCRailgunFull);
}
