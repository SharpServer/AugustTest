namespace AntiMeme.Maps.Core.SpecialDoors;

/// <summary>隔離区画の扉です。コードは施設内の報告書に書かれています。</summary>
public sealed class SecureQuartersDoor : SpecialDoor
{
    /// <inheritdoc/>
    public override string Marker => "SQ_Door";

    /// <inheritdoc/>
    public override string Code => "0727";
}
