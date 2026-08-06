using System;
using System.Linq;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Features.Wrappers;
using UnityEngine;

namespace AntiMeme.Maps.Objects;

/// <summary>Terminal の列車一両ぶん。移動サイクルは TerminalTrain が管理します。</summary>
public sealed class TerminalTrainCar : ObjectPrefab
{
    protected override string SchematicName => "STrain";
    protected override float SetupDelay => 0f;
}
