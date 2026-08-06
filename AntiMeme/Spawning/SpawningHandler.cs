using System.Linq;
using AntiMeme.Hud;
using AntiMeme.Maps;
using AntiMeme.Roles.Scps;
using AntiMeme.Spawning.Waves;
using PlayerRoles;
using Sliced.API.Enums;
using Sliced.API.Features;
using UnityEngine;
using AntiMeme.Roles.Fifthist;

namespace AntiMeme.Spawning;

/// <summary>
/// 湧いた波のテーマ曲とアナウンスを流します。
///
/// <para>
/// どの曲を鳴らすか・何と読み上げるかは波が自分で持っている
/// (<see cref="WaveSet.Theme"/> / <see cref="WaveSet.Announcement"/>) ので、
/// ここは受け取って流すだけです。旧実装は 24 分岐の switch が 2 つありました。
/// </para>
/// </summary>
public sealed class SpawningHandler : EventHandlerBase
{
    public override HandlerLifetime Lifetime => HandlerLifetime.Manual;

    protected override void OnEnabled()
    {
        SpawnSystem.Spawning += OnSpawning;
        SpawnSystem.Spawned += OnSpawned;
    }

    protected override void OnDisabled()
    {
        SpawnSystem.Spawning -= OnSpawning;
        SpawnSystem.Spawned -= OnSpawned;
    }

    /// <summary>
    /// 第五教会の司祭か SCP-3005 が居ると、第五教会と Eta-10 が湧くようになります。
    /// </summary>
    private static void OnSpawning(object sender, SpawnSystem.CustomSpawningEventArgs ev)
    {
        if (ev.Wave is not null || ev.NowContext is not DefaultContext) return;

        bool blessed = CustomRole.Active.Any(role => role is Scp3005 or FifthistPriest);

        if (!blessed) return;

        ev.Wave = ev.Faction switch
        {
            Faction.FoundationEnemy => Pick(ev.IsMiniWave, 40, 60),
            Faction.FoundationStaff => PickStaff(ev.IsMiniWave),
            _ => null,
        };
    }

    /// <summary>
    /// 第五教会が出る状態のときの敵対側抽選です。
    /// </summary>
    private static WaveSet Pick(bool miniWave, int fifthistWeight, int chaosWeight)
    {
        int roll = Random.Range(0, fifthistWeight + chaosWeight);

        if (roll < fifthistWeight)
            return Find(miniWave, typeof(FifthistWave), typeof(FifthistBackupWave));

        return Find(miniWave, typeof(ChaosInsurgencyWave), typeof(ChaosBackupWave));
    }

    /// <summary>
    /// 第五教会が出る状態のときの財団側抽選です。Eta-10 が出るようになります。
    /// </summary>
    private static WaveSet PickStaff(bool miniWave)
    {
        int roll = Random.Range(0, 100);

        if (roll < 40) return Find(miniWave, typeof(NtfWave), typeof(NtfBackupWave));
        if (roll < 60) return Find(miniWave, typeof(HdWave), typeof(HdBackupWave));

        return Find(miniWave, typeof(SneWave), typeof(SneBackupWave));
    }

    private static WaveSet Find(bool miniWave, System.Type normal, System.Type backup)
    {
        System.Type wanted = miniWave ? backup : normal;

        return SpawnContext.AllWaves.FirstOrDefault(wave => wave.GetType() == wanted);
    }

    private static void OnSpawned(object sender, SpawnSystem.CustomSpawningEventArgs ev)
    {
        if (ev.Wave is null || ev.SpawnCount <= 0) return;

        if (ev.Wave.Theme is { } theme)
            MapAudio.Play(theme, "WaveTheme", Vector3.zero, maxDistance: 999999999f);

        (string cassie, string subtitle) = ev.Wave.Announcement(ev.SpawnCount);

        if (!string.IsNullOrEmpty(cassie))
            FacilityAnnouncer.Say(cassie, subtitle, true);
    }
}
