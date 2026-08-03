using System.Collections.Generic;
using LabApi.Features.Wrappers;

namespace Sliced.API.Features;

public abstract class CustomTeam
{
    public virtual string Name { get; protected set; }
    public virtual string Description { get; protected set; }
    public virtual string TeamColor { get; protected set; } = "#FFFFFF";
    
    public static List<Player> Members { get; private set; }
}