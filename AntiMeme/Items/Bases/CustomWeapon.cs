using CameraShaking;
using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Items.Firearms.Modules;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using Mirror;
using Sliced.API.Features;
using UnityEngine;

namespace AntiMeme.Items.Bases;

/// <summary>
/// 銃器のカスタムアイテム基底です。
///
/// <para>
/// 数値の焼き付けは <see cref="Customize(Item)"/> のたびに走ります。落として拾い直すと
/// <see cref="LabApi.Features.Wrappers.Item"/> の実体が作り直されるため、
/// ダメージ・容量・アタッチメントは毎回焼き直す必要があります。
/// 一方 <see cref="InitialAmmo"/> は生成時 1 回だけです
/// (拾い直しのたびに満タンへ戻ってしまうため)。
/// </para>
/// <para>
/// 弾数そのものはゲーム側がシリアルをキーに保持している
/// (<see cref="MagazineModule.SyncData"/> / <see cref="CylinderAmmoModule.ChambersCache"/>) ので、
/// 落とす・拾うをまたいでも消えません。アイテムを物理的に差し替えるとき (モード切替など) だけ
/// <see cref="CaptureAmmo"/> / <see cref="RestoreAmmo"/> で持ち回します。
/// </para>
/// </summary>
public abstract class CustomWeapon : CustomItem
{
    private static bool hooked;

    private int totalAmmoBeforeShot;
    private bool initialAmmoApplied;

    protected CustomWeapon() => Hook();

    /// <summary>
    /// この銃の実体です。地面に落ちていれば null。
    /// </summary>
    public FirearmItem Firearm => Item as FirearmItem;

    /// <summary>
    /// 1 発あたりの基礎ダメージ。負値ならバニラのまま。
    /// アタッチメントのダメージ倍率はバニラ同様この値に掛かります。
    /// </summary>
    protected virtual float Damage => -1f;

    /// <summary>
    /// マガジン容量。0 ならバニラのまま。
    /// <b>アタッチメント補正を含んだ実効容量</b>として扱うので、
    /// 拡張マガジンを付けてもここに書いた数がそのまま最大装弾数になります。
    /// </summary>
    protected virtual int MagazineSize => 0;

    /// <summary>
    /// 生成直後の装填数。0 以下なら触りません。既定は満タン。
    /// </summary>
    protected virtual int InitialAmmo => MagazineSize;

    /// <summary>
    /// 1 発撃つのに消費する弾数。1 ならバニラのまま。
    /// 2 以上のときは残弾が足りなければ発射自体を止めます。
    /// </summary>
    protected virtual int AmmoDrain => 1;

    /// <summary>
    /// 地面に落ちているときの見た目のスケール。
    /// </summary>
    protected virtual Vector3 Scale => Vector3.one;

    /// <summary>
    /// 固定するアタッチメント。空ならプレイヤーの設定に任せます。
    /// </summary>
    protected virtual AttachmentName[] Attachments => [];

    /// <summary>
    /// プレイヤーによるアタッチメント変更を許すか。
    /// </summary>
    protected virtual bool AllowAttachmentChanges => true;

    /// <summary>
    /// この銃で誰かを傷つける直前に呼ばれます。ダメージの書き換えや追加効果はここで。
    /// </summary>
    protected virtual void OnHit(PlayerHurtingEventArgs ev)
    {
    }

    /// <summary>
    /// リロード開始時に呼ばれます。
    /// </summary>
    protected virtual void OnReloading(PlayerReloadingWeaponEventArgs ev)
    {
    }

    /// <summary>
    /// リロード完了時に呼ばれます。
    /// </summary>
    protected virtual void OnReloaded(PlayerReloadedWeaponEventArgs ev)
    {
    }

    /// <summary>
    /// 反動パターンを差し替えます。
    /// </summary>
    protected static void SetRecoil(Item item, RecoilSettings recoil)
    {
        if (item is not FirearmItem firearm) return;

        foreach (ModuleBase module in firearm.Modules)
        {
            if (module is RecoilPatternModule pattern)
                pattern.BaseRecoil = recoil;
        }
    }

    /// <summary>
    /// マガジンと薬室の弾数を取り出します。物理アイテムを差し替える直前に呼びます。
    /// </summary>
    public AmmoState CaptureAmmo() =>
        Firearm is { } firearm ? new AmmoState(firearm.StoredAmmo, firearm.ChamberedAmmo) : default;

    /// <summary>
    /// <see cref="CaptureAmmo"/> で取り出した弾数を書き戻します。
    /// </summary>
    public void RestoreAmmo(AmmoState state)
    {
        if (Firearm is not { } firearm) return;

        firearm.StoredAmmo = state.Magazine;
        firearm.ChamberedAmmo = state.Chamber;
    }

    /// <summary>
    /// ボルトを 1 回動かします。強制的に持ち替えさせた自動火器は未コックのまま残って
    /// 撃てなくなるので、その直後に呼びます。
    /// マガジンから薬室へ 1 発移るだけなので総弾数は変わりません。
    /// </summary>
    public void CycleAction()
    {
        if (Firearm is not { } firearm) return;

        foreach (ModuleBase module in firearm.Modules)
        {
            if (module is AutomaticActionModule automatic)
            {
                automatic.ServerCycleAction();
                return;
            }
        }
    }

