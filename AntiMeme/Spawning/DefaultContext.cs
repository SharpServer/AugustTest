using System;
using System.Collections.Generic;
using System.Linq;
using PlayerRoles;
using Sliced.API.Features;

using Logger = LabApi.Features.Console.Logger;

namespace AntiMeme.Spawning;

/// <summary>通常ラウンドの文脈です。波が持つ既定の重みをそのまま使います。</summary>
public sealed class DefaultContext : SpawnContext
{
    public override string Name => "Default";
}
