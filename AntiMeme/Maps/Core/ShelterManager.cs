using System.Linq;
using AntiMeme.Maps.Objects;
using LabApi.Features.Wrappers;
using UnityEngine;

namespace AntiMeme.Maps.Core;

/// <summary>
/// EZ 避難シェルターの停電と復電です。
///
/// <para>
/// 筋書きは 3 段。<b>初回侵入で停電 → 扉が開かなくなる → 発電機レバーで復電</b>。
/// </para>
/// </summary>
/// <remarks>
/// 旧実装ではこの流れが <c>LeverHandler</c> と <c>UDoorHandler</c> の
/// タグ別ハンドラ表に分かれて登録されていました。状態 (初回侵入済みか・点灯中か) を
/// 持っているのはシェルターなので、扉とレバーの反応もここへまとめます。
/// </remarks>
public sealed class ShelterManager : MapFeature
{
    /// <summary>シェルター内の照明に付いている名札です。</summary>
    public const string LightTag = "EzShelter";

    /// <summary>復電レバーに付いている名札です。</summary>
    public const string GeneratorLeverTag = "EzShelter_GeneratorLever";

    /// <summary>シェルターの扉に付いている名札です。</summary>
    public const string ShelterDoorTag = "EzEvacuationShelter";

    private const string PowerLoopKey = "EzShelter_Power";

    /// <summary>誰かが一度でも入ったかどうか。ここが false の間は停電していません。</summary>
    public static bool HasBeenEntered { get; private set; }

    /// <summary>いま照明が点いているかどうか。</summary>
    public static bool LightIsOn { get; private set; } = true;

    /// <inheritdoc/>
    public override void RegisterEvents()
    {
        InteractableLever.Toggling += OnLeverToggling;
        InteractableLever.Toggled += OnLeverToggled;
        MapSignals.DoorStateChanged += OnDoorStateChanged;
        UsefulDoor.Interacting += OnDoorInteracting;
    }

    /// <inheritdoc/>
    public override void UnregisterEvents()
    {
        InteractableLever.Toggling -= OnLeverToggling;
        InteractableLever.Toggled -= OnLeverToggled;
        MapSignals.DoorStateChanged -= OnDoorStateChanged;
        UsefulDoor.Interacting -= OnDoorInteracting;
    }

    /// <inheritdoc/>
    protected override void Reset()
    {
        HasBeenEntered = false;
        LightIsOn = true;
        MapAudio.Stop(PowerLoopKey);
    }

    /// <summary>停電中はシェルターの扉が動きません。</summary>
    private static void OnDoorInteracting(Player player, UsefulDoor door, ref bool allowed)
    {
        if (!Matches(door, ShelterDoorTag) || !HasBeenEntered || LightIsOn) return;

        allowed = false;
        player?.SendHint("<size=26>電力が通っていないようだ・・・</size>", 3f);
    }

    /// <summary>初回侵入で照明が落ちます。</summary>
    private static void OnDoorStateChanged(Player player, ObjectPrefab prefab, bool isOpen)
    {
        if (prefab is not UsefulDoor door || !Matches(door, ShelterDoorTag)) return;
        if (!isOpen || HasBeenEntered) return;

        HasBeenEntered = true;
        LightIsOn = false;

        foreach (ControllableLight light in ObjectPrefab.Find<ControllableLight>(LightTag))
            light.Level = 0;

        MapAudio.Play("Blackout.ogg", "EzShelter", door.Position, maxDistance: 20f);
    }

    /// <summary>停電するまでは発電機レバーを動かせません。</summary>
    private static void OnLeverToggling(Player player, InteractableLever lever, bool next, ref bool allowed)
    {
        if (!Matches(lever, GeneratorLeverTag)) return;

        if (!HasBeenEntered || LightIsOn)
            allowed = false;
    }

    /// <summary>発電機レバーで復電します。一度上げたら戻せません。</summary>
    private static void OnLeverToggled(Player player, InteractableLever lever, bool isOn)
    {
        if (!Matches(lever, GeneratorLeverTag) || LightIsOn) return;

        lever.CanInteract = false;
        LightIsOn = true;

        foreach (ControllableLight light in ObjectPrefab.Find<ControllableLight>(LightTag))
        {
            light.IsOn = true;
            light.Level = 100;
        }

        MapAudio.Play("PowerUp.ogg", PowerLoopKey + "_A", lever.Position, volume: 1.25f, maxDistance: 20f);
        MapAudio.Loop("Elec_Idle.ogg", PowerLoopKey, lever.Position, volume: 0.35f, maxDistance: 8f);
    }

    private static bool Matches(ObjectPrefab prefab, string tag) =>
        prefab is not null && string.Equals(prefab.Tag, tag, System.StringComparison.OrdinalIgnoreCase);
}
