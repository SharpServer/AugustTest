using System.Collections.Generic;
using System.Linq;
using AntiMeme.Abilities;
using AntiMeme.Audio;
using AntiMeme.Roles.Scps;
using CustomPlayerEffects;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using MEC;
using PlayerRoles;
using Sliced.API.Features;
using UnityEngine;

using ExiledFailingEscapeEventArgs = Exiled.Events.EventArgs.Player.FailingEscapePocketDimensionEventArgs;
using ExiledPlayer = Exiled.API.Features.Player;
using ExiledPlayerEvents = Exiled.Events.Handlers.Player;

namespace AntiMeme.Maps.Features;

/// <summary>ポケットディメンション外縁の落下保護を管理します。</summary>
public sealed class PDEx : MapFeature
{
    public static readonly List<Player> Players = new List<Player>();
    private CoroutineHandle loop;

    public override void RegisterEvents()
    {
        ServerEvents.RoundStarted += Start;
        ExiledPlayerEvents.FailingEscapePocketDimension += OnFailingEscape;
    }
    public override void UnregisterEvents()
    {
        ServerEvents.RoundStarted -= Start;
        ExiledPlayerEvents.FailingEscapePocketDimension -= OnFailingEscape;
        Timing.KillCoroutines(loop);
    }

    private static void OnFailingEscape(ExiledFailingEscapeEventArgs ev)
    {
        if (UnityEngine.Random.Range(0, 3) != 0 ||
            !MapPoints.TryGet("PDEX_JoinPoint", out Vector3 captivePoint) ||
            !MapPoints.TryGet("PDEX_JoinPointKing", out Vector3 kingPoint))
        {
            return;
        }

        Player[] kings = Player.ReadyList.Where(IsLivingScp106).ToArray();
        if (kings.Length == 0 || ev.Player?.ReferenceHub == null) return;

        Player captive = Player.Get(ev.Player.ReferenceHub);
        if (captive is null) return;

        ev.IsAllowed = false;
        captive.Position = captivePoint;
        captive.DisableEffect<PocketCorroding>();
        captive.EnableEffect<Slowness>(30);
        if (!Players.Contains(captive)) Players.Add(captive);

        foreach (Player king in kings)
        {
            king.Position = kingPoint;
            AbilityBase.Give<AllowEscapeAbility>(king);
            king.SendHint(
                "アビリティ「腐蝕からの解放」が付与されました。\n人間を釈放したくなったら使ってください。\n近接チャットも一時的に利用可能です！",
                8f);

            if (ExiledPlayer.Get(king.ReferenceHub) is { } exiled)
                ProximityVoice.SetForced(exiled, true);
        }
    }

    private static bool IsLivingScp106(Player player) =>
        player is { IsDestroyed: false, IsAlive: true } &&
        (CustomRole.Of(player) is Scp106 ||
         (CustomRole.Of(player) is null && player.Role == RoleTypeId.Scp106));

    private void Start() { Timing.KillCoroutines(loop); loop = Timing.RunCoroutine(Tick()); }
    private IEnumerator<float> Tick()
    {
        while (true)
        {
            Players.RemoveAll(player => player is not { IsDestroyed: false });
            foreach (Player player in Player.ReadyList)
            {
                if (player is not { IsDestroyed: false, IsAlive: true } || player.Position.y >= -450f) continue;
                if (!Players.Contains(player)) Players.Add(player);
                player.EnableEffect<PocketCorroding>();
            }
            yield return Timing.WaitForSeconds(1f);
        }
    }

    protected override void Reset()
    {
        Timing.KillCoroutines(loop);
        Players.Clear();
    }
}
