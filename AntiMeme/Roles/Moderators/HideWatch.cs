using System;
using System.Collections.Generic;
using AntiMeme.Items.Armor;
using AntiMeme.Items.Utility;
using CustomPlayerEffects;
using LabApi.Features.Wrappers;
using PlayerRoles;
using Sliced.API.Structs;

namespace AntiMeme.Roles.Moderators;

public sealed class HideWatch : ModeratorRoleBase
{
    public override string Name => "THE HIDEWATCH";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#00B7EB><b>THE HIDEWATCH</b></color>";

    /// <inheritdoc/>
    public override string TeamLabel => "<color=#FF1493>THE ADMINISTRATOR</color>";

    /// <inheritdoc/>
    public override string Objective => "ぐへへへへ";
    public override string Description => "ぐへへへへ";
    public override RoleTypeId BaseRole => RoleTypeId.Overwatch;
    public override string CustomInfo => "<color=#00ffff>THE HIDEWATCH</color>";
}
