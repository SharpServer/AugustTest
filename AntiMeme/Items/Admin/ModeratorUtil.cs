using AntiMeme.Items.Bases;
using System;
using AntiMeme.Items;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using PlayerRoles;
using PlayerStatsSystem;
using Sliced.API.Features;
using UnityEngine;
using AntiMeme.Roles.Moderators;
using Sliced.API.Features.Attributes;

namespace AntiMeme.Items.Admin;

/// <summary>管理者向けの対象操作ツールです。投擲で機能、Inspect で選択肢を切り替えます。</summary>
[LegacyName("ModUtil")]
public sealed class ModeratorUtil : CustomWeapon
{
    public enum UtilityMode { Inspect, Warn, Kick, Ban, Kill, Teleport, Inventory, Restrain, Voice, Role, Privilege, Heal }

    private static readonly string[] BanNames = ["10分", "1時間", "6時間", "12時間", "1日", "3日", "7日", "14日", "28日", "90日", "180日", "1年", "3年", "無期限"];
    private static bool hooked;
    private UtilityMode mode;
    private int option;
    private float nextAction;

    public ModeratorUtil() => Hook();

    public override ItemType BaseType => ItemType.GunCOM18;
    public override string Name => "Moderator Util";
    public override string Description => "管理者用モデレーションガン。投擲で機能切替、Inspect で詳細指定、対象を撃って実行。";
    protected override float Damage => 0f;
    protected override int MagazineSize => 18;
    protected override Vector3 Scale => Vector3.zero;
    protected override bool AllowAttachmentChanges => false;

    private static bool CanUse(Player player) => player is { IsDestroyed: false } &&
        (player.RemoteAdminAccess || CustomRole.Is<ModeratorRole>(player) || CustomRole.Is<HideAdmin>(player));

    private bool HeldBy(Player player) => CanUse(player) && player.CurrentItem is { } item && Of(item.Serial) is ModeratorUtil util && ReferenceEquals(util, this);

    protected override void OnDropping(PlayerDroppingItemEventArgs ev)
    {
        if (!ev.Throw || ev.Item.Serial != Serial) return;

        ev.IsAllowed = false;
        mode = (UtilityMode)(((int)mode + 1) % Enum.GetValues(typeof(UtilityMode)).Length);
        option = 0;
        ev.Player.SendHint(CurrentHint(), 3f);
    }

    private void Select(PlayerInspectingItemEventArgs ev)
    {
        if (ev.Item.Serial != Serial || !CanUse(ev.Player)) return;

        ev.IsAllowed = false;
        option = (option + 1) % OptionCount(mode);
        ev.Player.SendHint(CurrentHint(), 3f);
    }

    protected override void OnHit(PlayerHurtingEventArgs ev)
    {
        if (ev.Attacker is not { } actor || actor.CurrentItem is not { } item || Of(item.Serial) is not ModeratorUtil util || !ReferenceEquals(util, this))
            return;

        ev.IsAllowed = false;
        if (ev.DamageHandler is StandardDamageHandler handler)
            handler.Damage = 0f;
        if (!CanUse(actor) || ev.Player is not { } target || Time.time < nextAction) return;

        nextAction = Time.time + .6f;
        Execute(actor, target);
    }

    private void Execute(Player actor, Player target)
    {
        string result = mode switch
        {
            UtilityMode.Inspect => $"{target.Nickname} | ID:{target.PlayerId} | Role:{target.Role} | HP:{target.Health:0}/{target.MaxHealth:0}",
            UtilityMode.Warn => Warn(target),
            UtilityMode.Kick => Kick(actor, target),
            UtilityMode.Ban => Ban(actor, target),
            UtilityMode.Kill => Kill(target),
            UtilityMode.Teleport => Teleport(actor, target),
            UtilityMode.Inventory => Inventory(target),
            UtilityMode.Restrain => Restrain(actor, target),
            UtilityMode.Voice => Voice(target),
            UtilityMode.Role => ChangeRole(target),
            UtilityMode.Privilege => Privilege(target),
            UtilityMode.Heal => Heal(target),
            _ => string.Empty,
        };
        actor.SendHint($"<size=21><color=#ff8bd6>[Moderator Util]</color> {mode} / {OptionName()}\n{result}</size>", 4f);
    }

    private string Warn(Player target)
    {
        target.SendBroadcast("<color=red><b>Moderator Warning</b></color>\n管理者の指示に従ってください。", 6);
        return $"{target.Nickname} に警告を送信しました。";
    }

    private static string Kick(Player actor, Player target)
    {
        target.Kick(actor, "Moderator action");
        return $"{target.Nickname} を Kick しました。";
    }

