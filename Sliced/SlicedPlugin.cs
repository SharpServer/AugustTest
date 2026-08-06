using System;
using LabApi.Features;
using Sliced.API.Features;

using Plugin = LabApi.Loader.Features.Plugins.Plugin;

namespace Sliced;

/// <summary>
/// Sliced 本体。EventHandlerBase の派生クラスの自動登録を起動・停止するだけのプラグインです。
/// </summary>
public class SlicedPlugin : Plugin
{
    public override string Name => "Sliced";

    public override string Description => "Meow-nya";

    public override string Author => "org.sharp-server.jp.scpsl";

    public override Version RequiredApiVersion => new(LabApiProperties.CompiledVersion);

    public override void Enable()
    {
        EventHandlerRegistry.Initialize();
    }

    public override void Disable()
    {
        EventHandlerRegistry.Shutdown();
        GameMode.Shutdown();
        CustomRole.Shutdown();
        CustomItem.Shutdown();
        PlayerScope.Shutdown();
        RoundScope.Shutdown();
    }
}
