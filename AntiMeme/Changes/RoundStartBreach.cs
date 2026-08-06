using System.Linq;
using AntiMeme.GameModes;
using AntiMeme.GameModes.Modes;
using AntiMeme.Hud;
using LabApi.Features.Enums;
using Interactables.Interobjects.DoorUtils;
using LabApi.Features.Wrappers;
using Sliced.API.Features;

using ExiledDoor = Exiled.API.Features.Doors.Door;
using DoorLockType = Exiled.API.Enums.DoorLockType;

namespace AntiMeme.Changes;

/// <summary>
/// ラウンド開始直後の「収容違反が起きた」演出です。
/// </summary>
/// <remarks>
/// <para>
/// 放送と照明の明滅で収容違反を告げ、少し遅れて SCP-173 のゲートを開け、
/// 代わりに地上へ出るゲート A / B を一定時間封鎖します。
/// 旧実装では <c>EventHandler.OnRoundStarted</c> の中に入れ子の遅延として書かれ、
/// 除外するイベントを型の一覧で持っていました。
/// </para>
/// <para>
/// 出すかどうかはモード自身が <see cref="GameMode.AllowsBreachAnnouncement"/> と
/// <see cref="GameMode.AllowsGateLockdown"/> で名乗ります。ここに型を並べません。
/// </para>
/// </remarks>
public sealed class RoundStartBreach : EventHandlerBase
{
    /// <summary>放送と明滅までの待ち時間です。ゲームモードの起動 (0.75 秒) より後に置きます。</summary>
    private const float AnnounceDelay = 1f;

    /// <summary>ゲートを触るまでの待ち時間です。</summary>
    private const float GateDelay = 6f;

    /// <summary>照明が明滅する時間です。</summary>
    private const float FlickerDuration = 3f;

    /// <summary>ゲート A / B を閉じておく時間です。</summary>
    private const float GateLockSeconds = 120f;

    /// <inheritdoc/>
    public override void OnServerRoundStarted()
    {
        RoundScope.Current.Delay(AnnounceDelay, Announce);
        RoundScope.Current.Delay(GateDelay, Gates);
    }

    private static void Announce()
    {
        if (GameMode.Current is { AllowsBreachAnnouncement: false }) return;

        if (GameMode.Current is OmegaWarhead)
        {
            FacilityAnnouncer.Say(
                "Emergency , emergency , A large containment breach is currently started within the site. " +
                "All personnel must immediately begin evacuation .",
                "緊急、緊急、現在大規模な収容違反がサイト内で発生しています。" +
                "全職員は警備隊の指示に従い、避難を開始してください。");
        }
        else
        {
            FacilityAnnouncer.Say(
                "Attention, All personnel . Detected containment breach is currently started within the site. " +
                "All personnel must immediately begin evacuation .",
                "全職員へ通達。収容違反の発生を確認しました。" +
                "全職員は警備隊の指示に従い、避難を開始してください。");
        }

        foreach (Room room in Room.List)
            room.LightController?.FlickerLights(FlickerDuration);
    }

    private static void Gates()
    {
        // SCP-173 は自分で出てくる。開けておかないと収容室に閉じ込められる。
        foreach (Door door in Door.List.Where(door => door.DoorName is DoorName.Lcz173Gate))
        {
            door.Lock(DoorLockReason.AdminCommand, false);
            door.IsOpened = true;
        }

        if (GameMode.Current is { AllowsGateLockdown: false }) return;

        // 開幕からの地上直行を止める。時間指定の施錠は EXILED 側にしかない。
        foreach (ExiledDoor door in ExiledDoor.List.Where(door => door.Type is Exiled.API.Enums.DoorType.GateA or Exiled.API.Enums.DoorType.GateB))
            door.Lock(GateLockSeconds, DoorLockType.AdminCommand);
    }
}
