using CustomPlayerEffects;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using MapGeneration;
using UnityEngine;

namespace AntiMeme.Maps.Objects;

/// <summary>
/// ターミナル・リフト。HCZ 試験室の端末を操作すると、床ごと下の階層へ降りて戻ってきます。
/// </summary>
/// <remarks>
/// 旧実装は静的クラスに「リフト本体・操作端末の一覧・進行フラグ・タイムアウトハンドル」を
/// 並べ、施設ハンドラ側から差し込む作りでした。降りるのはこのスキマティック自身なので、
/// 状態も動作もオブジェクトが持てば足ります。
/// </remarks>
public sealed class TerminalRift : ObjectPrefab
{
    private const float Descent = 28.5f;
    private const float TravelSeconds = 12.5f;
    private const float HoldSeconds = 7.5f;
    private const float AudioRange = 30f;

    private Vector3 top;
    private bool moving;

    /// <inheritdoc/>
    protected override string SchematicName => "Rift";

    /// <inheritdoc/>
    protected override void OnSetup()
    {
        top = Position;

        PlayerEvents.SearchedToy += OnSearchedToy;

        // 降下中は落下死判定が入るので、試験室と地上では無効化する。
        PlayerEvents.UpdatingEffect += OnUpdatingEffect;
    }

    /// <inheritdoc/>
    protected override void OnDestroy()
    {
        PlayerEvents.SearchedToy -= OnSearchedToy;
        PlayerEvents.UpdatingEffect -= OnUpdatingEffect;
    }

    /// <summary>試験室の端末ならどれを触っても動きます。</summary>
    private void OnSearchedToy(PlayerSearchedToyEventArgs ev)
    {
        if (ev.Player?.Room?.Name != RoomName.HczTestroom) return;

        Descend();
    }

    /// <summary>
    /// 床が下がることで発生する落下死を止めます。
    /// 試験室と地上以外で落ちた場合は、リフトとは無関係なので通します。
    /// </summary>
    private static void OnUpdatingEffect(PlayerEffectUpdatingEventArgs ev)
    {
        if (ev.Effect is not PitDeath) return;

        RoomName? room = ev.Player?.Room?.Name;

        if (room is null or RoomName.HczTestroom or RoomName.Outside)
            ev.IsAllowed = false;
    }

    private void Descend()
    {
        if (moving) return;

        moving = true;

        Play("Moving.ogg");

        MoveTo(top, top + Vector3.down * Descent, TravelSeconds, () =>
        {
            Play("Beep.ogg");

            Delay(HoldSeconds, () =>
            {
                Play("Moving.ogg");

                MoveTo(Position, top, TravelSeconds, () =>
                {
                    Play("Beep.ogg");
                    moving = false;
                });
            });
        });
    }

    private void Play(string clip) => MapAudio.Play(clip, "TerminalRift", Position, maxDistance: AudioRange);
}
