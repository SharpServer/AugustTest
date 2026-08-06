using System;
using System.Collections.Generic;
using System.Linq;
using CustomPlayerEffects;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using Mirror;
using Sliced.API.Features;
using UnityEngine;

using Logger = LabApi.Features.Console.Logger;

namespace AntiMeme.Effects;

/// <summary>
/// <see cref="ICustomEffect"/> を実装した効果を、プレイヤーの効果コンポーネント群へ取り付けます。
///
/// <para>
/// ゲーム本体の <see cref="PlayerEffectsController.Awake"/> は
/// <c>effectsGameObject</c> の子から <see cref="StatusEffectBase"/> を集めて
/// <c>AllEffects</c> と <c>_effectsByType</c> を作ります。
/// つまり<b>プレイヤーのプレハブへ差しておけば、以降に接続する全員が普通に持ちます</b>。
/// 旧 <c>CustomStatusEffectsRegistry</c> がコントローラの内部フィールドを
/// リフレクションで舐めていたのは、既に生成済みのプレイヤーへ後入れするためでした。
/// その経路も残していますが、publicized アセンブリなので触るのは 4 つのフィールドだけです。
/// </para>
/// </summary>
public sealed class EffectInstaller : EventHandlerBase
{
    private static readonly List<Type> EffectTypes =
        TypeParser.FindTypes<ICustomEffect>()
            .Where(type => typeof(StatusEffectBase).IsAssignableFrom(type))
            .ToList();

    private static bool prefabPatched;

    /// <inheritdoc/>
    public override void RegisterEvents()
    {
        // ここが呼ばれる時点ではまだ NetworkManager が立ち上がっていないことがあるので、
        // ロビーに入るたびにもう一度試す (取り付け済みなら何もしない)。
        ServerEvents.WaitingForPlayers += Install;
        Install();
    }

    /// <inheritdoc/>
    public override void UnregisterEvents() => ServerEvents.WaitingForPlayers -= Install;

    private static void Install()
    {
        if (EffectTypes.Count == 0) return;

        InstallIntoPrefab();

        foreach (Player player in Player.List)
        {
            InstallIntoHub(player.ReferenceHub);
        }
    }

    /// <summary>
    /// プレイヤープレハブへ差します。以降に生成されるプレイヤーは
    /// <see cref="PlayerEffectsController.Awake"/> がそのまま拾ってくれます。
    /// </summary>
    private static void InstallIntoPrefab()
    {
        if (prefabPatched) return;

        NetworkManager manager = NetworkManager.singleton;
        if (manager == null || manager.playerPrefab == null) return;

        ReferenceHub hub = manager.playerPrefab.GetComponent<ReferenceHub>();
        if (hub == null || hub.playerEffectsController == null) return;

        foreach (Type type in EffectTypes)
        {
            Attach(hub.playerEffectsController, type);
        }

        prefabPatched = true;
        Logger.Info($"[AntiMeme] カスタム効果 {EffectTypes.Count} 件をプレイヤープレハブへ取り付けました。");
    }

    /// <summary>
    /// 既に生成済みのプレイヤーへ後入れします。<see cref="PlayerEffectsController.Awake"/> は
    /// 走り終わっているので、索引 3 つ (<c>AllEffects</c> / <c>EffectsLength</c> /
    /// <c>_effectsByType</c>) と同期リストを揃え直します。
    /// </summary>
    private static void InstallIntoHub(ReferenceHub hub)
    {
        if (hub == null || hub.playerEffectsController == null) return;

        PlayerEffectsController controller = hub.playerEffectsController;

        foreach (Type type in EffectTypes)
        {
            if (Attach(controller, type) is not { } effect) continue;

            controller.AllEffects = controller.AllEffects.Append(effect).ToArray();
            controller.EffectsLength = controller.AllEffects.Length;
            controller._effectsByType[type] = effect;

            while (controller._syncEffectsIntensity.Count < controller.EffectsLength)
            {
                controller._syncEffectsIntensity.Add(0);
            }
        }
    }

    /// <summary>
    /// 効果コンポーネントを 1 つぶら下げます。既にあれば null を返します。
    /// </summary>
    private static StatusEffectBase Attach(PlayerEffectsController controller, Type type)
    {
        GameObject root = controller.effectsGameObject;

        if (root == null || root.GetComponentInChildren(type, true) != null) return null;

        GameObject holder = new GameObject(type.Name);
        holder.transform.SetParent(root.transform, false);

        return holder.AddComponent(type) as StatusEffectBase;
    }
}
