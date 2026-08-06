using System.Collections.Generic;
using System.Linq;
using AntiMeme.Audio;
using AntiMeme.GameModes;
using AntiMeme.Maps.Objects;
using AntiMeme.Net;
using CustomPlayerEffects;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using MEC;
using PlayerRoles;
using Sliced.API.Features;
using UnityEngine;

using ExiledRound = Exiled.API.Features.Round;

namespace AntiMeme.Maps.Features;

/// <summary>
/// ラウンド開始待ちのロビーです。参加者を専用の部屋へ集め、看板に状況を出し、
/// カウントダウンが尽きる直前に開始演出へ入ります。
/// </summary>
/// <remarks>
/// 開始の足止めは <c>Round.IsLobbyLocked</c> で行います。
/// <c>CharacterClassManager.Init()</c> の内部ループが自前の残り時間を毎秒書き戻すため、
/// <c>LobbyWaitingTime</c> を直接いじっても次の tick で上書きされます。
/// ロックはそのループ自身が見ている分岐なので、確実に止まります。
/// </remarks>
public sealed class Lobby : MapFeature
{
    private const string MusicClip = "finalflash.ogg";
    private const string OutroClip = "finalflash_outro.ogg";
    private const string MusicSpeaker = "Lobby_Music_";
    private const string OutroSpeaker = "Lobby_Outro_";

    private const float MusicStartDelay = 1.5f;
    private const float MusicFadeIn = 3f;
    private const float TickInterval = 0.05f;
    private const float TriggerRemainingTime = 1f;
    private const int MinimumPlayers = 2;

    /// <summary>開始演出の待ち時間の調整幅です。負値なら outro の途中で開始します。</summary>
    private const float OutroExtraHold = -7.77f;

    private const float MoveDuration = 1.6f;
    private const float FadeDuration = 3f;

    private static readonly Vector3 WaitPosition = new Vector3(246.92f, 198.50f, -60.89f);
    private static readonly Vector3 MoveTarget = new Vector3(247.15f, 199.30f, -63.33f);
    private static readonly Vector3 FadeTarget = new Vector3(247.15f, 199.30f, -63.64f);
    private static readonly Quaternion FacingOut = Quaternion.Euler(0f, 180f, 0f);

    /// <summary>ロビーで Tutorial にしたプレイヤーです。役職割り当てはここも拾います。</summary>
    public static readonly HashSet<Player> Waiting = new HashSet<Player>();

    private readonly Dictionary<Player, SpeakerApi.Playback> music =
        new Dictionary<Player, SpeakerApi.Playback>();

    private readonly List<CoroutineHandle> handles = new List<CoroutineHandle>();

    private bool triggered;
    private bool lockedForPlayerCount;

    /// <inheritdoc/>
    public override void RegisterEvents()
    {
        ServerEvents.WaitingForPlayers += OnWaitingForPlayers;
        ServerEvents.RoundStarted += OnRoundStarted;
        PlayerEvents.Joined += OnJoined;
        PlayerEvents.Left += OnLeft;
        PlayerEvents.ValidatedVisibility += OnValidatedVisibility;
    }

    /// <inheritdoc/>
    public override void UnregisterEvents()
    {
        ServerEvents.WaitingForPlayers -= OnWaitingForPlayers;
        ServerEvents.RoundStarted -= OnRoundStarted;
        PlayerEvents.Joined -= OnJoined;
        PlayerEvents.Left -= OnLeft;
        PlayerEvents.ValidatedVisibility -= OnValidatedVisibility;

        Cleanup();
        ExiledRound.IsLobbyLocked = false;
    }

    /// <inheritdoc/>
    protected override void Reset() => Cleanup();

    private void OnWaitingForPlayers()
    {
        Cleanup();

        // バニラの「ラウンド開始」ボタンは自前の演出と噛み合わないので消しておく。
        if (GameObject.Find("StartRound") is { } startRound)
            startRound.transform.localScale = Vector3.zero;

        triggered = false;
        lockedForPlayerCount = true;
        ExiledRound.IsLobbyLocked = true;

        handles.Add(Timing.RunCoroutine(Run()));
    }

    private void OnJoined(PlayerJoinedEventArgs ev)
    {
        if (!ExiledRound.IsLobby || ev.Player is not { IsDestroyed: false, IsNpc: false } player || player.IsHost) return;

        player.SetRole(RoleTypeId.Tutorial, RoleChangeReason.None, RoleSpawnFlags.None);
        player.Rotation *= Quaternion.Euler(0f, 158f, 0f);

        Waiting.Add(player);

        handles.Add(Timing.CallDelayed(MusicStartDelay, () => StartMusic(player)));
        handles.Add(Timing.CallDelayed(MusicStartDelay, () =>
        {
            if (player is { IsDestroyed: false })
                player.EnableEffect<Fade>(255);
        }));
    }

