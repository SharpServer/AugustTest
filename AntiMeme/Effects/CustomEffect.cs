using System;
using System.Collections.Generic;
using CustomPlayerEffects;
using LabApi.Features.Wrappers;
using Sliced.API.Features;

namespace AntiMeme.Effects;

/// <summary>
/// AntiMeme が持ち込むカスタム状態効果の目印です。
///
/// <see cref="EffectInstaller"/> はこの印が付いた型だけを集めてプレイヤーへ取り付けます。
/// 旧 <c>CustomStatusEffectsRegistry</c> のような ID 採番・登録 API・解決表は要りません
/// (<see cref="StatusEffectBase"/> 自体で集めるとゲーム本体の効果まで拾ってしまうので、
/// 「自分の効果」を示すのに印だけを 1 つ置いています)。
/// </summary>
public interface ICustomEffect
{
}

/// <summary>
/// カスタム状態効果の基底です。ゲーム本体の <see cref="StatusEffectBase"/> に足しているのは 2 つだけです。
///
/// <list type="number">
/// <item>LabApi の <see cref="LabApi.Features.Wrappers.Player"/> を 1 回だけ引いて持つ</item>
/// <item>効果が続くあいだだけのイベント購読 (<see cref="Hook"/>)</item>
/// </list>
///
/// per-player 状態は素のフィールドに持ちます。効果インスタンスはプレイヤーごとに 1 つ
/// (プレイヤーの <c>effectsGameObject</c> にぶら下がるコンポーネント) なので、
/// 旧実装のような <c>Dictionary&lt;int, ...&gt;</c> は要りません。
/// </summary>
public abstract class CustomEffect : StatusEffectBase, ICustomEffect
{
    private readonly List<Action> unhooks = [];

    /// <summary>
    /// この効果が掛かっているプレイヤーです。効果が切れているあいだは null。
    /// </summary>
    public Player Player { get; private set; }

    /// <summary>
    /// このプレイヤーのスコープです。コルーチンはここに載せてください
    /// (退出・ラウンド再開で自動的に止まります)。
    /// </summary>
    protected PlayerScope Scope => PlayerScope.Of(Player);

    /// <inheritdoc/>
    public override void Enabled()
    {
        base.Enabled();

        // ホストの ReferenceHub には Player が付かない。ここで 1 回だけ確かめる。
        if (Player.Get(Hub) is not { } player) return;

        Player = player;
        OnEffectEnabled();
    }

    /// <inheritdoc/>
    public override void Disabled()
    {
        base.Disabled();
        Cleanup();
    }

    /// <summary>
    /// 退出などで <see cref="Disabled"/> を通らずに破棄されたときの受け皿です。
    /// </summary>
    public virtual void OnDestroy() => Cleanup();

    /// <summary>
    /// 効果が始まったときに呼ばれます。<see cref="Player"/> は非 null です。
    /// </summary>
    protected virtual void OnEffectEnabled()
    {
    }

    /// <summary>
    /// 効果が終わるときに呼ばれます。<see cref="Player"/> はまだ非 null です。
    /// </summary>
    protected virtual void OnEffectDisabled()
    {
    }

    /// <summary>
    /// 効果が続くあいだだけイベントを購読します。解除は効果が切れた時点でまとめて走ります。
    /// </summary>
    protected void Hook(Action subscribe, Action unsubscribe)
    {
        subscribe();
        unhooks.Add(unsubscribe);
    }

    /// <summary>
    /// このイベントの対象が自分かどうか。
    /// </summary>
    protected bool IsMine(Player other) =>
        other is not null && Player is { IsDestroyed: false } && ReferenceEquals(other.ReferenceHub, Player.ReferenceHub);

    private void Cleanup()
    {
        if (Player is null) return;

        for (int i = unhooks.Count - 1; i >= 0; i--)
        {
            unhooks[i]();
        }

        unhooks.Clear();
        OnEffectDisabled();
        Player = null;
    }
}
