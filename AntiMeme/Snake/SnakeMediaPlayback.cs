using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CentralAuth;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using HarmonyLib;
using InventorySystem.Items.Keycards;
using InventorySystem.Items.Keycards.Snake;
using MEC;
using Mirror;
using SNAPI.Events.EventArgs;
using SNAPI.Events.Handlers;
using UnityEngine;

using ExiledPlayer = Exiled.API.Features.Player;
using AntiMeme.Audio;

namespace AntiMeme.Snake;

public sealed class SnakeMediaPlayback : IDisposable
{
    private readonly ushort serial;
    private SnakeImagePlayback image;
    private SpeakerApi.Playback audio;
    private bool stopped;
    internal SnakeMediaPlayback(ushort serial) { this.serial = serial; }
    public ushort Serial => serial;
    public bool IsLoading { get; internal set; }
    public bool IsPlaying => !stopped && image?.IsPlaying == true;
    public Exception Error { get; internal set; }
    internal void Attach(SnakeImagePlayback imagePlayback, SpeakerApi.Playback audioPlayback) { image = imagePlayback; audio = audioPlayback; IsLoading = false; }
    public void Stop() { if (stopped) return; stopped = true; image?.Stop(); if (audio.IsValid) SpeakerApi.Stop(audio); SnakeMediaApi.Remove(this); }
    public void Dispose() => Stop();
}
