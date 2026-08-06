using Interactables.Interobjects.DoorUtils;
using InventorySystem.Items.Keycards;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using Sliced.API.Features;
using UnityEngine;

using KeycardItem = LabApi.Features.Wrappers.KeycardItem;
using KeycardPickup = LabApi.Features.Wrappers.KeycardPickup;

namespace AntiMeme.Items.Bases;

/// <summary>
/// カスタムキーカードの基底です。
///
/// <para>
/// 見た目と権限はゲーム側の <see cref="DetailBase"/> 群で表現されます。各 Detail は
/// 「次に作られるカードへ焼き付ける値」を static フィールドに置く作りなので、値をそこへ載せてから
/// <see cref="KeycardDetailSynchronizer.ServerProcessItem"/> を呼び直して焼き直します。
/// 焼き付いた内容はシリアルをキーに
/// <see cref="KeycardDetailSynchronizer.Database"/> へ入り、全クライアントへ配信されます。
/// </para>
/// <para>
/// 権限は 3 軸 (収容 / 武器庫 / 管理) の <see cref="KeycardLevels"/> です。
/// チェックポイント・ゲート・インターコム・核は管理レベルに含まれるので、
/// <see cref="DoorPermissionFlags"/> で書きたい場合も <c>new KeycardLevels(flags)</c> で渡せます。
/// </para>
/// </summary>
public abstract class CustomKeycard : CustomItem
{
    private static bool hooked;

    private ushort customizedSerial;

    protected CustomKeycard() => Hook();

    /// <inheritdoc/>
    public override ItemType BaseType => ItemType.KeycardCustomSite02;

    /// <summary>
    /// このカードの実体です。地面に落ちていれば null。
    /// </summary>
    public KeycardItem Keycard => Item as KeycardItem;

    /// <summary>
    /// 権限レベルです。
    /// </summary>
    protected virtual KeycardLevels Levels => default;

    /// <summary>
    /// カード本体の色です。
    /// </summary>
    protected virtual Color32 Tint => new Color32(255, 255, 255, 255);

    /// <summary>
    /// 権限表示部分の色です。
    /// </summary>
    protected virtual Color32 PermissionsColor => new Color32(255, 255, 255, 255);

    /// <summary>
    /// カード表面のラベルです。
    /// </summary>
    protected virtual string Label => string.Empty;

    /// <summary>
    /// ラベルの文字色です。
    /// </summary>
    protected virtual Color32 LabelColor => new Color32(255, 255, 255, 255);

    /// <summary>
    /// カードに書かれる所持者名です。
    /// </summary>
    protected virtual string HolderName => string.Empty;

    /// <summary>
    /// シリアル番号です。MetalCase / TaskForce のカードだけが表示します。
    /// </summary>
    protected virtual string SerialNumber => string.Empty;

    /// <summary>
    /// 摩耗の度合いです。Site02 / MetalCase のカードだけが表示します。
    /// </summary>
    protected virtual byte Wear => 0;

    /// <summary>
    /// 階級です。TaskForce のカードだけが表示します。
    /// </summary>
    protected virtual int Rank => 0;

    /// <summary>
    /// 見た目と権限を焼き直します。実行中に <see cref="Levels"/> などが変わったときに呼びます。
    /// </summary>
    public void Refresh()
    {
        customizedSerial = 0;
        Apply();
    }

    /// <summary>
    /// このカードを覗き込もうとしたときに呼ばれます。<c>ev.IsAllowed = false</c> で止められます。
    /// </summary>
    protected virtual void OnInspecting(PlayerInspectingKeycardEventArgs ev)
    {
    }

    /// <summary>
    /// このカードを手に持って発電機のロックを解除しようとしたときに呼ばれます。
    /// </summary>
    protected virtual void OnUnlockingGenerator(PlayerUnlockingGeneratorEventArgs ev)
    {
    }

    /// <inheritdoc/>
    protected override void Customize(Item item) => Apply();

    /// <inheritdoc/>
    protected override void Customize(Pickup pickup) => Apply();

    private void Apply()
    {
        if (customizedSerial == Serial) return;

        InventorySystem.Items.Keycards.KeycardItem item = Keycard?.Base;
        InventorySystem.Items.Keycards.KeycardPickup pickup = (Pickup as KeycardPickup)?.Base;

        if (item is null && pickup is null) return;

        CustomPermsDetail._customLevels = Levels;
        CustomPermsDetail._customColor = PermissionsColor;
        CustomTintDetail._customColor = Tint;
        CustomLabelDetail._customText = Label;
        CustomLabelDetail._customColor = LabelColor;
        CustomItemNameDetail._customText = Name;
        NametagDetail._customNametag = HolderName;
        CustomSerialNumberDetail._customVal = SerialNumber;
        CustomWearDetail._customWearLevel = Wear;
        CustomRankDetail._index = Rank;

        // 焼き付けはシリアルごとに 1 回しか行われないので、古い内容を捨ててから焼き直す。
        KeycardDetailSynchronizer.Database.Remove(Serial);

        if (item is not null)
            KeycardDetailSynchronizer.ServerProcessItem(item);
        else
            KeycardDetailSynchronizer.ServerProcessPickup(pickup);

        customizedSerial = Serial;
    }

    private static void Hook()
    {
        if (hooked) return;

        hooked = true;

        PlayerEvents.InspectingKeycard += OnAnyInspectingKeycard;
        PlayerEvents.UnlockingGenerator += OnAnyUnlockingGenerator;
        AntiMeme.Items.ItemRuntime.Register(() =>
        {
            PlayerEvents.InspectingKeycard -= OnAnyInspectingKeycard;
            PlayerEvents.UnlockingGenerator -= OnAnyUnlockingGenerator;
            hooked = false;
        });
    }

    private static void OnAnyInspectingKeycard(PlayerInspectingKeycardEventArgs ev)
    {
        if (Of(ev.KeycardItem.Serial) is CustomKeycard keycard)
            keycard.OnInspecting(ev);
    }

    private static void OnAnyUnlockingGenerator(PlayerUnlockingGeneratorEventArgs ev)
    {
        if (ev.Player.CurrentItem is { } held && Of(held.Serial) is CustomKeycard keycard)
            keycard.OnUnlockingGenerator(ev);
    }
}
