using System;
using System.Collections.Generic;
using AntiMeme.Items.Keycards;
using Sliced.API.Features;

namespace AntiMeme.Maps.Objects;

/// <summary>Access Tuner と Data Cell を一つのマップモデルから取り出す箱です。</summary>
public sealed class AccessTunerBox : ObjectPrefab
{
    private readonly HashSet<string> consumed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private bool setup;

    public bool SpawnAccessTuner { get; set; } = true;
    public int SpawnAccessTunerLevel { get; set; }
    public bool SpawnLeftDataCell { get; set; } = true;
    public bool SpawnRightDataCell { get; set; } = true;
    public int SpawnLeftDataCellLevel { get; set; } = 1;
    public int SpawnRightDataCellLevel { get; set; } = 1;
    protected override string SchematicName => "AccessTunerCase";

    protected override void OnSetup()
    {
        Bind("AccessTuner", () => SpawnTuner(SpawnAccessTunerLevel), "AccessTunerModel");
        Bind("LeftSide", () => SpawnCell(SpawnLeftDataCellLevel), "LeftSideModel");
        Bind("RightSide", () => SpawnCell(SpawnRightDataCellLevel), "RightSideModel");
        setup = true;
        ApplyVisibility();
    }

    protected override void OnDestroy() => consumed.Clear();

    private void Bind(string key, Func<CustomItem> create, string model)
    {
        InteractableHandle handle = GetInteractable(key);
        if (handle is null) return;
        handle.Interacted += ev =>
        {
            if (!handle.Enabled || ev?.Player is not { IsDestroyed: false } player) return;
            CustomItem item = create();
            if (item is null) return;
            CustomItem.Give(item.GetType(), player);
            consumed.Add(key);
            ApplyVisibility();
        };
    }

    private CustomItem SpawnTuner(int level) => level switch
    {
        1 => new AccessTunerLv1(),
        2 => new AccessTunerLv2(),
        3 => new AccessTunerLv3(),
        _ => new AccessTunerBroken(),
    };

    private CustomItem SpawnCell(int level) => level switch
    {
        2 => new DataCellLv2(),
        3 => new DataCellLv3(),
        _ => new DataCellLv1(),
    };

    private void ApplyVisibility()
    {
        if (!setup && !IsAlive) return;
        ApplyPart("AccessTuner", SpawnAccessTuner, "AccessTunerModel");
        ApplyPart("LeftSide", SpawnLeftDataCell, "LeftSideModel");
        ApplyPart("RightSide", SpawnRightDataCell, "RightSideModel");
    }

    private void ApplyPart(string key, bool configured, string model)
    {
        bool visible = configured && !consumed.Contains(key);
        if (GetInteractable(key) is { } handle) handle.Enabled = visible;
        SetBlockVisible(key, visible);
        SetBlockVisible(model, visible);
    }
}
