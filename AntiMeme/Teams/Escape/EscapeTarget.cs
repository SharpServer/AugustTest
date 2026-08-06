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

namespace AntiMeme.Teams.Escape;

/// <summary>
/// 脱出後に付ける役職です。バニラ役職かカスタム役職型のどちらか一方を持ちます。
/// </summary>
public readonly struct EscapeTarget
{
    private EscapeTarget(RoleTypeId? vanilla, Type custom)
    {
        Vanilla = vanilla;
        Custom = custom;
    }

    public RoleTypeId? Vanilla { get; }

    public Type Custom { get; }

    public bool IsEmpty => Vanilla is null && Custom is null;

    public static EscapeTarget None => default;

    public static EscapeTarget VanillaRole(RoleTypeId role) => new EscapeTarget(role, null);

    public static EscapeTarget CustomRole<T>() where T : CustomRole, new() =>
        new EscapeTarget(null, typeof(T));
}
