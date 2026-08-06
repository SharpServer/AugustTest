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
using AntiMeme.Roles.Fifthist;
using AntiMeme.Roles.FoundationForces;
using AntiMeme.Roles.GoC;

namespace AntiMeme.Teams.Escape;

/// <summary>通常ラウンドの陣営・拘束者による脱出先です。</summary>
public sealed class DefaultEscapeRule : EscapeRule
{
    public override int Priority => 100;

    public override EscapeTarget Resolve(EscapeContext context)
    {
        CustomTeam team = context.PlayerTeam;
        CustomTeam cuffer = context.CufferTeam;

        if (Is<ClassDTeam>(team))
        {
            if (IsFoundation(cuffer)) return EscapeTarget.VanillaRole(RoleTypeId.NtfPrivate);
            if (Is<FifthistTeam>(cuffer)) return EscapeTarget.CustomRole<FifthistConvert>();
            if (Is<GoCTeam>(cuffer)) return EscapeTarget.CustomRole<GoCOperative>();

            return EscapeTarget.VanillaRole(RoleTypeId.ChaosConscript);
        }

        if (Is<ScientistsTeam>(team))
        {
            if (IsInsurgency(cuffer)) return EscapeTarget.VanillaRole(RoleTypeId.ChaosConscript);
            if (Is<FifthistTeam>(cuffer)) return EscapeTarget.CustomRole<FifthistConvert>();
            if (Is<GoCTeam>(cuffer)) return EscapeTarget.CustomRole<GoCOperative>();

            return EscapeTarget.VanillaRole(RoleTypeId.NtfSpecialist);
        }

        if (Is<ChaosInsurgencyTeam>(team))
        {
            if (IsFoundation(cuffer)) return EscapeTarget.VanillaRole(RoleTypeId.NtfPrivate);
            if (Is<FifthistTeam>(cuffer)) return EscapeTarget.CustomRole<FifthistConvert>();
            if (Is<GoCTeam>(cuffer)) return EscapeTarget.CustomRole<GoCOperative>();

            return EscapeTarget.None;
        }

        if (Is<FoundationForcesTeam>(team) || Is<GuardsTeam>(team))
        {
            if (IsInsurgency(cuffer)) return EscapeTarget.VanillaRole(RoleTypeId.ChaosConscript);
            if (Is<FifthistTeam>(cuffer)) return EscapeTarget.CustomRole<FifthistConvert>();
            if (Is<GoCTeam>(cuffer)) return EscapeTarget.CustomRole<GoCOperative>();

            return EscapeTarget.None;
        }

        if (Is<FifthistTeam>(team))
        {
            if (IsInsurgency(cuffer)) return EscapeTarget.VanillaRole(RoleTypeId.ChaosConscript);
            if (IsFoundation(cuffer)) return EscapeTarget.VanillaRole(RoleTypeId.NtfPrivate);
            if (Is<GoCTeam>(cuffer)) return EscapeTarget.CustomRole<GoCOperative>();
        }

        return EscapeTarget.None;
    }

    private static bool Is<T>(CustomTeam team) where T : CustomTeam, new() =>
        ReferenceEquals(team, CustomTeam.Get<T>());

    private static bool IsFoundation(CustomTeam team) =>
        Is<FoundationForcesTeam>(team) || Is<ScientistsTeam>(team) || Is<GuardsTeam>(team);

    private static bool IsInsurgency(CustomTeam team) =>
        Is<ChaosInsurgencyTeam>(team) || Is<ClassDTeam>(team);
}
