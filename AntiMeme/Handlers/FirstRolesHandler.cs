using AntiMeme.SpawnSets;
using Sliced.API.Features;

namespace AntiMeme.Handlers;

public class FirstRolesHandler : EventHandlerBase
{
    public override void OnServerRoundStarted()
    {
        new FirstRolesSetSCPs().Spawn();
        new FirstRolesSetNormals().Spawn();
    }
}