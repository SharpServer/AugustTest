using System.Linq;
using AntiMeme.Audio;
using AntiMeme.Maps.Features;
using AntiMeme.Roles.Scps;
using CustomPlayerEffects;
using LabApi.Features.Wrappers;
using MapGeneration;
using PlayerRoles;
using Sliced.API.Features;
using UnityEngine;

using ExiledPlayer = Exiled.API.Features.Player;

namespace AntiMeme.Abilities;

/// <summary>SCP-106 がポケットディメンション外縁の捕虜を解放して収容室へ戻る能力です。</summary>
public sealed class AllowEscapeAbility : AbilityBase
{
    public override string Name => "腐蝕からの解放";

    public override string Description => "ポケットディメンション外縁の生存者を解放し、自身を収容室へ戻します。";

    public override float Cooldown => 999f;

    public override int MaxUses => 1;

    protected override void OnUsed()
    {
        foreach (Player captive in PDEx.Players.ToArray())
        {
            if (captive is not { IsDestroyed: false }) continue;

            Room destination = Room.List
                .Where(room => room.Name is not (RoomName.Pocket or RoomName.Outside))
                .OrderBy(_ => UnityEngine.Random.value)
                .FirstOrDefault();
            if (destination is not null)
                captive.Position = destination.Transform.TransformPoint(Vector3.up * 0.25f);

            captive.DisableEffect<Slowness>();
            captive.DisableEffect<PocketCorroding>();
            captive.EnableEffect<Traumatized>();
            PDEx.Players.Remove(captive);
        }

        Vector3? containment = Room.Get(RoomName.Hcz106).FirstOrDefault()?.Transform.TransformPoint(Vector3.up * 0.25f);
        foreach (Player king in Player.ReadyList.Where(IsScp106).ToArray())
        {
            AbilityBase.Revoke<AllowEscapeAbility>(king);
            if (ExiledPlayer.Get(king.ReferenceHub) is { } exiled)
                ProximityVoice.SetForced(exiled, false);

            if (containment.HasValue)
                king.Position = containment.Value;
        }
    }

    private static bool IsScp106(Player player) =>
        CustomRole.Of(player) is Scp106 ||
        (CustomRole.Of(player) is null && player.Role == RoleTypeId.Scp106);
}
