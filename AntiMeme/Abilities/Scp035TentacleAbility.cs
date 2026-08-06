using AntiMeme.Maps.Objects;
using Sliced.API.Features;

namespace AntiMeme.Abilities;

/// <summary>SCP-035 が視線先の床へ一時的な触手を生成します。</summary>
public sealed class Scp035TentacleAbility : AbilityBase
{
    private const float Lifetime = 30f;

    public override string Name => "触手";

    public override string Description => "視線先に30秒間活動する触手を生成します。";

    public override float Cooldown => 10f;

    protected override void OnUsed()
    {
        new Tentacle
        {
            Position = CreateSinkholeAbility.AimedGround(Player, 12f, 10f, 0.02f),
            Lifespan = Lifetime,
            IsSaveable = false,
        }.Create();
    }
}
