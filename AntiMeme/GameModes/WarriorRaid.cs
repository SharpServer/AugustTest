using System;
using System.Linq;
using AntiMeme.Maps;
using AntiMeme.Maps.Objects;
using AntiMeme.Teams.Factions;
using LabApi.Features.Wrappers;
using Sliced.API.Features;
using UnityEngine;

namespace AntiMeme.GameModes;

/// <summary>
/// 季節の戦士による襲撃の共通進行です。
///
/// <para>
/// 筋書きは 4 段。<b>宣言 → 掌握 → 作業時間 → 成否判定</b>。
/// 作業時間の終わりに戦士が 1 人でも生きていれば施設が落ち、
/// 全滅していれば財団の勝ちとして平常運転へ戻ります。
/// </para>
/// </summary>
/// <remarks>
/// 雪とお菓子で違うのは<b>呼称・色・落ちてくる物・とどめの文言</b>だけなので、
/// 進行そのものはここに 1 本だけ置きます。
/// </remarks>
public abstract class WarriorRaid : RaidMode
{
    private const float WorkTime = 1000f;
    private const float DetonationDelay = 145f;

    /// <summary>この襲撃で立てる戦士の役職です。</summary>
    protected abstract Type WarriorRole { get; }

    /// <summary>落としてくる物です。</summary>
    protected abstract ObjectPrefab CreateProjectile();

    /// <summary>陣営色です。放送の装飾に使います。</summary>
    protected abstract string Color { get; }

    /// <summary>指令者の呼称です。</summary>
    protected abstract string Emperor { get; }

    /// <summary>掌握後に施設を染める色です。</summary>
    protected abstract Color LightColor { get; }

    /// <summary>とどめの段階で施設を染める色です。</summary>
    protected abstract Color DoomColor { get; }

    /// <summary>最後の指令の文言です。</summary>
    protected abstract string FinalOrder { get; }

    /// <summary>攻撃プロトコルの名前です。</summary>
    protected abstract string AttackProtocol { get; }

    /// <summary>カウントダウン放送の締めです (読み上げ)。</summary>
    protected abstract string FarewellCassie { get; }

    /// <summary>カウントダウン放送の締めです (字幕)。</summary>
    protected abstract string FarewellSubtitle { get; }

    public override int MinimumPlayers => 5;

    public override int Weight => 10;

    public override bool AllowsWarhead => false;

    /// <summary>掌握の一環としてテスラゲートを止めます。</summary>
    public override bool AllowsTesla => false;

    /// <summary>掌握された施設では除染も走らせません。</summary>
    public override bool AllowsDecontamination => false;

    /// <inheritdoc/>
    protected override void OnStarted()
    {
        Delay(2f, () => ConvertRatio(WarriorRole, LivingScps(), 1f / 3f));

        Delay(10f, () => Say(
            "$pitch_1.02 Danger Detected Unknown Organism in Gate A . Please Check $pitch_.2 .g4 .g1 .g2",
            "警告、不明な生命体がGate Aで検出されました。確認を"));

        Delay(22f, () => Say(
            "$pitch_.8 Successfully terminated Foundations Cassie System and putted New Division Cassie System . " +
            "Cassie is now under us",
            $"<color=#00b7eb>財団のCassieシステム</color>の<color=red>終了</color>に成功。" +
            $"新たな<color={Color}>{Emperor}たちのCassieシステム</color>の導入も成功。<split> " +
            $"Cassieは今や<b><color={Color}>{Emperor}</color></b>の手中にある。",
            noise: false));

        Delay(67f, () =>
        {
            Say(
                "$pitch_.8 First Order . Light up all facility . Accepted .",
                $"<b><color={Color}>{Emperor}</color></b>の最初の指令：全施設のライトアップ ...承認",
                noise: false);

            LightUp(LightColor);
        });

        Delay(75f, () => Say(
            "$pitch_.8 Next Order . Turn off Tesla Gates . Accepted .",
            "次の指令：テスラゲートの無効化 ...承認",
            noise: false));

        Delay(83f, () => Say(
            "$pitch_.8 All Division . Work Time .",
            "戦士達よ、働く時間だ。",
            noise: false));

        Delay(83f + WorkTime, Judge);
    }

    /// <summary>作業時間の終わりに成否を決めます。</summary>
    private void Judge()
    {
        if (!Living.Any(player => CustomTeam.Of(player) is WarriorsTeam))
        {
            Say(
                "$pitch_.2 .g3 $pitch_.7 .g2 $pitch_.4 .g4 .g5 .g5 $pitch_1 .g1 .g2 .g3 Attention . All personnel . " +
                "the Foundation Forces Successfully Terminated All Forces . All System now backed to the Foundation . " +
                "All Division Command Orders Now Terminated . Please back to normal Containment Breach Security Mode",
                $"全職員に報告します。財団の部隊は全{Emperor}達勢力の排除に成功しました。" +
                "全てのDIVISION COMMANDの指令は正常に終了。全職員は収容違反の対応モデルに復帰してください。");

            return;
        }

        Say(
            "$pitch_.8 All Division Agents Tasks completed . Last Order . . $pitch_.75 Destroy the Facility . " +
            "$pitch_.4 .g1 $pitch_.26 .g5 .g6 .g4 $pitch_2 .g1 $pitch_.75 Good by all anomalys and foundation personnels .",
            $"全戦士達の任務完了を確認。最後の指令を下す：<b><color={Color}>{FinalOrder}</color></b>");

        Delay(15f, () => Say(
            "$pitch_.2 .g4 .g4 $pitch_1 $pitch_.75 BY ORDER OF DIVISION COMMAND . " +
            "THE DEAD MANS SEQUENCE AND ATTACK PROTOCOL ACTIVATED . DETONATION IN TMINUS 145 SECONDS . " +
            FarewellCassie,
            $"BY ORDER OF <color={Color}><b>DIVISION COMMAND</b></color>. " +
            $"THE DEAD MANS SEQUENCE AND {AttackProtocol} ATTACK PROTOCOL ACTIVATED. DETONATION IN T-145 SECONDS. " +
            $"<color=red><b>{FarewellSubtitle}</b></color>"));

        Delay(25f, Doom);
    }

    /// <summary>落着までの演出です。</summary>
    private void Doom()
    {
        MapAudio.Play("cir.ogg", "WarriorRaid", Vector3.zero, maxDistance: 999f);

        CreateProjectile().Create();

        TintRooms(DoomColor);
        OpenAndSealDoors();

        Delay(DetonationDelay, () =>
            ExplodeLiving("ALPHA WARHEADに爆破された", $"{AttackProtocol} ATTACKに爆破された"));
    }
}
