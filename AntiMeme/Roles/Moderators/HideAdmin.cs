using System;
using System.Collections.Generic;
using AntiMeme.Items.Armor;
using AntiMeme.Items.Utility;
using CustomPlayerEffects;
using LabApi.Features.Wrappers;
using PlayerRoles;
using Sliced.API.Structs;

namespace AntiMeme.Roles.Moderators;

public sealed class HideAdmin : ModeratorRoleBase
{
    private bool previousBypass;
    private bool previousNoclip;
    private bool previousSpectatable;

    public override string Name => "THE ADMINISTRATOR";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#FF1493><b>THE ADMINISTRATOR</b></color>";

    /// <inheritdoc/>
    public override string TeamLabel => "<color=#FF1493>THE ADMINISTRATOR</color>";

    /// <inheritdoc/>
    public override string Objective => "なぁ～んでもできる！";
    public override string Description => "なぁ～んでもできる！";
    public override RoleTypeId BaseRole => RoleTypeId.Tutorial;
    public override float? MaxHealth => 99999f;
    public override string CustomInfo => "<color=#FF1493>THE ADMINISTRATOR</color>";
    public override IReadOnlyList<ItemType> Items => [ ItemType.KeycardO5 ];
    public override IReadOnlyList<Type> CustomItems => [ typeof(CloakGenerator) ];
    public override IReadOnlyList<RoleEffect> Effects =>
    [ RoleEffect.Of<DamageReduction>(255), RoleEffect.Of<Fade>(255), RoleEffect.Of<NightVision>(255) ];

    protected override void OnSpawned()
    {
        previousBypass = Player.IsBypassEnabled;
        previousNoclip = Player.IsNoclipEnabled;
        previousSpectatable = Player.IsSpectatable;
        Player.IsBypassEnabled = true;
        Player.IsNoclipEnabled = true;
        Player.IsSpectatable = false;
    }

    protected override void OnRemoved()
    {
        if (Player is not { IsDestroyed: false }) return;

        Player.IsBypassEnabled = previousBypass;
        Player.IsNoclipEnabled = previousNoclip;
        Player.IsSpectatable = previousSpectatable;
    }
}
