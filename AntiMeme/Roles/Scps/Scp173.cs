using AntiMeme.Roles;
using UnityEngine;
using System.Collections.Generic;
using AntiMeme.Abilities;
using CustomPlayerEffects;
using PlayerRoles;
using Sliced.API.Features;
using Sliced.API.Structs;

using ExiledScp173 = Exiled.Events.Handlers.Scp173;
using BlinkingEventArgs = Exiled.Events.EventArgs.Scp173.BlinkingEventArgs;

namespace AntiMeme.Roles.Scps;

/// <summary>
/// SCP-173。体力とヒュームシールドを盛ったうえで、
/// <b>3 人以上に同時に見られている間は瞬き移動を封じます</b>。
/// </summary>
public class Scp173 : ScpRole
{
    private const int BlinkBlockObservers = 3;

    /// <inheritdoc/>
    protected internal override string CassieName => "SCP 1 7 3";

    public override string Name => "SCP-173";

    /// <inheritdoc/>
    public override string HudLabel => "<color=#c50000>SCP-173</color>";

    /// <inheritdoc/>
    public override string Objective => "一瞬の隙を突き、財団職員共をへし折れ！";

    public override string Description =>
        "相手が瞬きしたときに超高速で移動し、首をへし折る。";

    public override RoleTypeId BaseRole => RoleTypeId.Scp173;

    /// <summary>マップ側のマーカーで指定します。マーカーが無ければバニラの地点です。</summary>
    public override Vector3? SpawnPosition => SpawnPoints.Tagged("Scp173SpawnPoint");

    public override float? MaxHealth => 4500f;

    public override string CustomInfo => "SCP-173";

    /// <summary>
    /// 収容室から出るまでの助走をなくすための開幕 1 分間の鈍足です。
    /// </summary>
    public override IReadOnlyList<RoleEffect> Effects =>
    [
        RoleEffect.Of<Slowness>(95, 60f),
    ];

    protected override void OnSpawned()
    {
        SetHumeShield(1500f);
        AbilityBase.Give<TeleportRandomAbility>(Player);
        AbilityBase.Give<PlaceTantrumAbility>(Player);

        // LabApi の Scp173Events には観測者数を持つ瞬き前イベントが無いので、ここだけ EXILED を使う。
        Hook(
            () => ExiledScp173.Blinking += OnBlinking,
            () => ExiledScp173.Blinking -= OnBlinking);
    }

    private void OnBlinking(BlinkingEventArgs ev)
    {
        if (IsMine(ev.Player.ReferenceHub) && ev.Targets.Count >= BlinkBlockObservers)
            ev.Scp173.BlinkReady = false;
    }
}
