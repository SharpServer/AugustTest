using System.Collections.Generic;
using AntiMeme.Maps;
using AntiMeme.Teams.Factions;
using CustomPlayerEffects;
using PlayerRoles;
using Sliced.API.Features;
using Sliced.API.Structs;
using UnityEngine;

namespace AntiMeme.Roles.Initiative;

/// <summary>
/// Horizon Initiative Wolf。SCP-049 sense パッチ自体は別ドメインの責務です。
/// </summary>
public sealed class InitiativeWolf : CustomRole
{
    public override string Name => "Horizon Initiative Wolf";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#4DFFB8>Horizon Initiative Wolf</color>";

    /// <inheritdoc/>
    public override string Objective => "異なる神に属する奴らを一人残らず絶滅させろ";

    public override CustomTeam Team => CustomTeam.Get<InitiativeTeam>();

    public override string Description => "Xx_ULTIMATE_xX";

    public override RoleTypeId BaseRole => RoleTypeId.Scp049;

    public override Vector3? SpawnPosition => SpawnPoints.NtfEntry;

    public override float? MaxHealth => 110f;

    public override string CustomInfo => "<color=#4DFFB8>Horizon Initiative Wolf</color>";

    public override IReadOnlyList<RoleEffect> Effects =>
    [
        RoleEffect.Of<MovementBoost>(23),
    ];

    protected override void OnSpawned()
    {
        // 旧役職は SCP-049 のヒュームシールドを使わない。
        SetHumeShield(0f);

        Scope.Delay(1.5f, _ =>
        {
            if (Of(Player) is not InitiativeWolf) return;

            RoleWear wear = RoleWear.AttachSchematic(
                Player,
                "SCP035",
                Vector3.forward * 0.05f + Vector3.up * 0.65f,
                Vector3.one * 1.285f);

            if (wear is null) return;

            wear.WithLight(Color.yellow, 20f, 8f);
            Scope.OnDispose(__ => wear.Dispose());
        });
    }
}
