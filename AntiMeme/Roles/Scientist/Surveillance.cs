using System;
using System.Collections.Generic;
using AntiMeme.Items.Keycards;
using AntiMeme.Items.Utility;
using AntiMeme.Items.Weapons;
using MapGeneration;
using PlayerRoles;
using UnityEngine;
using AntiMeme.Roles.Scps;

namespace AntiMeme.Roles.Scientist;

public sealed class Surveillance : ScientistRole
{
    public override string Name => "Surveillance";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#faff86>Surveillance</color>";

    /// <inheritdoc/>
    public override string Objective => "施設の状況を監視し、脱出の機会を見極めよ。";
    public override string Description => "W.I.P";
    public override RoleTypeId BaseRole => RoleTypeId.Scp079;
    public override float? MaxHealth => 100f;
    public override string CustomInfo => "Surveillance";
}
