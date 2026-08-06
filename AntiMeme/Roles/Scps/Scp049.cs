using AntiMeme.Abilities;
using PlayerRoles;
using Sliced.API.Features;

namespace AntiMeme.Roles.Scps;

/// <summary>
/// SCP-049。バニラの 049 に体力とヒュームシールドを上乗せしただけの素直な役職です。
/// </summary>
public class Scp049 : ScpRole
{
    /// <inheritdoc/>
    protected internal override string CassieName => "SCP 0 4 9";

    public override string Name => "SCP-049";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#c50000>SCP-049</color>";

    /// <inheritdoc/>
    public override string Objective => "悪疫を根絶するため、名医の勘で患者を見つけ出し救済せよ。";

    public override string Description =>
        "悪疫を根絶する使命を抱いたペスト医師の見た目のSCP。\n" +
        "名医の感で患者を救い出せ！";

    public override RoleTypeId BaseRole => RoleTypeId.Scp049;

    public override float? MaxHealth => 2200f;

    public override string CustomInfo => "SCP-049";

    protected override void OnSpawned()
    {
        SetHumeShield(1200f);
        AbilityBase.Give<SenseOfGreatDoctorAbility>(Player);
    }
}
