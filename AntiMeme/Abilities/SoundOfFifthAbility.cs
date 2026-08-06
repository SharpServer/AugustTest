using AntiMeme.Effects;
using System.Collections.Generic;
using System.Linq;
using CustomPlayerEffects;
using LabApi.Features.Wrappers;
using Utils;

namespace AntiMeme.Abilities;

/// <summary>
/// 第五主義の音波。敵の五感を潰し、味方を鼓舞します。射程を切り替えて使います。
/// </summary>
public sealed class SoundOfFifthAbility : ChoiceAbility
{
    /// <summary>
    /// 射程だけが違う 2 つの音波です。中身が不変なので全プレイヤーで共有します
    /// (どれを選んでいるかはアビリティのインスタンス側が持ちます)。
    /// </summary>
    private static readonly IReadOnlyList<AbilityChoice> Ranges =
    [
        new SoundPulse("近距離", 5f, "5m以内の対象へ強く干渉します。"),
        new SoundPulse("遠距離", 12f, "12m以内の対象へ広く干渉します。"),
    ];

    /// <inheritdoc/>
    public override string Name => "第五からの音";

    /// <inheritdoc/>
    public override float Cooldown => 20f;

    /// <inheritdoc/>
    public override IReadOnlyList<AbilityChoice> Choices => Ranges;
}

/// <summary>
/// 音波そのもの。射程を持ち、効果を自分で出します。
/// </summary>
internal sealed class SoundPulse : AbilityChoice
{
    private const float FlashDuration = 2.5f;
    private const float DebuffDuration = 45f;
    private const float AllyBuffDuration = 5f;
    private const byte BlindnessIntensity = 40;

    private readonly float radius;

    public SoundPulse(string name, float radius, string description)
    {
        Name = name;
        Description = description;
        this.radius = radius;
    }

    /// <inheritdoc/>
    public override string Name { get; }

    /// <inheritdoc/>
    public override string Description { get; }

    /// <summary>
    /// 射程内で音が届く相手です。反ミーム耐性を得ている相手には届きません。
    /// </summary>
    private IEnumerable<Player> Reachable(Player player) =>
        AbilityTargeting.Around(player, radius).Where(target => !AntiMemeResistance.Has(target));

    /// <inheritdoc/>
    public override bool CanUse(Player player, out string failureReason)
    {
        if (Reachable(player).Any())
        {
            failureReason = null;

            return true;
        }

        failureReason = "効果範囲内に対象が存在しません。";

        return false;
    }

    /// <inheritdoc/>
    public override void Use(Player player)
    {
        foreach (Player target in Reachable(player))
        {
            if (AbilityTargeting.IsAlly(player, target))
            {
                target.EnableEffect<Invigorated>(1, AllyBuffDuration);

                continue;
            }

            ExplosionUtils.ServerSpawnEffect(target.Position, ItemType.GrenadeFlash);

            target.EnableEffect<Flashed>(1, FlashDuration);
            target.EnableEffect<Deafened>(255, DebuffDuration);
            target.EnableEffect<Hemorrhage>(255, DebuffDuration);
            target.EnableEffect<Blindness>(BlindnessIntensity, DebuffDuration);
            target.EnableEffect<Blurred>(255, DebuffDuration);
        }
    }
}
