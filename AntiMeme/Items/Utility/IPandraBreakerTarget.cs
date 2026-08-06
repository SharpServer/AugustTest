namespace AntiMeme.Items.Utility;

/// <summary>
/// Pandra Breaker が抑制装置を起爆できる対象の契約です。
/// SCP-076 側の役職 (<c>Roles.Scps.Scp076</c>) が実装します。
/// </summary>
public interface IPandraBreakerTarget
{
    bool IsActive { get; }
    bool IsResistanceState { get; }
    bool TryDetonateSuppressionDevice();
}
