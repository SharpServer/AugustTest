using System.Collections.Generic;
using LabApi.Features.Wrappers;
using MEC;
using Mirror;
using PlayerRoles.FirstPersonControl;
using Sliced.API.Features;
using UnityEngine;

namespace AntiMeme.Abilities;

/// <summary>
/// プレイヤーを物理的に吹き飛ばす／打ち上げるための最小限の口です。
///
/// <para>
/// 中身はゲーム本体の <see cref="FpcMotor"/> を直接触るのでフレームワークに依存しません。
/// 旧 <c>FpcMovementExtensions</c> は重力・インパルス・復帰ハンドルの 3 つを
/// <c>Dictionary&lt;int, ...&gt;</c> で持っていましたが、寿命は <see cref="PlayerScope"/> に預けられるので
/// 「同じプレイヤーへ二重にインパルスを掛けない」ための 1 本だけが残っています
/// (二重に走ると <see cref="CharacterController.Move"/> が 1 フレームで 2 回動かしてしまうため)。
/// </para>
/// </summary>
internal static class FpcPush
{
    private static readonly Dictionary<Player, CoroutineHandle> Running = new Dictionary<Player, CoroutineHandle>();

    /// <summary>
    /// 強制的にジャンプさせます。<paramref name="gravityDuration"/> のあいだだけ重力を差し替えて浮遊感を出します。
    /// </summary>
    public static bool Jump(Player player, float power, Vector3 horizontalVelocity, Vector3 gravity, float gravityDuration)
    {
        if (!TryGetMotor(player, out FpcMotor motor)) return false;

        if (gravityDuration > 0f)
        {
            motor.GravityController.Gravity = gravity;

            PlayerScope.Of(player).Delay(
                gravityDuration,
                owner => FpcGravityController.ServerSetGravity(owner.ReferenceHub, FpcGravityController.DefaultGravity));
        }

        // ForceJump は現在の MoveDirection.y を見るので、前後で 2 回書いて上向き成分を確定させる。
        motor.MoveDirection = new Vector3(horizontalVelocity.x, Mathf.Max(motor.MoveDirection.y, 0f), horizontalVelocity.z);
        motor.ResetFallDamageCooldown();

        motor.JumpController.ForceJump(power);

        motor.MoveDirection = new Vector3(horizontalVelocity.x, Mathf.Max(motor.MoveDirection.y, power), horizontalVelocity.z);
        motor.ResetFallDamageCooldown();

        return true;
    }

    /// <summary>
    /// 指定した速度で <paramref name="duration"/> 秒かけて押し出します。速度は時間とともに減衰します。
    /// </summary>
    public static bool Impulse(Player player, Vector3 velocity, float duration)
    {
        if (velocity.sqrMagnitude < 0.01f || !TryGetMotor(player, out FpcMotor motor)) return false;

        if (velocity.y > 0f)
            motor.JumpController.ForceJump(velocity.y);

        PlayerScope scope = PlayerScope.Of(player);

        if (Running.TryGetValue(player, out CoroutineHandle previous))
            Timing.KillCoroutines(previous);
        else
            scope.OnDispose(owner => Running.Remove(owner));

        Running[player] = scope.Track(Timing.RunCoroutine(Push(player, velocity, Mathf.Max(0.05f, duration))));

        return true;
    }

    private static IEnumerator<float> Push(Player player, Vector3 velocity, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (!player.IsAlive || !TryGetMotor(player, out FpcMotor motor)) break;

            // フレーム落ちで 1 回に大きく飛ばないように刻む。
            float delta = Mathf.Clamp(Time.deltaTime, 0.01f, 0.05f);

            motor.MoveDirection = velocity;
            motor.ResetFallDamageCooldown();
            Move(motor.MainModule, motor, velocity * delta);

            elapsed += delta;

            float decay = Mathf.Clamp01(delta * 5.5f);
            velocity.x = Mathf.Lerp(velocity.x, 0f, decay);
            velocity.z = Mathf.Lerp(velocity.z, 0f, decay);
            velocity.y = Mathf.Max(velocity.y + motor.GravityController.Gravity.y * delta * 0.25f, -1f);

            yield return Timing.WaitForOneFrame;
        }

        Running.Remove(player);
    }

    private static void Move(FirstPersonMovementModule module, FpcMotor motor, Vector3 displacement)
    {
        if (module.CharControllerSet)
        {
            module.CharController.Move(displacement);
            module.Position = motor.CachedTransform.position;
        }
        else
        {
            module.Position += displacement;
        }

        // ServerOverridePosition は OnServerPositionOverwritten を無条件に呼ぶので、購読者が居るときだけ通す。
        if (NetworkServer.active && module.OnServerPositionOverwritten is not null)
            module.ServerOverridePosition(module.Position);
    }

    private static bool TryGetMotor(Player player, out FpcMotor motor)
    {
        if (player.RoleBase is IFpcRole { FpcModule: { ModuleReady: true, Motor: not null } module })
        {
            motor = module.Motor;

            return true;
        }

        motor = null;

        return false;
    }
}
