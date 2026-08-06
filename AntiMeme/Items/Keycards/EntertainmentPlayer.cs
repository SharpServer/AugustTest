using System;
using System.Collections.Generic;
using AntiMeme.Audio;
using AntiMeme.Items.Bases;
using Interactables.Interobjects.DoorUtils;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using Sliced.API.Features;
using UnityEngine;

using ExiledPlayer = Exiled.API.Features.Player;
using AntiMeme.Snake;

namespace AntiMeme.Items.Keycards;

/// <summary>Chaos Keycard の Snake 画面で映像やミニゲームを起動する端末です。</summary>
public sealed class EntertainmentPlayer : CustomKeycard
{
    private enum Mode { BadApple, Doom, PacMan, Tetris }

    private const string BadApple = "https://www.nicovideo.jp/watch/sm8628149";
    private static bool hooked;
    private Mode mode;
    private ISnakeGameSession session;

    public EntertainmentPlayer() => Hook();

    public override string Name => "Chaos Entertainment System [TEST]";
    public override string Description => "Bad Apple、DOOM、PAC-MAN、TETRIS を楽しめる端末。投げる操作でモード切替、カードを覗いて起動。";
    public override ItemType BaseType => ItemType.KeycardChaosInsurgency;
    protected override KeycardLevels Levels => default;
    protected override string Label => "ENTERTAINMENT";
    protected override string HolderName => "Chaos Insurgency";

    protected override void OnReleased()
    {
        Stop();
        base.OnReleased();
    }

    protected override void OnDropping(PlayerDroppingItemEventArgs ev)
    {
        if (!ev.Throw || ev.Item.Serial != Serial) return;

        ev.IsAllowed = false;
        Stop();
        mode = (Mode)(((int)mode + 1) % 4);
        ev.Player.SendHint($"<size=22>ENTERTAINMENT MODE\n{ModeName(mode)}\nカードを覗いて起動</size>", 3.5f);
    }

    private void Start(PlayerInspectingKeycardEventArgs ev)
    {
        if (ev.KeycardItem.Serial != Serial || ExiledPlayer.Get(ev.Player.ReferenceHub) is not { } player || session is not null)
            return;

        ev.IsAllowed = false;
        Stop();

        try
        {
            switch (mode)
            {
                case Mode.BadApple:
                    SnakeMediaApi.PlayPixelMedia(player, BadApple, new SnakeMediaOptions
                    {
                        Image = new SnakeImageOptions
                        {
                            FramesPerSecond = 15f,
                            MaxFrames = 4000,
                            Invert = true,
                            RenderStyle = SnakeImageRenderStyle.AbstractSilhouette,
                            AbstractionLevel = 2,
                            RestoreSnakeOnStop = true,
                        },
                        IsSpatial = true,
                        MaxDistance = 12f,
                        MinDistance = 1f,
                        Volume = 1f,
                        AudioPlayerName = $"Entertainment_BadApple_{Serial}",
                    });
                    break;
                case Mode.Doom:
                    session = new SnakeDoomEngine(player, Serial);
                    session.Start();
                    break;
                case Mode.PacMan:
                    session = new SnakeGridGameEngine(player, Serial, SnakeGridGameMode.PacMan);
                    session.Start();
                    break;
                case Mode.Tetris:
                    session = new SnakeGridGameEngine(player, Serial, SnakeGridGameMode.Tetris);
                    session.Start();
                    break;
            }

            ev.Player.SendHint($"<size=22>{ModeName(mode)} を開始しました。</size>", 4f);
        }
        catch (Exception exception)
        {
            Stop();
            ev.Player.SendHint($"起動に失敗しました: {exception.Message}", 4f);
        }
    }

    private void Stop()
    {
        SnakeMediaApi.Stop(Serial);
        if (session is not null)
        {
            session.Stop(true);
            session.Dispose();
            session = null;
        }
    }

    private static void Hook()
    {
        if (hooked) return;

        hooked = true;
        PlayerEvents.InspectingKeycard += HandleInspecting;
        ItemRuntime.Register(() =>
        {
            PlayerEvents.InspectingKeycard -= HandleInspecting;
            hooked = false;
        });
    }

    private static void HandleInspecting(PlayerInspectingKeycardEventArgs ev) =>
        (Of(ev.KeycardItem.Serial) as EntertainmentPlayer)?.Start(ev);

    private static string ModeName(Mode value) => value switch
    {
        Mode.BadApple => "Bad Apple!!",
        Mode.Doom => "DOOM",
        Mode.PacMan => "PAC-MAN",
        Mode.Tetris => "TETRIS",
        _ => value.ToString(),
    };
}
