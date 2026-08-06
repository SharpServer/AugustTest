using System.Collections.Generic;
using System.Linq;
using AntiMeme.Effects;
using AntiMeme.Roles.Warriors;
using LabApi.Features.Wrappers;
using Sliced.API.Features;
using UnityEngine;

using ExiledRoom = Exiled.API.Features.Room;

namespace AntiMeme.GameModes.Modes;

/// <summary>
/// 水の戦士の襲撃。施設が下から順に沈んでいき、水位より下にいる者は溺れます。
/// </summary>
/// <remarks>
/// 水位は<b>絶対高度 1 つ</b>で表します。旧実装は水そのものを巨大な Primitive として
/// 生成し、その体積にプレイヤーが入っているかで判定していましたが、
/// 施設全体が水没していく演出なので「今どこまで浸かったか」の 1 値で足ります。
/// </remarks>
public sealed class WaterWarriorsAttack : RaidMode
{
    /// <summary>完全に沈むまでの秒数です。</summary>
    private const float FloodDuration = 198f;

    /// <summary>沈み切ったときの水面の高さです。地上より上まで来ます。</summary>
    private const float FinalWaterLevel = 325f;

    /// <summary>水位判定に使う、足元からの高さです。頭が浸かったら溺れます。</summary>
    private const float ProbeHeight = 0.9f;

    /// <summary>この深さまで潜ると部屋の色が振り切ります。</summary>
    private const float FullTintDepth = 35f;

    private const float TickInterval = 1f;

    private static readonly Color ShallowColor = Color.cyan;
    private static readonly Color DeepColor = new Color(0f, 0.2f, 0.45f);

    private float startTime;
    private float bottomLevel;

    public override string Name => "Water Warriors Raid";

    /// <inheritdoc/>
    public override bool AllowsGateLockdown => false;

    public override string Description => "水の戦士が施設を洪水に沈める。";

    public override int MinimumPlayers => 5;

    public override int Weight => 10;

    /// <inheritdoc/>
    protected override IReadOnlyList<Season> Seasons => [Season.Summer];

    public override bool AllowsWarhead => false;

    /// <summary>いま水面がある高さです。</summary>
    private float WaterLevel =>
        Mathf.Lerp(bottomLevel, FinalWaterLevel, Mathf.Clamp01((Time.time - startTime) / FloodDuration));

    protected override void OnStarted()
    {
        startTime = Time.time;

        // 施設の最下層から水が上がってくる。マップごとに高さが違うので実測する。
        bottomLevel = ExiledRoom.List.Count == 0 ? 0f : ExiledRoom.List.Min(room => room.Position.y);

        Delay(2f, () => ConvertRatio<WaterWarrior>(LivingScps(), 1f / 3f));

        Say("Unknown Anomaly found in Surface Gate A", "全職員に通達。不明な物体が地上ゲートAで確認されました。");

        Delay(10f, () => Say(
            "Facility status is dangerous. Water Warriors detected",
            "施設状態は危険です。水の戦士たちを排除してください。"));

        Loop(TickInterval, FloodTick);
    }

    private void FloodTick()
    {
        float level = WaterLevel;

        TintByDepth(level);
        DrownSubmerged(level);
    }

    /// <summary>部屋ごとに、水面からどれだけ潜っているかで色を変えます。</summary>
    private static void TintByDepth(float level)
    {
        foreach (ExiledRoom room in ExiledRoom.List)
        {
            float depth = level - room.Position.y;

            if (depth <= 0f) continue;

            room.AreLightsOff = false;
            room.Color = Color.Lerp(ShallowColor, DeepColor, Mathf.Clamp01(depth / FullTintDepth));
        }
    }

    /// <summary>
    /// 水面より下にいる者を溺れさせます。水の戦士は自分たちが起こした洪水では溺れません。
    /// </summary>
    private static void DrownSubmerged(float level)
    {
        foreach (Player player in Living)
        {
            if (CustomRole.Is<WaterWarrior>(player)) continue;
            if (player.Position.y + ProbeHeight > level) continue;
            if (player.HasEffect<FloodDrowning>()) continue;

            player.EnableEffect<FloodDrowning>(FloodDrowning.DefaultIntensity, FloodDrowning.DefaultDuration);
        }
    }
}
