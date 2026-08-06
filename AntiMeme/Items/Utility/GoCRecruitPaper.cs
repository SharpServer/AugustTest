using AntiMeme.Items.Bases;
using AntiMeme.Items.Scp914;
using LabApi.Features.Wrappers;
using Sliced.API.Features;
using AntiMeme.Roles.GoC;
using UnityEngine;

namespace AntiMeme.Items.Utility;

/// <summary>使用者を GoC 一般工作員へ編入する装備申請書です。</summary>
public sealed class GoCRecruitPaper : CustomUsable, IScp914Upgradable
{
    /// <inheritdoc/>
    public Scp914RuleSet Scp914Rules => new Scp914RuleSet
    {
        Rough = Scp914Rule.Destroy,
        Coarse = Scp914Rule.Destroy,
        OneToOne = Scp914Rule.To<Scp1425>(),
        Fine = Scp914Rule.Destroy,
        VeryFine = Scp914Rule.Destroy,
    };

    public override ItemType BaseType => ItemType.Medkit;

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => new Color(0f, 0f, 200f / 255f);

    public override string Name => "UNGOC一般工作員セット";

    public override string Description =>
        "一般工作員一人分のアイテムが入っている。\nこれを使えば工作員を一人だけ増やせる。";

    protected override bool DestroyWhenDepleted => false;

    protected override void OnUse(Player player) => CustomRole.Spawn<GoCOperative>(player);
}
