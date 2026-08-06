using LabApi.Features.Wrappers;

using ExiledRoom = Exiled.API.Features.Room;
using RoomType = Exiled.API.Enums.RoomType;

namespace AntiMeme.Maps;

/// <summary>
/// 部屋名の日本語訳です。
/// </summary>
/// <remarks>
/// <para>
/// 発狂表示・S-Nav・HUD がそれぞれ自前の訳表を持っていたので 1 本にまとめました。
/// 訳が要る場所が増えてもここだけを直せば済みます。
/// </para>
/// <para>
/// <b>キーはゲーム本体の <c>MapGeneration.RoomName</c> ではなく EXILED の
/// <see cref="RoomType"/> です。</b> <c>RoomName</c> は名前付きの部屋にしか値が無く、
/// 通路・交差点・曲がり角・エレベーターホールがすべて <c>Unnamed</c> に潰れます。
/// 旧 <c>RoomNameTranslator</c> が <c>RoomType</c> を使っていたのはこのためで、
/// <c>RoomName</c> に置き換えたせいで施設の 3 分の 1 が訳せなくなっていました。
/// </para>
/// </remarks>
public static class RoomNames
{
    /// <summary>
    /// 日本語名です。訳が無い部屋は <paramref name="fallback"/> を返します
    /// (null なら旧実装と同じ「エラー」)。
    /// </summary>
    public static string Of(RoomType room, string fallback = null) => room switch
    {
        RoomType.Unknown => "不明",
        RoomType.LczArmory => "武器庫",
        RoomType.HczElevatorA => "エレベーターホールA",
        RoomType.HczElevatorB => "エレベーターホールB",
        RoomType.LczCurve => "曲がり角",
        RoomType.LczStraight => "直線通路",
        RoomType.Lcz914 => "SCP-914収容室",
        RoomType.LczCrossing => "交差点",
        RoomType.LczTCross => "三叉路",
        RoomType.LczCafe => "PCルーム",
        RoomType.LczPlants => "栽培室",
        RoomType.LczToilets => "トイレ",
        RoomType.LczAirlock => "エアロック",
        RoomType.Lcz173 => "SCP-173収容室",
        RoomType.LczClassDSpawn => "Dクラス職員収容室",
        RoomType.LczCheckpointB => "チェックポイントB-L",
        RoomType.LczGlassBox => "SCP-372収容室",
        RoomType.LczCheckpointA => "チェックポイントA-L",
        RoomType.Lcz330 => "SCP-330テストチェンバー",
        RoomType.Hcz079 => "SCP-079収容室",
        RoomType.EzCheckpointHallwayA => "チェックポイントE-A",
        RoomType.HczArmory => "武器庫",
        RoomType.Hcz939 => "SCP-939収容室",
        RoomType.HczTestRoom => "テストルーム",
        RoomType.HczHid => "MicroHID格納庫",
        RoomType.Hcz049 => "SCP-049収容室",
        RoomType.HczEzCheckpointA => "チェックポイントA-H",
        RoomType.HczCrossing => "交差点",
        RoomType.Hcz106 => "SCP-106収容室",
        RoomType.HczNuke => "AlphaWarhead格納庫",
        RoomType.HczTesla => "テスラゲート",
        RoomType.HczEzCheckpointB => "チェックポントB-H",
        RoomType.HczCurve => "曲がり角",
        RoomType.Hcz096 => "SCP-096収容室",
        RoomType.EzVent => "搬出ゲート",
        RoomType.EzIntercom => "放送室",
        RoomType.EzGateA => "ゲートA",
        RoomType.EzDownstairsPcs => "通路横PCルーム",
        RoomType.EzCurve => "曲がり角",
        RoomType.EzPcs => "PCルーム",
        RoomType.EzCrossing => "三叉路",
        RoomType.EzCollapsedTunnel => "崩壊した通路",
        RoomType.EzConference => "VIPルーム",
        RoomType.EzStraight => "直線通路",
        RoomType.EzCafeteria => "ベンチ付き直線通路",
        RoomType.EzUpstairsPcs => "2階付きPCルーム",
        RoomType.EzGateB => "ゲートB",
        RoomType.EzShelter => "非常用シェルター",
        RoomType.Pocket => "[削除済み]",
        RoomType.Surface => "地上",
        RoomType.HczStraight => "直線通路",
        RoomType.EzTCross => "三叉路",
        RoomType.EzChef => "直線通路",
        RoomType.EzStraightColumn => "直線通路",
        RoomType.EzCheckpointHallwayB => "チェックポイントB-H",
        RoomType.HczDss08 => "DSS-08 玄妙除却室",
        RoomType.HczCornerDeep => "曲がり角",
        RoomType.HczIntersectionJunk => "三叉路",
        RoomType.HczIntersection => "三叉路",
        RoomType.HczStraightC => "直線通路 (トイレ)",
        RoomType.HczStraightPipeRoom => "直線通路",
        RoomType.HczStraightVariant => "直線通路",
        RoomType.EzSmallrooms => "直線通路 (EV)",
        RoomType.Hcz127 => "SCP-127収容室",
        RoomType.HczServerRoom => "サーバー室",
        RoomType.HczIncineratorWayside => "焼却炉",
        RoomType.HczLoadingBay => "三叉路",
        _ => fallback ?? "エラー",
    };

    /// <summary>
    /// LabAPI の部屋から日本語名を引きます。<see cref="RoomType"/> は EXILED 側にしか
    /// 無いので、ここでラッパーを引き直します。
    /// </summary>
    public static string Of(Room room, string fallback = null) =>
        room is null || ExiledRoom.Get(room.Base) is not { } exiled
            ? fallback ?? "エラー"
            : Of(exiled.Type, fallback);
}
