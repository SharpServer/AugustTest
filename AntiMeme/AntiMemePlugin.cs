using System;
using LabApi.Features;
using LabApi.Loader.Features.Plugins;

namespace AntiMeme;

public class AntiMemePlugin : Plugin<Config>
{
    public override string Name => "AntiMeme";
    public override string Description => "Nyaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    public override string Author => "org.sharp-server.jp.scpsl";
    public override Version RequiredApiVersion => new(LabApiProperties.CompiledVersion);
    public override void Enable()
    {
        
    }

    public override void Disable()
    {
        
    }
}