using System.Collections.Generic;
using System.Linq;
using LabApi.Features.Wrappers;
using MEC;
using UnityEngine;

namespace AntiMeme.Maps.Features;

/// <summary>登録した Sinkhole へ近づいたプレイヤーへ脱落通知を送ります。</summary>
public sealed class Sinkhole : MapFeature
{
    private static readonly List<Vector3> Points = new List<Vector3>();
    private readonly HashSet<int> joining = new HashSet<int>();
    private CoroutineHandle loop;
    public float Radius { get; set; } = 1.5f;
    public static void Register(Vector3 point) { if (!Points.Contains(point)) Points.Add(point); }
    public static void ClearPoints() => Points.Clear();
    public override void RegisterEvents() => loop = Timing.RunCoroutine(Tick());
    public override void UnregisterEvents() => Timing.KillCoroutines(loop);
    private IEnumerator<float> Tick()
    {
        while (true)
        {
            foreach (Player player in Player.ReadyList.Where(player => player is { IsDestroyed: false, IsAlive: true }))
            {
                Vector3 point = Points.OrderBy(candidate => (candidate - player.Position).sqrMagnitude).FirstOrDefault();
                if (Points.Count == 0 || Vector3.Distance(player.Position, point) > Radius || !joining.Add(player.PlayerId)) continue;
                player.SendHint("足元が崩れた！", 3f);
                player.Position = point + Vector3.down;
                joining.Remove(player.PlayerId);
            }
            yield return Timing.WaitForSeconds(.25f);
        }
    }
    protected override void Reset() { joining.Clear(); Points.Clear(); }
}
