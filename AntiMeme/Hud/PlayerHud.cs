using HintServiceMeow.Core.Models.Hints;
using HintServiceMeow.Core.Utilities;
using LabApi.Features.Wrappers;

using HsmHint = HintServiceMeow.Core.Models.Hints.Hint;

namespace AntiMeme.Hud;

/// <summary>
/// 1 人分の HUD です。<c>player.Hud()</c> で取り出します。
///
/// 状態は一切持ちません。ヒントの実体は HintServiceMeow の
/// <see cref="PlayerDisplay"/> が抱えているので、
/// こちら側に per-player の辞書は要りません。値型なので使い捨てで構いません。
/// </summary>
public readonly struct PlayerHud
{
    private readonly Player player;

    internal PlayerHud(Player player)
    {
        this.player = player;
    }

    /// <summary>この画面の持ち主。</summary>
    public Player Player => player;

    /// <summary>HintServiceMeow の描画先。</summary>
    public PlayerDisplay Display => PlayerDisplay.Get(player);

    /// <summary>
    /// 表示すべき対象です。生きていれば自分、死んで誰かを観戦中ならその相手。
    /// 旧実装が観戦イベントを購読して <c>Dictionary&lt;int, Player&gt;</c> に覚えていた部分は、
    /// LabApi の <see cref="LabApi.Features.Wrappers.Player.CurrentlySpectating"/> で足ります。
    /// </summary>
    public Player Subject
    {
        get
        {
            if (player.IsAlive) return player;

            return player.CurrentlySpectating is { IsAlive: true } target ? target : player;
        }
    }

    /// <summary>
    /// スロットに文字列を入れます。空文字なら、既にあるヒントを空にするだけで新しくは作りません
    /// (何も表示しないプレイヤーのぶんまでヒントを作らないため)。
    /// </summary>
    public void Set(HudSlot slot, HudContent content)
    {
        PlayerDisplay display = Display;

        if (display.GetHint(slot.Id) is AbstractHint existing)
        {
            // Parameters を先に入れる。Text の変更で描画が走るので、順が逆だと 1 周ぶん古い値が出る。
            existing.Parameters = content.Parameters;
            existing.Text = content.Text;

            return;
        }

        if (string.IsNullOrEmpty(content.Text)) return;

        display.AddHint(new HsmHint
        {
            Id = slot.Id,
            Text = content.Text,
            Parameters = content.Parameters,
            XCoordinate = slot.X,
            YCoordinate = slot.Y,
            YCoordinateAlign = slot.VerticalAlign,
            FontSize = slot.FontSize,
            Alignment = slot.Alignment,
            SyncSpeed = slot.SyncSpeed,
            ResolutionBasedAlign = true,
        });
    }

    /// <summary>スロットの現在の文字列。まだ何も入っていなければ空文字。</summary>
    public string Get(HudSlot slot) =>
        Display.GetHint(slot.Id) is AbstractHint hint ? hint.Text : string.Empty;

    /// <summary>
    /// pull スロットを今すぐ 1 回だけ更新します。ループの間隔を待たずに反映したいときに使います。
    /// </summary>
    public void Refresh(HudSlot slot)
    {
        if (slot.Source is null) return;

        Set(slot, slot.Source(player, Subject));
    }

    /// <summary>スロットを消します。</summary>
    public void Clear(HudSlot slot) => Display.RemoveHint(slot.Id);

    /// <summary>この HUD が持つスロットをすべて消します。他プラグインのヒントには触りません。</summary>
    public void ClearAll()
    {
        PlayerDisplay display = Display;

        foreach (HudSlot slot in HudSlot.All)
        {
            display.RemoveHint(slot.Id);
        }
    }

    /// <summary>全員のスロットに同じ文字列を入れます。</summary>
    public static void SetAll(HudSlot slot, HudContent text)
    {
        foreach (Player target in Player.ReadyList)
        {
            new PlayerHud(target).Set(slot, text);
        }
    }

    /// <summary>全員の HUD を消します。</summary>
    public static void ClearEveryone()
    {
        foreach (Player target in Player.ReadyList)
        {
            new PlayerHud(target).ClearAll();
        }
    }
}
