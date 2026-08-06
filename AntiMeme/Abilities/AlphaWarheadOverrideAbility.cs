using LabApi.Features.Wrappers;
using Sliced.API.Features;

// ゲーム本体にも Cassie 型があるので別名で指す。LabApi 側は Cassie が旧形式で、Announcer が現行。
using LabCassie = LabApi.Features.Wrappers.Announcer;

namespace AntiMeme.Abilities;

/// <summary>
/// 管制を無視して Alpha Warhead を強制起動します。起動後は止められません。
/// </summary>
public sealed class AlphaWarheadOverrideAbility : AbilityBase
{
    private const float Countdown = 90f;

    private const string Announcement =
        "$PITCH_0.2 .g4 .g4 BY $PITCH_0.8 BY ORDER OF FACILITY SYSTEM CONTROL, " +
        "ALPHA WARHEAD FORCE OPERATION ACTIVATED. DETONATE IN T MINUS 90 SECONDS.";

    private const string Subtitles =
        "<color=red><b>BY ORDER OF FACILITY SYSTEM CONTROL, ALPHA WARHEAD FORCE OPERATION ACTIVATED. " +
        "DETONATE IN T-90 SECONDS.</b></color>";

    /// <inheritdoc/>
    public override string Name => "ALPHA WARHEAD OVERRIDE";

    /// <inheritdoc/>
    public override string Description => "管制を無視して弾頭を強制起動する。取り消しはできない。";

    /// <inheritdoc/>
    public override float Cooldown => 999f;

    /// <inheritdoc/>
    public override int MaxUses => 1;

    /// <inheritdoc/>
    protected override bool CanUse(out string failureReason)
    {
        if (!base.CanUse(out failureReason)) return false;

        if (Warhead.IsDetonated || Warhead.IsDetonationInProgress)
        {
            failureReason = "現在はALPHA WARHEAD OVERRIDEを実行できません。";

            return false;
        }

        return true;
    }

    /// <inheritdoc/>
    protected override void OnUsed()
    {
        LabCassie.Message(Announcement, Subtitles);

        // カウントダウンは起動した本人が居なくなっても走り切る必要があるので、
        // PlayerScope ではなくラウンドのスコープに載せる。
        RoundScope.Current.Delay(Countdown, Warhead.Detonate);
    }
}
