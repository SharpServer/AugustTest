using LabApi.Features.Wrappers;
using Sliced.API.Features;
using UnityEngine;

namespace AntiMeme.Abilities;

/// <summary>
/// アビリティの見た目だけを担当する、寿命付きプリミティブの置き場です。
///
/// <para>
/// 後始末は <see cref="RoundScope"/> に預けます。旧実装は <c>Timing.CallDelayed</c> の中で
/// <c>try { toy.Destroy(); } catch { }</c> を書いていましたが、ラウンド再開で先に消えた玩具を
/// 掴んでいたのが原因なので、スコープに載せれば例外そのものが起きません。
/// </para>
/// </summary>
internal static class AbilityVisuals
{
    /// <summary>
    /// 当たり判定のない色付きプリミティブを 1 つ出して、指定秒後に消します。
    /// </summary>
    public static void Spawn(PrimitiveType type, Vector3 position, Vector3 scale, Color color, float lifetime)
    {
        // Customize してから Spawn する。Mirror の SpawnMessage はスケールを含むので、
        // この順なら UnSpawn → 変更 → Spawn を書かずに済む。
        PrimitiveObjectToy toy = PrimitiveObjectToy.Create(position, Quaternion.identity, scale, networkSpawn: false);

        toy.Type = type;
        toy.Color = color;
        toy.Flags = AdminToys.PrimitiveFlags.Visible;
        toy.Spawn();

        RoundScope.Current.Delay(lifetime, toy.Destroy);
    }
}
