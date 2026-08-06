using AntiMeme.Items.Bases;
using CameraShaking;
using InventorySystem.Items.Firearms.Modules;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Features.Wrappers;
using UnityEngine;

namespace AntiMeme.Items.Weapons;

/// <summary>
/// 発射するたびに反動値を増減させる調整用リボルバーです。
/// 投擲操作で「どの値を」「どちら向きに」動かすかを切り替えます。
/// </summary>
/// <remarks>
/// 調整対象と方向は<b>ただの表</b>です。以前は 1 段ごとに <c>HybridWeapon</c> の
/// モードクラスを 1 つ作って輪に繋いでいましたが、12 モードすべてが同じ
/// <see cref="ItemType.GunRevolver"/> を土台にしていたため、切り替えのたびに
/// 同じリボルバーを破棄して配り直すだけの空回りでした。
/// 振る舞いを持たない 3 要素の組にクラスを与える必要はありません。
/// </remarks>
public sealed class GunRecoilRampRevolver : CustomWeapon
{
    private const float AnimationTimeStep = 0.05f;
    private const float KickStep = 50f;
    private const float MinAnimationTime = 0.01f;
    private const float MaxAnimationTime = 5f;
    private const float MaxKick = 2000f;

    /// <summary>調整対象と向きの一巡です。投擲のたびに 1 つ進み、末尾から先頭へ戻ります。</summary>
    private static readonly Step[] Steps =
    [
        new Step(Field.All, 1),
        new Step(Field.AnimationTime, 1),
        new Step(Field.ZAxis, 1),
        new Step(Field.FovKick, 1),
        new Step(Field.UpKick, 1),
        new Step(Field.SideKick, 1),
        new Step(Field.All, -1),
        new Step(Field.AnimationTime, -1),
        new Step(Field.ZAxis, -1),
        new Step(Field.FovKick, -1),
        new Step(Field.UpKick, -1),
        new Step(Field.SideKick, -1),
    ];

    private RecoilSettings recoil;
    private bool captured;
    private int step;

    /// <summary>調整できる反動のフィールドです。</summary>
    private enum Field
    {
        All,
        AnimationTime,
        ZAxis,
        FovKick,
        UpKick,
        SideKick,
    }

    public override ItemType BaseType => ItemType.GunRevolver;

    public override string Name => $"Recoil Ramp Revolver [{Label}]";

    public override string Description =>
        $"発射ごとに Recoil {Steps[step].Target} を {Sign} 方向へ変化させる調整用リボルバー。";

    protected override float Damage => 1f;

    protected override int MagazineSize => 18;

    protected override bool AllowAttachmentChanges => false;

    private string Sign => Steps[step].Direction >= 0 ? "+" : "-";

    private string Label => $"{Steps[step].Target} {Sign}";

    /// <inheritdoc/>
    protected override void Customize(Item item)
    {
        base.Customize(item);

        // 素の反動を 1 度だけ控えておき、以後はこの値を積み上げていく。
        if (captured || item is not FirearmItem firearm) return;

        foreach (ModuleBase module in firearm.Modules)
        {
            if (module is not RecoilPatternModule pattern) continue;

            recoil = pattern.BaseRecoil;
            captured = true;
            break;
        }
    }

    /// <inheritdoc/>
    protected override void OnDropping(PlayerDroppingItemEventArgs ev)
    {
        if (!ev.Throw) return;

        ev.IsAllowed = false;
        step = (step + 1) % Steps.Length;

        Owner?.SendHint($"<size=23>Recoil Ramp Revolver [{Label}]</size>", 2f);
    }

    /// <inheritdoc/>
    protected override void OnShot()
    {
        base.OnShot();

        recoil = Clamp(Adjust(recoil));

        if (captured)
            SetRecoil(Item, recoil);

        if (Firearm is { } firearm)
            firearm.StoredAmmo = firearm.MaxAmmo;
    }

    private RecoilSettings Adjust(RecoilSettings value)
    {
        Step current = Steps[step];
        bool all = current.Target == Field.All;

        if (all || current.Target == Field.AnimationTime)
            value.AnimationTime += AnimationTimeStep * current.Direction;

        if (all || current.Target == Field.ZAxis)
            value.ZAxis += KickStep * current.Direction;

        if (all || current.Target == Field.FovKick)
            value.FovKick += KickStep * current.Direction;

        if (all || current.Target == Field.UpKick)
            value.UpKick += KickStep * current.Direction;

        if (all || current.Target == Field.SideKick)
            value.SideKick += KickStep * current.Direction;

        return value;
    }

    private static RecoilSettings Clamp(RecoilSettings value) => new RecoilSettings(
        Mathf.Clamp(value.AnimationTime, MinAnimationTime, MaxAnimationTime),
        Mathf.Clamp(value.ZAxis, -MaxKick, MaxKick),
        Mathf.Clamp(value.FovKick, -MaxKick, MaxKick),
        Mathf.Clamp(value.UpKick, -MaxKick, MaxKick),
        Mathf.Clamp(value.SideKick, -MaxKick, MaxKick));

    private readonly struct Step
    {
        public Step(Field target, int direction)
        {
            Target = target;
            Direction = direction;
        }

        public Field Target { get; }

        public int Direction { get; }
    }
}