    private static string Ban(Player actor, Player target)
    {
        if (target.Ban(actor, "Moderator action", BanDurationSeconds(0)))
            return $"{target.Nickname} を Ban しました。";
        return $"{target.Nickname} の Ban に失敗しました。";
    }

    private static long BanDurationSeconds(int index) => index >= BanNames.Length - 1 ? 0L : (long)new[] { 600, 3600, 21600, 43200, 86400, 259200, 604800, 1209600, 2419200, 7776000, 15552000, 31536000, 94608000 }[Mathf.Clamp(index, 0, 12)];

    private static string Kill(Player target)
    {
        target.Kill("Moderator action");
        return $"{target.Nickname} を Kill しました。";
    }

    private string Teleport(Player actor, Player target)
    {
        switch (option)
        {
            case 0: target.Position = actor.Position + actor.Camera.forward * 1.5f; return "Bringしました。";
            case 1: actor.Position = target.Position + Vector3.up * .2f; return "Gotoしました。";
            default:
                Vector3 position = actor.Position;
                actor.Position = target.Position + Vector3.up * .2f;
                target.Position = position + Vector3.up * .2f;
                return "位置を交換しました。";
        }
    }

    private string Inventory(Player target)
    {
        switch (option)
        {
            case 0: target.ClearInventory(); return "インベントリを消去しました。";
            case 1: target.DropEverything(); return "全アイテムをドロップさせました。";
            case 2: target.AddItem(ItemType.Medkit); return "Medkitを付与しました。";
            default: target.AddItem(ItemType.Radio); return "Radioを付与しました。";
        }
    }

    private static string Restrain(Player actor, Player target)
    {
        target.IsDisarmed = !target.IsDisarmed;
        return target.IsDisarmed ? "拘束しました。" : "拘束を解除しました。";
    }

    private static string Voice(Player target)
    {
        if (target.IsMuted) target.Unmute(true); else target.Mute();
        return target.IsMuted ? "通常VCをミュートしました。" : "通常VCミュートを解除しました。";
    }

    private string ChangeRole(Player target)
    {
        RoleTypeId role = option switch
        {
            0 => RoleTypeId.Spectator,
            1 => RoleTypeId.Tutorial,
            2 => RoleTypeId.ClassD,
            3 => RoleTypeId.Scientist,
            4 => RoleTypeId.FacilityGuard,
            5 => RoleTypeId.NtfPrivate,
            _ => RoleTypeId.ChaosRifleman,
        };
        target.SetRole(role);
        return $"{target.Nickname} を {role} に変更しました。";
    }

    private string Privilege(Player target)
    {
        switch (option)
        {
            case 0: target.IsGodModeEnabled = !target.IsGodModeEnabled; return $"GodMode={target.IsGodModeEnabled}";
            case 1: target.IsBypassEnabled = !target.IsBypassEnabled; return $"Bypass={target.IsBypassEnabled}";
            default: target.IsNoclipEnabled = !target.IsNoclipEnabled; return $"Noclip={target.IsNoclipEnabled}";
        }
    }

    private string Heal(Player target)
    {
        if (option == 0) target.Health = target.MaxHealth;
        else if (option == 1) target.Health = Mathf.Min(target.MaxHealth, target.Health + 50f);
        else target.MaxHealth = option == 2 ? 100f : 999f;
        if (option >= 2) target.Health = target.MaxHealth;
        return "HPを調整しました。";
    }

    private string CurrentHint() => $"<size=21>Moderator Util\nT: {mode} / I: {OptionName()}</size>";
    private string OptionName() => mode == UtilityMode.Ban ? BanNames[Mathf.Clamp(option, 0, BanNames.Length - 1)] : option.ToString();

    private static int OptionCount(UtilityMode value) => value switch
    {
        UtilityMode.Inspect or UtilityMode.Teleport or UtilityMode.Privilege => 3,
        UtilityMode.Warn or UtilityMode.Kick or UtilityMode.Inventory or UtilityMode.Voice or UtilityMode.Role or UtilityMode.Heal => 4,
        UtilityMode.Ban => BanNames.Length,
        _ => 1,
    };

    private static void Hook()
    {
        if (hooked) return;

        hooked = true;
        PlayerEvents.InspectingItem += OnInspecting;
        ItemRuntime.Register(() =>
        {
            PlayerEvents.InspectingItem -= OnInspecting;
            hooked = false;
        });
    }

    private static void OnInspecting(PlayerInspectingItemEventArgs ev) =>
        (Of(ev.Item.Serial) as ModeratorUtil)?.Select(ev);
}
