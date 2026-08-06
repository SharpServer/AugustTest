using System.Collections.Generic;
using AntiMeme.Maps.Objects;
using MEC;
using UnityEngine;

namespace AntiMeme.Maps.Features;

/// <summary>列車スキマティックを周期的に生成・破棄します。</summary>
public static class TerminalTrain
{
    private static CoroutineHandle loop;
    private static TerminalTrainCar current;
    private static Vector3 start, checkpoint, end;
    public static bool IsRunning => loop.IsRunning;
    public static void Start(Vector3 startPoint, Vector3 checkpointPoint, Vector3 endPoint)
    {
        Stop(); start = startPoint; checkpoint = checkpointPoint; end = endPoint;
        loop = Timing.RunCoroutine(Run());
    }
    public static void Stop()
    {
        Timing.KillCoroutines(loop); loop = default;
        current?.Destroy(); current = null;
    }
    private static IEnumerator<float> Run()
    {
        while (true)
        {
            current = (TerminalTrainCar)new TerminalTrainCar { Position = start }.Create();
            yield return Timing.WaitForSeconds(3f);
            if (current is { IsAlive: true }) current.Position = checkpoint;
            yield return Timing.WaitForSeconds(35f);
            if (current is { IsAlive: true }) current.Position = end;
            yield return Timing.WaitForSeconds(3f);
            current?.Destroy(); current = null;
            yield return Timing.WaitForSeconds(50f);
        }
    }
}

public sealed class TerminalTrainRuntime : MapFeature
{
    public override void RegisterEvents() => LabApi.Events.Handlers.ServerEvents.RoundStarted += StartFromPoints;
    public override void UnregisterEvents()
    {
        LabApi.Events.Handlers.ServerEvents.RoundStarted -= StartFromPoints;
        TerminalTrain.Stop();
    }
    private static void StartFromPoints()
    {
        if (MapPoints.TryGet("TrainStartPoint", out Vector3 start) &&
            MapPoints.TryGet("TrainCheckpointPoint", out Vector3 checkpoint) &&
            MapPoints.TryGet("TrainEndPoint", out Vector3 end))
            TerminalTrain.Start(start, checkpoint, end);
    }
    protected override void Reset() => TerminalTrain.Stop();
}
