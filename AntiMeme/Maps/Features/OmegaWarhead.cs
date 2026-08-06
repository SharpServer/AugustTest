using System;
using System.Collections.Generic;
using System.Linq;
using AntiMeme.Maps.Objects;
using LabApi.Features.Wrappers;
using MEC;
using UnityEngine;

namespace AntiMeme.Maps.Features.Warhead;

/// <summary>OMEGA 起動シーケンスの状態と外部通知です。</summary>
public static class OmegaWarhead
{
    private static CoroutineHandle sequence;
    public static bool IsWarheadStarted { get; private set; }
    public static event EventHandler<OmegaWarheadStartingEventArgs> Starting;
    public static event Action Detonating;
    public static bool StartProtocol(float triggerTime = 0f, Player startedBy = null)
    {
        if (IsWarheadStarted) return false;
        Timing.KillCoroutines(sequence);
        sequence = Timing.RunCoroutine(Run(triggerTime, startedBy));
        return true;
    }
    private static IEnumerator<float> Run(float delay, Player player)
    {
        if (delay > 0f) yield return Timing.WaitForSeconds(delay);
        OmegaWarheadStartingEventArgs args = new OmegaWarheadStartingEventArgs(player, true);
        Starting?.Invoke(null, args);
        if (!args.IsAllowed) yield break;
        IsWarheadStarted = true;
        AlarmLight.SetAlarmState(true);
        MapAudio.Play("omega_v2.ogg", "OmegaWarhead", Vector3.zero, maxDistance: 999f);
        yield return Timing.WaitForSeconds(5f);
        Detonating?.Invoke();
        foreach (Player target in Player.ReadyList.Where(target => target is { IsDestroyed: false, IsAlive: true })) target.Damage(100000f, "OMEGA WARHEAD");
        Reset();
    }
    public static void Reset() { Timing.KillCoroutines(sequence); sequence = default; IsWarheadStarted = false; AlarmLight.SetAlarmState(false); }
}
