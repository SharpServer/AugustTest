using System.Collections.Generic;
using Sliced.API.Features;

namespace AntiMeme.Abilities;

/// <summary>
/// 複数の効果から 1 つを選んで使うアビリティです。
///
/// <para>
/// 選択中の位置は<b>ただのフィールド</b>です。<see cref="AbilityBase"/> はプレイヤー 1 人につき
/// 1 インスタンスなので、旧 <c>OptionAbilityBase</c> のように
/// 「今どのプレイヤーの選択を見ているのか」を引数で持ち回す必要がありません。
/// </para>
/// <para>
/// 選択肢の切り替えは <see cref="SelectNext"/> / <see cref="SelectPrevious"/> を入力層から呼びます。
/// 基底が入力方式を持たないのは <see cref="AbilityBase"/> と同じ方針です。
/// </para>
/// </summary>
/// <example>
/// <code>
/// public sealed class SoundOfFifthAbility : ChoiceAbility
/// {
///     private static readonly IReadOnlyList&lt;AbilityChoice&gt; Ranges =
///     [
///         new SoundPulse("近距離", 5f),
///         new SoundPulse("遠距離", 12f),
///     ];
///
///     public override IReadOnlyList&lt;AbilityChoice&gt; Choices => Ranges;
/// }
/// </code>
/// </example>
public abstract class ChoiceAbility : AbilityBase
{
    private int index;

    /// <summary>
    /// 選択肢です。中身は不変なので <c>static readonly</c> で全プレイヤー共有にして構いません。
    /// </summary>
    public abstract IReadOnlyList<AbilityChoice> Choices { get; }

    /// <summary>
    /// 現在選んでいる選択肢です。
    /// </summary>
    public AbilityChoice Selected => Choices[index];

    /// <inheritdoc/>
    public override string Description => Selected.Description;

    /// <summary>
    /// 切り替えヒントの表示秒数です。
    /// </summary>
    protected virtual float SwitchHintDuration => 2f;

    /// <summary>
    /// 次の選択肢へ切り替えます。末尾からは先頭へ戻ります。
    /// </summary>
    public void SelectNext() => Select(1);

    /// <summary>
    /// 前の選択肢へ切り替えます。先頭からは末尾へ回ります。
    /// </summary>
    public void SelectPrevious() => Select(-1);

    /// <inheritdoc/>
    protected sealed override void OnUsed() => Selected.Use(Player);

    /// <inheritdoc/>
    protected override bool CanUse(out string failureReason) =>
        base.CanUse(out failureReason) && Selected.CanUse(Player, out failureReason);

    private void Select(int step)
    {
        int count = Choices.Count;

        index = ((index + step) % count + count) % count;

        AbilityChoice choice = Selected;

        Player.SendHint(
            $"<size=24>{Name}: {choice.Name}</size>\n<size=20>{choice.Description}</size>",
            SwitchHintDuration);
    }
}
