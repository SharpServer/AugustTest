using System.Collections.Generic;
using System.Linq;
using AntiMeme.Maps.Objects;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using MEC;
using UnityEngine;

namespace AntiMeme.Maps.Features;

/// <summary>SCP-513 の対象リストと追跡演出の生成周期を管理します。</summary>
public static class Scp513
{
    private static readonly HashSet<Player> Targets = new HashSet<Player>();
    private static CoroutineHandle loop;
    public static void AddTarget(Player player) { if (player is { IsDestroyed: false }) Targets.Add(player); }
    public static void RemoveTarget(Player player) { if (player is not null) Targets.Remove(player); }
    public static void Start() { Stop(); loop = Timing.RunCoroutine(Run()); }
    public static void Stop() { Timing.KillCoroutines(loop); loop = default; }
    private static IEnumerator<float> Run()
    {
        while (true)
        {
            foreach (Player player in Targets.ToArray().Where(player => player is { IsDestroyed: false, IsAlive: true }))
            {
                new Scp513Manifestation { TargetPlayer = player, Lifespan = 2f }.Create();
            }
            yield return Timing.WaitForSeconds(10f);
        }
    }
}