    /// <summary>
    /// ロビーでは全員を同じ場所へ重ねて置くので、互いに見えないようにします。
    /// </summary>
    private static void OnValidatedVisibility(PlayerValidatedVisibilityEventArgs ev)
    {
        if (ExiledRound.IsLobby)
            ev.IsVisible = false;
    }

    private void OnLeft(PlayerLeftEventArgs ev)
    {
        Waiting.Remove(ev.Player);
        StopMusic(ev.Player);
        SpeakerApi.TryDestroy(OutroSpeaker + ev.Player.PlayerId);
    }

    private void OnRoundStarted()
    {
        Cleanup();
        ExiledRound.IsLobbyLocked = false;

        // 演出用に付けた無敵と Noclip を戻す。実スポーンが落ち着いてから触る。
        Timing.CallDelayed(2f, () =>
        {
            foreach (Player player in Player.ReadyList)
            {
                player.IsNoclipEnabled = false;
                player.IsGodModeEnabled = false;
            }
        });
    }

    private IEnumerator<float> Run()
    {
        yield return Timing.WaitForSeconds(0.5f);

        while (ExiledRound.IsLobby)
        {
            if (!triggered)
            {
                HoldForPlayerCount();
                HerdPlayers();

                // 人数不足の間 LobbyWaitingTime は負値になるので、実カウントダウン中だけ見る。
                if (ExiledRound.LobbyWaitingTime > 0 && ExiledRound.LobbyWaitingTime <= TriggerRemainingTime)
                {
                    triggered = true;
                    BeginStartSequence();
                }
            }

            UpdateSigns();

            yield return Timing.WaitForSeconds(TickInterval);
        }
    }

    /// <summary>
    /// ロビーの人数です。<b>看板の表記とロビーロックの解除判定は必ずこれを共有します。</b>
    /// 演出用に立てている内部 NPC は参加者ではないので数に入れません。
    /// </summary>
    private static int PlayerCount => Player.ReadyList.Count(x => !InternalNpcs.IsManaged(x));

    private void HoldForPlayerCount()
    {
        bool tooFew = PlayerCount < MinimumPlayers;

        if (tooFew)
        {
            lockedForPlayerCount = true;
            ExiledRound.IsLobbyLocked = true;
        }
        else if (lockedForPlayerCount)
        {
            lockedForPlayerCount = false;
            ExiledRound.IsLobbyLocked = false;
        }
    }

    private static void HerdPlayers()
    {
        foreach (Player player in Player.ReadyList)
        {
            // NPC は演出・当たり判定のために置いた場所が意味を持つ。ロビーへ引き剥がさない。
            if (player.IsHost || player.IsNpc) continue;

            player.Position = WaitPosition;
            player.IsNoclipEnabled = true;
            player.IsGodModeEnabled = true;
        }
    }

    /// <summary>
    /// 看板の持ち主です。マップが置いた <c>OldMenuRoom</c> に後から掴まるので、
    /// マップの読み込みが終わっていなくても作っておいて構いません。
    /// </summary>
    private LobbyRoom Room()
    {
        if (ObjectPrefab.All.OfType<LobbyRoom>().FirstOrDefault() is { } existing)
            return existing;

        LobbyRoom created = new LobbyRoom { IsSaveable = false };
        created.Create();

        return created;
    }

    private void UpdateSigns()
    {
        LobbyRoom room = Room();

        int count = PlayerCount;

        if (room.PlayerCountText is { } players)
            players.TextFormat = $"<b><u>{count} / {Server.MaxPlayers}</u></b>";

        if (room.NextEventText is { } next)
            next.TextFormat = $"<b><u>Next Event: {GameModeSelection.DisplayName}</u></b>";

        if (room.RemainingTimeText is { } remaining)
        {
            float seconds = triggered ? 0f : ExiledRound.LobbyWaitingTime;
            remaining.TextFormat = $"<b><u>Remaining Time to Start: {(int)seconds}</u></b>";
        }
    }

