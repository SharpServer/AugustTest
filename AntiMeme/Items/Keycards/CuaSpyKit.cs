using System;
using System.Collections.Generic;
using System.Linq;
using AntiMeme.Items.Bases;
using Exiled.API.Extensions;
using Interactables.Interobjects.DoorUtils;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using PlayerRoles;
using Sliced.API.Features;
using UnityEngine;

using ExiledPlayer = Exiled.API.Features.Player;
using AntiMeme.Roles.ChaosInsurgency;
using Sliced.API.Features.Attributes;

namespace AntiMeme.Items.Keycards;

/// <summary>カオス潜入工作員用の外見切替カードです。</summary>
[LegacyName("CUA_SpyKit")]
public sealed class CuaSpyKit : CustomKeycard
{
    private readonly struct Appearance
    {
        public Appearance(RoleTypeId role, string label)
        {
            Role = role;
            Label = label;
        }

        public RoleTypeId Role { get; }
        public string Label { get; }
    }

    private static readonly Appearance[] Appearances =
    [
        new(RoleTypeId.ChaosMarauder, "変装を解除"),
        new(RoleTypeId.ClassD, "Class-D Personnel"),
        new(RoleTypeId.Scientist, "Scientist Personnel"),
    ];

    private static bool hooked;
    private int selected;
    private bool morphed;

    public CuaSpyKit() => Hook();

    public override string Name => "CUA式スパイキット";

    /// <inheritdoc/>
    protected override bool PickupLightEnabled => true;

    /// <inheritdoc/>
    protected override Color PickupLightColor => new Color32(0, 75, 0, 255);
    public override string Description => "カオスの潜入工作員が持つ変装セット。投げる操作で外見を切り替え、カードを覗いて実行する。";
    protected override KeycardLevels Levels => default;
    protected override string Label => "CUA. SpyKit";
    protected override Color32 LabelColor => new Color32(255, 255, 255, 255);
    protected override string HolderName => "Chaos Insurgency";
    protected override Color32 Tint => new Color32(0, 68, 0, 255);
    protected override Color32 PermissionsColor => new Color32(0, 0, 0, 255);
    protected override int Rank => 2;

    private bool CanUse(Player player) => CustomRole.Is<ChaosUndercoverAgent>(player);

    protected override void OnReleased()
    {
        if (Owner is { } owner && morphed)
            Morph(owner, Appearances[0]);

        morphed = false;
    }

    protected override void OnDropping(PlayerDroppingItemEventArgs ev)
    {
        if (Of(ev.Item.Serial) is not CuaSpyKit kit || !ev.Throw || !kit.CanUse(ev.Player)) return;

        ev.IsAllowed = false;
        kit.selected = (kit.selected + 1) % Appearances.Length;
        ev.Player.SendHint($"<size=22>[変装メニュー]\n現在選択中: {kit.AppearancesText()}</size>", 2.5f);
    }

    private void Inspect(PlayerInspectingKeycardEventArgs ev)
    {
        if (Of(ev.KeycardItem.Serial) is not CuaSpyKit kit || !kit.CanUse(ev.Player)) return;

        ev.IsAllowed = false;
        kit.Morph(ev.Player, Appearances[kit.selected]);
    }

    private static void Handcuff(PlayerCuffingEventArgs ev)
    {
        if (ev.Player is { } player && player.CurrentItem is { } item && Of(item.Serial) is CuaSpyKit kit && kit.CanUse(player))
            kit.Morph(player, Appearances[0]);
    }

    private static void OnLeft(PlayerLeftEventArgs ev)
    {
        if (ev.Player is { } player && CustomItem.Tracked.OfType<CuaSpyKit>().FirstOrDefault(kit => kit.Owner == player) is { } kit)
            kit.morphed = false;
    }

    private void Morph(Player player, Appearance appearance)
    {
        if (ExiledPlayer.Get(player.ReferenceHub) is { } exiled)
            exiled.ChangeAppearance(appearance.Role);

        morphed = appearance.Role != RoleTypeId.ChaosMarauder;
        player.CustomInfo = morphed ? appearance.Label : CustomRole.Of(player)?.CustomInfo ?? string.Empty;
        player.SendHint($"<size=23>{appearance.Label} に変装しました。</size>", 2.5f);
    }

    private string AppearancesText() => Appearances[selected].Label;

    private static void Hook()
    {
        if (hooked) return;

        hooked = true;
        PlayerEvents.InspectingKeycard += HandleInspecting;
        PlayerEvents.Cuffing += OnCuffing;
        PlayerEvents.Left += OnLeft;
        ItemRuntime.Register(() =>
        {
            PlayerEvents.InspectingKeycard -= HandleInspecting;
            PlayerEvents.Cuffing -= OnCuffing;
            PlayerEvents.Left -= OnLeft;
            hooked = false;
        });
    }

    private static void HandleInspecting(PlayerInspectingKeycardEventArgs ev) =>
        (Of(ev.KeycardItem.Serial) as CuaSpyKit)?.Inspect(ev);

    private static void OnCuffing(PlayerCuffingEventArgs ev) => Handcuff(ev);
}
