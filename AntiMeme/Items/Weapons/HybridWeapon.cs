using System;
using System.Collections.Generic;
using AntiMeme.Items.Bases;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;

namespace AntiMeme.Items.Weapons;

/// <summary>
/// モードを切り替えられる武器の土台です。
///
/// <para>
/// モードは<b>それぞれが独立したカスタムアイテム</b>で、切り替えると物理アイテムごと差し替わります
/// (土台のバニラアイテムが違うので、同じ実体のまま化けさせることはできません)。
/// 各モードは <see cref="NextMode"/> で次のモードを指し、輪になるように繋ぎます。
/// </para>
/// <para>
/// 弾数はモードごとに別勘定です。切り替えて戻ってくると、そのモードを離れたときの弾数に戻ります。
/// </para>
/// </summary>
/// <remarks>
/// 旧 <c>CItemHybrid</c> は親アイテムが <c>BuildSubModes()</c> で子インスタンスの一覧を持ち、
/// 親子でシリアルを張り替える作りでした。ここでは<b>モードそのものがアイテム</b>なので、
/// 親という概念も一覧も要りません。
///
/// <para>
/// 切り替えの操作は<b>投擲</b>に割り当てています。旧実装はキー入力
/// (<c>EnableKeyModeSwitch</c>) を使っていましたが、入力層 (Server Specific Settings) は
/// まだ再構築されていません。入力層が入ったら <see cref="SwitchMode"/> をそこから呼ぶだけで移せます。
/// </para>
/// </remarks>
public abstract class HybridWeapon : CustomWeapon
{
    private Dictionary<Type, CustomWeapon.AmmoState> ammoByMode =
        new Dictionary<Type, CustomWeapon.AmmoState>();

    private object sharedModeState;

    /// <summary>
    /// 次のモードの型です。最後のモードは最初のモードを指して輪にしてください。
    /// </summary>
    protected abstract Type NextMode { get; }

    /// <summary>
    /// このモードの表示名です。切り替えたときにヒントで出ます。
    /// </summary>
    protected virtual string ModeName => Name;

    /// <summary>
    /// モードをまたいで共有する状態を取得します。最初のモードで 1 度だけ生成され、
    /// 以後の物理アイテム差し替えにも同じインスタンスが渡ります。
    /// </summary>
    protected T SharedModeState<T>() where T : class, new()
    {
        if (sharedModeState is T state) return state;

        state = new T();
        sharedModeState = state;

        return state;
    }

    /// <summary>
    /// 前のモードから共有状態を受け取った直後に呼ばれます。
    /// 状態を銃のモジュールへ焼き直す必要があるモードで使います。
    /// </summary>
    protected virtual void OnModeStateReceived()
    {
    }

    /// <summary>
    /// 次のモードへ切り替えます。持ち主が居なければ何もしません。
    /// </summary>
    public void SwitchMode()
    {
        if (Owner is not { } owner) return;

        // 離れるモードの弾数を覚えてから捨てる。
        ammoByMode[GetType()] = CaptureAmmo();

        Dictionary<Type, CustomWeapon.AmmoState> carried = ammoByMode;

        Destroy();

        if (Give(NextMode, owner) is not HybridWeapon next) return;

        next.ammoByMode = carried;
        next.sharedModeState = sharedModeState;

        if (carried.TryGetValue(NextMode, out CustomWeapon.AmmoState ammo))
            next.RestoreAmmo(ammo);

        next.OnModeStateReceived();

        // 強制的に持ち替えた自動火器は未コックのまま残るので、ここで 1 回動かす。
        owner.CurrentItem = next.Item;
        next.CycleAction();

        owner.SendHint($"<size=23>{next.ModeName}</size>", 2f);
    }

    /// <inheritdoc/>
    protected override void OnDropping(PlayerDroppingItemEventArgs ev)
    {
        if (!ev.Throw) return;

        ev.IsAllowed = false;
        SwitchMode();
    }
}
