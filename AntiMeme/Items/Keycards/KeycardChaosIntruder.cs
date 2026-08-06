using AntiMeme.Items.Bases;
using AntiMeme.Items.Scp914;
using Interactables.Interobjects.DoorUtils;
using UnityEngine;

namespace AntiMeme.Items.Keycards;

/// <summary>
/// カオスの侵入部隊用アクセスデバイスです。
/// </summary>
/// <remarks>
/// 旧実装はカオスのバニラカード (<c>KeycardChaosInsurgency</c>) を土台にして、
/// 拾得時と生成時に <c>Permissions</c> を毎回上書きしていました。
/// 現行のキーカードは権限を Detail に焼き付ける作りで、
/// バニラカードの Detail は差し替えられません。
/// 権限を自前で決めるカードはカスタムカード (Site02) を土台にします。
/// </remarks>
public sealed class KeycardChaosIntruder : CustomKeycard, IScp914Upgradable
{
    /// <inheritdoc/>
    public Scp914RuleSet Scp914Rules => new Scp914RuleSet
    {
        Rough = Scp914Rule.Destroy,
        Coarse = Scp914Rule.To<KeycardConscripts>(),
        OneToOne = Scp914Rule.To<AccessTunerLv1>(),
        Fine = Scp914Rule.ToVanilla(ItemType.KeycardChaosInsurgency),
        VeryFine = Scp914Rule.Weighted(
            (0.90f, Scp914Rule.To<KeycardArmoryLevel3>()),
            (0.10f, Scp914Rule.To<MasterCard>())),
    };

    /// <inheritdoc/>
    public override string Name => "Chaos Intruder Device";

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => new Color32(0, 75, 0, 255);

    /// <inheritdoc/>
    public override string Description => "カオスの権限を持つ侵入部隊用デバイス。";

    /// <inheritdoc/>
    protected override KeycardLevels Levels => new KeycardLevels(1, 2, 1);

    /// <inheritdoc/>
    protected override string Label => "INTRUDER";

    /// <inheritdoc/>
    protected override Color32 LabelColor => new Color32(218, 255, 218, 255);

    /// <inheritdoc/>
    protected override Color32 Tint => new Color32(20, 90, 38, 255);

    /// <inheritdoc/>
    protected override Color32 PermissionsColor => new Color32(4, 32, 12, 255);

    /// <inheritdoc/>
    protected override string HolderName => "Chaos Intruder";
}
