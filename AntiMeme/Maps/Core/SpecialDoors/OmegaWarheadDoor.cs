using System;
using AntiMeme.Items.Keycards;

namespace AntiMeme.Maps.Core.SpecialDoors;

/// <summary>
/// Omega Warhead 区画への扉です。番号では開かず、専用のアクセス装置だけが通します。
/// </summary>
public sealed class OmegaWarheadDoor : SpecialDoor
{
    /// <inheritdoc/>
    public override string Marker => "OWJoin";

    /// <inheritdoc/>
    public override Type RequiredItem => typeof(OmegaWarheadAccess);
}
