using AntiMeme.Items.Bases;
using LabApi.Features.Wrappers;

namespace AntiMeme.Items.Nvg;

/// <summary>
/// SCP-1344 を土台にしたカスタム暗視ゴーグルです。
/// </summary>
/// <remarks>
/// 掛け外しの段取り (演出の長さ・幻覚効果の除去・顔の見た目の後始末) は
/// <see cref="CustomGoggles"/> が持ちます。こちらが足すのは
/// <b>掛かっている間のライトと電池</b>だけです。
/// </remarks>
public abstract class CustomNvg : CustomGoggles
{
    protected CustomNvg() => NvgManager.Hook();

    /// <summary>ライトと電池の設定です。</summary>
    protected virtual NvgProfile Profile => NvgProfile.Default;

    /// <inheritdoc/>
    protected override bool Remove1344Effect => Profile.Remove1344Effect;

    /// <inheritdoc/>
    /// <remarks>掛かり切ってから点けます。装着演出の途中で点けると暗転と喧嘩します。</remarks>
    protected override void OnWorn(Player player) => NvgManager.Start(Serial, player, Profile);

    /// <inheritdoc/>
    protected override void OnRemoved(Player player) => NvgManager.Stop(Serial);

    /// <inheritdoc/>
    protected override void OnUnequipped(Player player) => NvgManager.Stop(Serial);

    /// <inheritdoc/>
    protected override void OnReleased()
    {
        NvgManager.Stop(Serial);
        base.OnReleased();
    }

    public static bool HasBattery(ushort serial) => NvgManager.HasBattery(serial);

    public static float GetBattery(ushort serial, float fallback = 0f) =>
        NvgManager.GetBattery(serial, fallback);

    public static bool SetBattery(ushort serial, float battery, bool reviveIfDead = true) =>
        NvgManager.TrySetBattery(serial, battery, reviveIfDead);

    public static bool Recharge(ushort serial) => NvgManager.TrySetBattery(serial, 100f, true);

    public static bool IsActive(ushort serial) => NvgManager.IsActive(serial);
}
