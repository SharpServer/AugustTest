namespace AntiMeme.Items.Utility.Battery;

/// <summary>
/// SCP:CB 電池で充電できる対象の契約です。
/// 電池は「何を充電しているか」を知らず、対象側が自分の残量と充電方法を名乗ります。
/// </summary>
public interface IRechargeableBatteryTarget
{
    /// <summary>選択ヒントに出す種別名です。</summary>
    string Kind { get; }

    /// <summary>選択ヒントに出す表示名です。</summary>
    string DisplayName { get; }

    /// <summary>残量です。0〜100。</summary>
    float Percent { get; }

    /// <summary>今この対象を充電できるか。</summary>
    bool CanRecharge { get; }

    /// <summary>充電します。<paramref name="fullRecharge"/> なら満充電にします。</summary>
    void Recharge(float amount, bool fullRecharge);
}
