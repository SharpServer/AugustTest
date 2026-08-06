using System;
using System.Collections.Generic;
using System.Linq;
using Interactables.Interobjects.DoorUtils;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using Sliced.API.Features;
using UnityEngine;

namespace AntiMeme.Maps.Features;

/// <summary>
/// 施設管理者制御室のコンソールです。
///
/// <para>
/// 操作は 3 手です。キーカードを手に持ってコンソールを調べると<b>セッション開始</b>、
/// そのキーカードを投げると<b>機能の切り替え</b>、もう一度コンソールを調べると<b>実行</b>。
/// 別のアイテムに持ち替えるとセッションは終わります。
/// </para>
/// </summary>
/// <remarks>
/// 機能は <see cref="FacilityControlRoomFunction"/> を継承するだけで載ります。
/// 旧実装は文字列 <c>Id</c> で機能を引き、実行回数とクールダウンをその文字列でキーにした
/// 辞書 2 本に持っていましたが、機能インスタンス自体をキーにすれば辞書は 1 本で足ります。
/// </remarks>
public sealed class FacilityControlRoom : MapFeature
{
    private const float ConsoleRadius = 3f;
    private const float SessionMaxDistance = 4.5f;
    private const float HintInterval = 0.8f;

    /// <summary>マーカーが置かれていないマップ向けの既定座標です。</summary>
    private static readonly Vector3 DefaultConsolePosition = new Vector3(107.921f, 296.313f, -68.748f);

    /// <summary>実装されている機能の一覧です。継承するだけで並びます。</summary>
    private static readonly IReadOnlyList<FacilityControlRoomFunction> Functions =
        TypeParser.FindTypes<FacilityControlRoomFunction>()
            .Select(type => (FacilityControlRoomFunction)Activator.CreateInstance(type))
            .OrderBy(function => function.Order)
            .ThenBy(function => function.DisplayName, StringComparer.Ordinal)
            .ToArray();

    private readonly Dictionary<Player, Session> sessions = new Dictionary<Player, Session>();

    private readonly Dictionary<FacilityControlRoomFunction, State> states =
        new Dictionary<FacilityControlRoomFunction, State>();

    /// <inheritdoc/>
    public override void RegisterEvents()
    {
        PlayerEvents.SearchedToy += OnSearchedToy;
        PlayerEvents.DroppingItem += OnDroppingItem;
        PlayerEvents.ChangedItem += OnChangedItem;
        PlayerEvents.Left += OnLeft;
    }

    /// <inheritdoc/>
    public override void UnregisterEvents()
    {
        PlayerEvents.SearchedToy -= OnSearchedToy;
        PlayerEvents.DroppingItem -= OnDroppingItem;
        PlayerEvents.ChangedItem -= OnChangedItem;
        PlayerEvents.Left -= OnLeft;
    }

    /// <inheritdoc/>
    protected override void Reset()
    {
        sessions.Clear();
        states.Clear();

        foreach (FacilityControlRoomFunction function in Functions)
            function.ResetState();
    }

    private static Vector3 ConsolePosition =>
        MapPoints.TryGet("AntiMemeButton", out Vector3 marker) ? marker : DefaultConsolePosition;

    private void OnSearchedToy(PlayerSearchedToyEventArgs ev)
    {
        if (Functions.Count == 0 || ev.Interactable is null) return;
        if ((ev.Interactable.Position - ConsolePosition).sqrMagnitude >= ConsoleRadius * ConsoleRadius) return;

        if (sessions.TryGetValue(ev.Player, out Session session))
        {
            Execute(ev.Player, session);

            return;
        }

        Start(ev.Player);
    }

    /// <summary>
    /// 差してあるキーカードを投げると機能が 1 つ進みます。落とすことはできません。
    /// </summary>
    private void OnDroppingItem(PlayerDroppingItemEventArgs ev)
    {
        if (!sessions.TryGetValue(ev.Player, out Session session)) return;
        if (ev.Item is null || ev.Item.Serial != session.KeycardSerial) return;

        ev.IsAllowed = false;
        session.Index = (session.Index + 1) % Functions.Count;
        session.Notice = null;

        ShowMenu(ev.Player, session);
    }

