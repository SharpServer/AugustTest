using AntiMeme.Items.Melee;
using AntiMeme.Teams;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Features.Wrappers;
using Sliced.API.Features;
using Sliced.API.Structs;

namespace AntiMeme.Changes;

/// <summary>
/// SCP-1509 で蘇生させた相手を、蘇生させた側の仲間として立たせます。
/// </summary>
/// <remarks>
/// 何を立てるかは陣営自身が <see cref="CustomTeam.Resurrection"/> で名乗ります。
/// 旧実装はここに 9 分岐の switch を持っていましたが、
/// 陣営が増えるたびにこの switch も直すことになるので、宣言側へ寄せました。
/// </remarks>
public sealed class Scp1509Resurrection : EventHandlerBase
{
    /// <inheritdoc/>
    public override void OnPlayerScp1509Resurrecting(PlayerScp1509ResurrectingEventArgs ev)
    {
        if (!ev.IsAllowed || ev.RevivedPlayer is not { IsDestroyed: false } target) return;

        // SCP-148 を握っている間は蘇生の理を捻じ曲げない。
        if (CustomItem.Of(ev.Player.CurrentItem?.Serial ?? 0) is Scp148) return;

        if (CustomTeam.Of(ev.Player) is not { } team) return;

        // 陣営が何も名乗らないなら、蘇生させた本人と同じものを立てる。
        if (team.Resurrection is not { } definition)
        {
            CopyCaster(ev.Player, target);

            return;
        }

        // ゲーム本体の役職設定より後に上書きしたいので 1 tick 遅らせる。
        PlayerScope.Of(target).Delay(0.1f, revived => definition.SpawnFor(revived));
    }

    /// <summary>
    /// 蘇生させた側と同じ役職を複製します。カスタム役職ならそれ、無ければバニラ役職。
    /// </summary>
    private static void CopyCaster(Player caster, Player target)
    {
        PlayerScope.Of(target).Delay(0.1f, revived =>
        {
            if (CustomRole.Of(caster) is { } custom)
            {
                CustomRole.Spawn(custom.GetType(), revived);

                return;
            }

            revived.SetRole(caster.Role, PlayerRoles.RoleChangeReason.None, PlayerRoles.RoleSpawnFlags.None);
        });
    }
}
