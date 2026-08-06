using UnityEngine;

namespace AntiMeme.Maps.Objects;

/// <summary>
/// お菓子の戦士が最後に落とす爆弾です。空から 150 秒かけて地上へ降りてきます。
/// </summary>
public sealed class CandyNuke : ObjectPrefab
{
    private const float DescentSeconds = 150f;

    private static readonly Vector3 Start = new Vector3(-90f, 500f, -45f);
    private static readonly Vector3 End = new Vector3(70f, 300f, -45f);

    /// <inheritdoc/>
    protected override string SchematicName => "Candy_Nuke";

    /// <inheritdoc/>
    public override bool FollowsMarker => false;

    /// <inheritdoc/>
    protected override void OnSetup()
    {
        Position = Start;
        Rotation = Quaternion.Euler(0f, 0f, 55f);

        MoveTo(Start, End, DescentSeconds);
    }
}
