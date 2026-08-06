using LabApi.Features.Wrappers;
using Utils;

namespace AntiMeme.Items.Scp914;

/// <summary>
/// 「入れた本人ごと吹き飛ぶ」当たり枠の共通処理です。
///
/// 旧実装は HE グレネードのピックアップを 5 個生成してその場で <c>Explode()</c> していました。
/// 実体を作る意味は無かったので、爆発そのものを 5 回起こします。
/// </summary>
internal static class Scp914Detonation
{
    private const int BlastCount = 5;

    /// <summary>
    /// アイテムを消し、出力位置で HE 相当の爆発を起こします。
    /// </summary>
    public static void Blast(Scp914Context context)
    {
        UnityEngine.Vector3 position = context.OutputPosition;

        // 加害者は「入れた人」。床のアイテムなら持ち主がいないのでホスト扱いにする。
        ReferenceHub source = context.Owner is { IsDestroyed: false } owner
            ? owner.ReferenceHub
            : Server.Host.ReferenceHub;

        context.Consume();

        for (int index = 0; index < BlastCount; index++)
            ExplosionUtils.ServerExplode(position, new Footprinting.Footprint(source), ExplosionType.Grenade);
    }
}