    /// <inheritdoc/>
    protected override void Customize(Item item)
    {
        if (item is not FirearmItem firearm) return;

        ApplyAttachments(firearm);
        ApplyDamage(firearm);
        ApplyCapacity(firearm);

        if (initialAmmoApplied || InitialAmmo <= 0) return;

        initialAmmoApplied = true;
        firearm.StoredAmmo = Mathf.Min(InitialAmmo, firearm.MaxAmmo);
    }

    /// <inheritdoc/>
    protected override void Customize(Pickup pickup)
    {
        if (Scale == Vector3.one) return;

        // Mirror のスポーンメッセージはスケールを含むので、送信済みなら送り直す。
        if (pickup.IsSpawned)
        {
            NetworkServer.UnSpawn(pickup.GameObject);
            pickup.Transform.localScale = Scale;
            pickup.Spawn();
            return;
        }

        pickup.Transform.localScale = Scale;
    }

    /// <inheritdoc/>
    protected override void OnShooting(PlayerShootingWeaponEventArgs ev)
    {
        if (AmmoDrain <= 1) return;

        FirearmItem firearm = ev.FirearmItem;
        int total = firearm.StoredAmmo + firearm.ChamberedAmmo;

        if (total < AmmoDrain)
        {
            ev.IsAllowed = false;
            return;
        }

        totalAmmoBeforeShot = total;
    }

    /// <inheritdoc/>
    protected override void OnShot()
    {
        if (AmmoDrain <= 1 || Firearm is not { } firearm) return;

        int consumed = totalAmmoBeforeShot - (firearm.StoredAmmo + firearm.ChamberedAmmo);
        int remaining = AmmoDrain - consumed;
        if (remaining <= 0) return;

        int fromChamber = Mathf.Min(firearm.ChamberedAmmo, remaining);
        if (fromChamber > 0)
        {
            firearm.ChamberedAmmo -= fromChamber;
            remaining -= fromChamber;
        }

        if (remaining > 0)
            firearm.StoredAmmo = Mathf.Max(0, firearm.StoredAmmo - remaining);
    }

    private void ApplyAttachments(FirearmItem firearm)
    {
        if (Attachments.Length == 0) return;

        firearm.AttachmentsCode = firearm.ValidateAttachmentsCode(Attachments);
    }

    private void ApplyDamage(FirearmItem firearm)
    {
        if (Damage < 0f) return;

        foreach (ModuleBase module in firearm.Modules)
        {
            if (module is HitscanHitregModuleBase hitreg)
                hitreg.BaseDamage = Damage;
        }
    }

    private void ApplyCapacity(FirearmItem firearm)
    {
        if (MagazineSize <= 0) return;

        // AmmoMax = _defaultCapacity + アタッチメント補正。補正分を引いてから焼く。
        int modifier = (int)firearm.Base.AttachmentsValue(AttachmentParam.MagazineCapacityModifier);
        int capacity = Mathf.Max(0, MagazineSize - modifier);

        foreach (ModuleBase module in firearm.Modules)
        {
            switch (module)
            {
                case MagazineModule magazine:
                    magazine._defaultCapacity = capacity;
                    break;

                case CylinderAmmoModule cylinder:
                    cylinder._defaultCapacity = capacity;
                    break;
            }
        }
    }

    private static void Hook()
    {
        if (hooked) return;

        hooked = true;

        PlayerEvents.ChangingAttachments += OnAnyChangingAttachments;
        PlayerEvents.Hurting += OnAnyHurting;
        PlayerEvents.ReloadingWeapon += OnAnyReloading;
        PlayerEvents.ReloadedWeapon += OnAnyReloaded;

        AntiMeme.Items.ItemRuntime.Register(() =>
        {
            PlayerEvents.ChangingAttachments -= OnAnyChangingAttachments;
            PlayerEvents.Hurting -= OnAnyHurting;
            PlayerEvents.ReloadingWeapon -= OnAnyReloading;
            PlayerEvents.ReloadedWeapon -= OnAnyReloaded;
            hooked = false;
        });
    }

    private static void OnAnyChangingAttachments(PlayerChangingAttachmentsEventArgs ev)
    {
        if (Of(ev.FirearmItem.Serial) is CustomWeapon weapon && !weapon.AllowAttachmentChanges)
            ev.IsAllowed = false;
    }

    private static void OnAnyReloading(PlayerReloadingWeaponEventArgs ev)
    {
        if (Of(ev.FirearmItem.Serial) is CustomWeapon weapon)
            weapon.OnReloading(ev);
    }

    private static void OnAnyReloaded(PlayerReloadedWeaponEventArgs ev)
    {
        if (Of(ev.FirearmItem.Serial) is CustomWeapon weapon)
            weapon.OnReloaded(ev);
    }

    /// <summary>
    /// 命中イベントは武器を教えてくれないので、撃った本人が今持っているアイテムで引きます。
    /// </summary>
    private static void OnAnyHurting(PlayerHurtingEventArgs ev)
    {
        if (ev.Attacker is not { IsDestroyed: false } attacker) return;
        if (attacker.CurrentItem is not { } held) return;

        if (Of(held.Serial) is CustomWeapon weapon)
            weapon.OnHit(ev);
    }

    /// <summary>
    /// 銃の弾薬状態です。物理アイテムを差し替えるあいだだけ持ち回します。
    /// </summary>
    public readonly struct AmmoState
    {
        public AmmoState(int magazine, int chamber)
        {
            Magazine = magazine;
            Chamber = chamber;
        }

        /// <summary>マガジンに入っている弾数です。</summary>
        public int Magazine { get; }

        /// <summary>薬室に入っている弾数です。</summary>
        public int Chamber { get; }
    }
}
