using System.Linq;
using AntiMeme.Maps;
using AntiMeme.Maps.Objects;
using AntiMeme.Roles.ChaosInsurgency;
using AntiMeme.Teams.Factions;
using CustomPlayerEffects;
using LabApi.Features.Wrappers;
using Sliced.API.Features;
using UnityEngine;

namespace AntiMeme.GameModes.Modes;

/// <summary>
/// カオス・インサージェンシーの大規模侵攻。DELTA COMMAND が CASSIE を掌握し、
/// 施設を消灯させたうえで最後に地上攻撃を仕掛けます。
/// </summary>
public sealed class ChaosInsurgencyRaid : RaidMode
{
    private const float WorkTime = 1000f;
    private const string DeltaColor = "#228b22";

    private static readonly Color Blackout = new Color(55f / 255f, 55f / 255f, 55f / 255f);
    private static readonly Color DoomColor = new Color32(255, 0, 0, 255);

    public override string Name => "Chaos Insurgency Raid";

    public override string Description => "カオス・インサージェンシーが施設を制圧する。";

    public override int MinimumPlayers => 5;

    public override int Weight => 10;

    public override bool AllowsWarhead => false;

    /// <summary>掌握の一環としてテスラゲートを止めます。</summary>
    public override bool AllowsTesla => false;

    protected override void OnStarted()
    {
        Delay(2f, () => ConvertRatio<ChaosCommando>(LivingScps(), 1f / 3f));

        Delay(10f, () => Say(
            "$pitch_1.02 Danger Detected Unknown Forces in Gate A . Please Check $pitch_.2 .g4 .g1 .g2",
            "警告、不明な部隊がGate Aで検出されました。確認を"));

        Delay(22f, () => Say(
            "$pitch_.8 Successfully terminated Foundations Cassie System and putted New Insurgencys Cassie System . " +
            "Cassie is now under delta command",
            "<color=#00b7eb>財団のCassieシステム</color>の<color=red>終了</color>に成功。" +
            $"新たな<color={DeltaColor}>インサージェンシーのCassieシステム</color>の導入も成功。<split> " +
            $"Cassieは今や<b><color={DeltaColor}>DELTA COMMAND</color></b>の手中にある。",
            noise: false));

        Delay(67f, () =>
        {
            Say(
                "$pitch_.8 First Order of Delta Command . Turn off all facilitys . Accepted .",
                $"<b><color={DeltaColor}>DELTA COMMAND</color></b>の最初の指令：全施設の消灯 ...承認",
                noise: false);

            // 消灯で SCP だけが一方的に有利にならないよう、暗視も同時に配る。
            TintRooms(Blackout);
            GiveNightVisionToScps();
        });

        Delay(75f, () => Say(
            "$pitch_.8 Next Order . Turn off Tesla Gates . Accepted .",
            "次の指令：テスラゲートの無効化 ...承認",
            noise: false));

        Delay(83f, () => Say(
            "$pitch_.8 All Insurgency Agents . Work Time .",
            "インサージェンシーのエージェント達よ、働く時間だ。",
            noise: false));

        Delay(83f + WorkTime, Judge);
    }

    /// <summary>作業時間の終わりに成否を決めます。</summary>
    private void Judge()
    {
        if (!Living.Any(player => CustomTeam.Of(player) is ChaosInsurgencyTeam))
        {
            Say(
                "$pitch_.2 .g3 $pitch_.7 .g2 $pitch_.4 .g4 .g5 .g5 $pitch_1 .g1 .g2 .g3 Attention . All personnel . " +
                "the Foundation Forces Successfully Terminated All Chaos Insurgency Forces . " +
                "All System now backed to the Foundation . All Delta Command Orders Now Terminated . " +
                "Please back to normal Containment Breach Security Mode",
                "全職員に報告します。財団の部隊は全カオス・インサージェンシー勢力の排除に成功しました。" +
                "全てのDELTA COMMANDの指令は正常に終了。全職員は収容違反の対応モデルに復帰してください。");

            return;
        }

        Say(
            "$pitch_.8 All Insurgency Agents Tasks completed . Last Order . . $pitch_.75 Destroy the Facility . " +
            "$pitch_.4 .g1 $pitch_.26 .g5 .g6 .g4 $pitch_2 .g1 $pitch_.75 Good by all anomalys and foundation personnels .",
            "全インサージェンシーエージェントの任務完了を確認。最後の指令を下す：<b><color=red>施設を破壊せよ</color></b>");

        Delay(15f, () => Say(
            "$pitch_.2 .g4 .g4 $pitch_1 $pitch_.75 BY ORDER OF DELTA COMMAND . " +
            "THE DEAD MANS SEQUENCE AND SURFACE ATTACK PROTOCOL ACTIVATED . DETONATION IN TMINUS 145 SECONDS . ",
            $"BY ORDER OF <color={DeltaColor}><b>DELTA COMMAND</b></color>. " +
            "THE DEAD MANS SEQUENCE AND SURFACE ATTACK PROTOCOL ACTIVATED. DETONATION IN T-145 SECONDS. "));

        Delay(25f, Doomsday);
    }

    /// <summary>地上攻撃です。脱出の猶予を与えてから締め切ります。</summary>
    private void Doomsday()
    {
        MapAudio.Play("cir.ogg", "ChaosRaid", Vector3.zero, maxDistance: 999f);

        new ChaosNuke().Create();

        TintRooms(DoomColor);
        OpenAndSealDoors();

        Delay(3f, () => Say(
            "This is O5 Message from the Site 1, For All personnel, Please escape from the facility .",
            "[Site-01, O5からの通信]全職員へ通達：救助部隊を派遣しました。" +
            "直ちに<color=green>脱出口</color>から<color=yellow>脱出</color>してください。",
            noise: false));

        // 締め切り。ここから先は逃がさない。
        Delay(143f, CloseAllDoors);

        Delay(148f, () =>
            ExplodeLiving("ALPHA WARHEADに爆破された", "SURFACE ATTACK PROTOCOL に爆破された"));
    }

    private static void CloseAllDoors()
    {
        foreach (Door door in Door.List)
        {
            if (door is not { IsDestroyed: false }) continue;

            door.IsOpened = false;
            door.IsLocked = true;
        }
    }

    /// <summary>消灯で SCP だけが得をしないよう、人間側と条件を揃えます。</summary>
    private static void GiveNightVisionToScps()
    {
        foreach (Player player in Living.Where(player => player.IsSCP))
            player.EnableEffect<NightVision>(255);
    }
}