    /// <summary>
    /// 開始演出です。BGM を outro へ差し替え、全員を扉の外へ運びながら暗転させます。
    /// 演出が終わるまでラウンド開始は足止めします。
    /// </summary>
    private void BeginStartSequence()
    {
        ExiledRound.IsLobbyLocked = true;

        float hold = Mathf.Max(0f, SpeakerApi.GetClipDuration(OutroClip) + OutroExtraHold);

        foreach (Player player in Waiting.ToArray())
        {
            if (player is not { IsDestroyed: false }) continue;

            StopMusic(player);

            int id = player.PlayerId;

            SpeakerApi.Play(
                OutroClip,
                OutroSpeaker + id,
                player.Position,
                destroyOnEnd: true,
                parent: player.GameObject.transform,
                maxDistance: 10f,
                minDistance: 0.1f,
                volume: 1f,
                listeners: listener => listener is not null && listener.Id == id);

            handles.Add(Timing.RunCoroutine(Transition(player)));
        }

        handles.Add(Timing.CallDelayed(hold, () =>
        {
            ExiledRound.IsLobbyLocked = false;

            if (ExiledRound.IsLobby)
                Round.Start();
        }));
    }

    private static IEnumerator<float> Transition(Player player)
    {
        player.EnableEffect<Blindness>(0);

        Vector3 from = player.Position;
        Quaternion facing = player.Rotation;

        for (float elapsed = 0f; elapsed < MoveDuration; elapsed += Time.deltaTime)
        {
            // ExtraHold を詰めるとラウンドが先に始まりうる。その場合は実スポーン位置を壊さない。
            if (player is not { IsDestroyed: false } || !ExiledRound.IsLobby) yield break;

            float progress = elapsed / MoveDuration;
            player.Position = Vector3.Lerp(from, MoveTarget, progress);
            player.Rotation = Quaternion.Lerp(facing, FacingOut, progress);

            yield return 0f;
        }

        for (float elapsed = 0f; elapsed < FadeDuration; elapsed += Time.deltaTime)
        {
            if (player is not { IsDestroyed: false } || !ExiledRound.IsLobby) yield break;

            float progress = elapsed / FadeDuration;
            player.Position = Vector3.Lerp(MoveTarget, FadeTarget, progress);
            player.Rotation = FacingOut;

            if (player.ReferenceHub.playerEffectsController.TryGetEffect(out Blindness blindness))
                blindness.Intensity = (byte)Mathf.RoundToInt(255f * progress);

            yield return 0f;
        }
    }

    /// <summary>
    /// ロビー BGM を本人にだけ流します。開始直後は無音で、そこからフェードインします。
    /// </summary>
    private void StartMusic(Player player)
    {
        if (player is not { IsDestroyed: false } || !ExiledRound.IsLobby || triggered) return;
        if (!Waiting.Contains(player) || music.ContainsKey(player)) return;

        int id = player.PlayerId;

        SpeakerApi.Playback playback = SpeakerApi.PlayLoop(
            MusicClip,
            MusicSpeaker + id,
            player.Position,
            player.GameObject.transform,
            maxDistance: 10f,
            minDistance: 0.1f,
            volume: 0f,
            listeners: listener => listener is not null && listener.Id == id);

        music[player] = playback;
        handles.Add(Timing.RunCoroutine(FadeIn(playback)));
    }

    private IEnumerator<float> FadeIn(SpeakerApi.Playback playback)
    {
        for (float elapsed = 0f; elapsed < MusicFadeIn; elapsed += Time.deltaTime)
        {
            if (!playback.IsValid || triggered) yield break;

            playback.SetVolume(Mathf.Clamp01(elapsed / MusicFadeIn));

            yield return 0f;
        }

        if (playback.IsValid)
            playback.SetVolume(1f);
    }

    private void StopMusic(Player player)
    {
        if (music.TryGetValue(player, out SpeakerApi.Playback playback))
            playback.Stop();

        music.Remove(player);
        SpeakerApi.TryDestroy(MusicSpeaker + player.PlayerId);
    }

    private void Cleanup()
    {
        foreach (CoroutineHandle handle in handles)
            Timing.KillCoroutines(handle);

        handles.Clear();

        foreach (SpeakerApi.Playback playback in music.Values.ToArray())
            playback.Stop();

        music.Clear();
        Waiting.Clear();

        foreach (string speaker in SpeakerApi.GetAudioPlayerNames()
                     .Where(name => name.StartsWith(MusicSpeaker) || name.StartsWith(OutroSpeaker))
                     .ToArray())
        {
            SpeakerApi.TryDestroy(speaker);
        }

        triggered = false;
        lockedForPlayerCount = false;
    }
}
