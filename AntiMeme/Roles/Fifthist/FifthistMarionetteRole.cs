using System;
using System.Collections.Generic;
using AntiMeme.Items.Keycards;
using AntiMeme.Items.Melee;
using AntiMeme.Items.Utility;
using AntiMeme.Items.Weapons;
using AntiMeme.Maps;
using LabApi.Features.Wrappers;
using PlayerRoles;
using UnityEngine;

namespace AntiMeme.Roles.Fifthist;

public sealed class FifthistMarionetteRole : FifthistRole
{
    public override string Name => "Fifthist Marionette";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#ff5ffa>Fifthist: Marionette</color>";

    /// <inheritdoc/>
    public override string Objective => "第五教会に従い、生存者どもを騙しながら第五しろ！";
    public override string Description => "ピンクの光によって作り替えられてしまった人間の成れの果て。\n第五教会に従い、生存者どもを騙しながら第五しろ！";
    public override RoleTypeId BaseRole => RoleTypeId.Scp0492;
    public override float? MaxHealth => 100f;
    public override string CustomInfo => "<color=#FF0090>Fifthist Marionette</color>";
}
