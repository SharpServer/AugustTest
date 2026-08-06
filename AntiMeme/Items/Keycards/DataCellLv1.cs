using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using Sliced.API.Features;

namespace AntiMeme.Items.Keycards;

/// <summary>Access Tuner を Lv.1 に同期させるデータセルです。</summary>
public sealed class DataCellLv1 : DataCell
{
    /// <inheritdoc/>
    protected override int CellLevel => 1;
}
