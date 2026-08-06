using System;
using System.Linq;
using AntiMeme.Maps.Objects;
using LabApi.Features.Wrappers;
using PlayerRoles;

using ExiledPlayer = Exiled.API.Features.Player;
using ExiledScp079 = Exiled.API.Features.Roles.Scp079Role;

namespace AntiMeme.Maps.Core;

/// <summary>
/// EZ の遠隔扉制御レバーです。下げると SCP-079 が 20 秒間信号を失い、レバーは自動で戻ります。
/// </summary>
/// <remarks>
/// 旧実装は <c>LeverHandler</c> のタグ別ハンドラ表に登録されていましたが、
/// 新実装ではその表に誰も登録しておらず、レバーを動かしても何も起きませんでした。
/// </remarks>
public sealed class RemoteDoorControlLever : MapFeature
{
    /// <summary>このレバーに付いている名札です。</summary>
    public const string LeverTag = "EZ_RemoteDoorControl";

    /// <summary>信号を失っている時間です。</summary>
    private const float BlackoutSeconds = 20f;

    /// <inheritdoc/>
    public override void RegisterEvents() => InteractableLever.Toggled += OnLeverToggled;

    /// <inheritdoc/>
    public override void UnregisterEvents() => InteractableLever.Toggled -= OnLeverToggled;

    private void OnLeverToggled(Player player, InteractableLever lever, bool isOn)
    {
        // 効くのは「下げた」ときだけ。戻すのはこちらの仕事。
        if (isOn || lever is null || !string.Equals(lever.Tag, LeverTag, StringComparison.OrdinalIgnoreCase)) return;

        lever.CanInteract = false;

        foreach (Player target in Player.ReadyList.Where(candidate =>
                     candidate is { IsDestroyed: false } && candidate.Role is RoleTypeId.Scp079))
        {
            target.SendHint(
                "<color=red>Remote Door ControlがONにされた！</color>\n※20秒後に復帰します",
                BlackoutSeconds);

            if (ExiledPlayer.Get(target.ReferenceHub)?.Role is ExiledScp079 scp079)
                scp079.LoseSignal(BlackoutSeconds);
        }

        Sliced.API.Features.RoundScope.Current.Delay(BlackoutSeconds, () =>
        {
            if (!lever.IsAlive) return;

            lever.CanInteract = true;
            lever.Toggle();
        });
    }
}
