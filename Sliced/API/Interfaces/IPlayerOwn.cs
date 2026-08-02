using LabApi.Features.Wrappers;

namespace Sliced.API.Interfaces;

public interface IPlayerOwn
{
    public Player Player { get; }
    public ReferenceHub ReferenceHub => Player.ReferenceHub;
}