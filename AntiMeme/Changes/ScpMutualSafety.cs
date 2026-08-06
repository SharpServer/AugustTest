using AntiMeme.Roles.Scps;
using AntiMeme.Teams.Factions;
using LabApi.Events.Arguments.Scp173Events;
using LabApi.Events.Handlers;
using PlayerRoles;
using Sliced.API.Features;

namespace AntiMeme.Changes;

/// <summary>
/// SCP 同士が足を引っ張り合わないようにします。
/// SCP-173 は仲間の SCP に見られても止まりません。
/// </summary>
public sealed class ScpMutualSafety : EventHandlerBase
{
    /// <inheritdoc/>
    public override void RegisterEvents() => Scp173Events.AddingObserver += OnAddingObserver;

    /// <inheritdoc/>
    public override void UnregisterEvents() => Scp173Events.AddingObserver -= OnAddingObserver;

    private static void OnAddingObserver(Scp173AddingObserverEventArgs ev)
    {
        bool observerIsScp = CustomTeam.Of(ev.Target) is ScpTeam;
        bool subjectIs173 = CustomRole.Of(ev.Player) is Scp173 || ev.Player.Role is RoleTypeId.Scp173;

        if (observerIsScp && subjectIs173)
            ev.IsAllowed = false;
    }
}