    /// <summary>差してあるキーカードから持ち替えたらセッション終了です。</summary>
    private void OnChangedItem(PlayerChangedItemEventArgs ev)
    {
        if (!sessions.TryGetValue(ev.Player, out Session session)) return;
        if (ev.NewItem is { } item && item.Serial == session.KeycardSerial) return;

        End(ev.Player, "<size=24>制御室操作を終了しました。\nキーカードから持ち替えました。</size>");
    }

    private void OnLeft(PlayerLeftEventArgs ev) => End(ev.Player, null);

    private void Start(Player player)
    {
        if (player.CurrentItem is not KeycardItem keycard)
        {
            player.SendHint("<size=24>管理権限を持つキーカードを手に持ってください。</size>", 3.5f);

            return;
        }

        Session session = new Session { KeycardSerial = keycard.Serial };
        sessions[player] = session;

        ShowMenu(player, session);

        // 退出・死亡・ラウンド再開のいずれでもスコープが閉じるので、このループは取り残されない。
        PlayerScope.Of(player).RunLoop(HintInterval, target =>
        {
            if (!sessions.TryGetValue(target, out Session current) || !ReferenceEquals(current, session)) return;

            if ((target.Position - ConsolePosition).sqrMagnitude > SessionMaxDistance * SessionMaxDistance)
            {
                End(target, "<size=24>制御室操作を終了しました。\nコンソールから離れました。</size>");

                return;
            }

            ShowMenu(target, current);
        });
    }

    private void End(Player player, string hint)
    {
        if (!sessions.Remove(player)) return;

        if (!string.IsNullOrEmpty(hint))
            player.SendHint(hint, 3.5f);
    }

    private void Execute(Player player, Session session)
    {
        if (player.CurrentItem is not KeycardItem keycard || keycard.Serial != session.KeycardSerial)
        {
            End(player, "<size=24>制御室操作を終了しました。\nキーカードが差さっていません。</size>");

            return;
        }

        FacilityControlRoomFunction function = Functions[session.Index];
        State state = StateOf(function);

        if (!HasPermissions(keycard, function.RequiredPermissions))
        {
            session.Notice = $"<color=#ff5555>{function.DisplayName} を実行する権限がありません。</color>";

            return;
        }

        if (function.MaxExecutions > 0 && state.Count >= function.MaxExecutions)
        {
            session.Notice =
                $"<color=#ff5555>{function.DisplayName} は使用回数上限です ({state.Count}/{function.MaxExecutions})。</color>";

            return;
        }

        float remaining = state.ReadyAt - Time.time;

        if (remaining > 0f)
        {
            session.Notice = $"<color=#ff5555>{function.DisplayName} はクールダウン中です。残り {remaining:F0} 秒。</color>";

            return;
        }

        FacilityControlRoomFunctionResult result =
            function.Execute(new FacilityControlRoomFunctionContext(player, keycard, state.Count));

        if (result.CountAsExecution)
        {
            state.Count++;
            state.ReadyAt = function.Cooldown > 0f ? Time.time + function.Cooldown : 0f;
        }

        session.Notice = result.Hint;
    }

    private State StateOf(FacilityControlRoomFunction function)
    {
        if (states.TryGetValue(function, out State state)) return state;

        state = new State();
        states[function] = state;

        return state;
    }

    private static bool HasPermissions(KeycardItem keycard, DoorPermissionFlags required) =>
        required == DoorPermissionFlags.None ||
        (keycard.Base.GetPermissions(null) & required) == required;

    private void ShowMenu(Player player, Session session)
    {
        FacilityControlRoomFunction function = Functions[session.Index];
        string notice = string.IsNullOrEmpty(session.Notice) ? string.Empty : $"\n{session.Notice}";

        player.SendHint(
            $"<size=24>施設管理者制御室  [{session.Index + 1}/{Functions.Count}]\n" +
            $"<b>{function.DisplayName}</b></size>\n" +
            $"<size=20>{function.Description}</size>\n" +
            $"<size=18>キーカードを投げる: 切り替え / コンソールを調べる: 実行</size>{notice}",
            HintInterval + 0.3f);
    }

    /// <summary>1 人ぶんの操作状態です。</summary>
    private sealed class Session
    {
        public ushort KeycardSerial;

        public int Index;

        /// <summary>直近の実行結果です。次の切り替えまで出し続けます。</summary>
        public string Notice;
    }

    /// <summary>1 機能ぶんの実行回数とクールダウンです。</summary>
    private sealed class State
    {
        public int Count;

        public float ReadyAt;
    }
}
