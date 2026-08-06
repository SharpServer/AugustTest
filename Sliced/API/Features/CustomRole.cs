using System;
using System.Collections.Generic;
using System.Reflection;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using PlayerRoles;
using PlayerRoles.PlayableScps.HumeShield;
using Sliced.API.Interfaces;
using Sliced.API.Structs;
using UnityEngine;

using Logger = LabApi.Features.Console.Logger;

namespace Sliced.API.Features;

/// <summary>
/// カスタム役職です。<b>プレイヤー 1 人につき 1 インスタンス</b>生成されます。
/// フィールドをそのまま per-player 状態として使えるので、
/// プレイヤー ID をキーにした static 辞書を自分で用意する必要はありません。
///
/// 何を持たせるかは <c>public override</c> を数行書くだけで宣言できます。
/// 文字列の識別キーも専用 enum もありません。型そのものが同一性です。
/// </summary>
/// <example>
/// <code>
/// public sealed class ChaosSniper : CustomRole
/// {
///     public override string Name     => "Chaos Sniper";
///     public override CustomTeam Team => CustomTeam.Get&lt;ChaosTeam&gt;();
///     public override RoleTypeId BaseRole => RoleTypeId.ChaosRifleman;
///
///     public override IReadOnlyList&lt;ItemType&gt; Items =>
///     [
///         ItemType.GunE11SR,
///         ItemType.ArmorCombat,
///     ];
///
///     public override IReadOnlyDictionary&lt;ItemType, ushort&gt; Ammo =>
///         new Dictionary&lt;ItemType, ushort&gt; { [ItemType.Ammo556x45] = 120 };
///
///     protected override void OnSpawned()
///     {
///         // Scope は退出・ラウンド再開・役職変更で自動的に閉じる
///         Scope.RunLoop(5f, p => p.SendHint("狙撃手として動け"));
///     }
/// }
/// </code>
/// </example>
public abstract class CustomRole : IPlayerOwn
{
    private static readonly Dictionary<Player, CustomRole> ActiveRoles = new Dictionary<Player, CustomRole>();

    private static bool hooked;

    // ShowStatus の置き場。表示層は Status を読むだけなので、ここに持たせて構わない。
    private string status;
    private float statusExpiresAt;

    /// <summary>
    /// この役職を持っているプレイヤーです。<see cref="Spawn(Player)"/> で設定されます。
    /// </summary>
    public Player Player { get; private set; }

    /// <summary>
    /// このカスタム役職へ切り替わる直前のバニラ役職です。
    /// 改宗解除など、元の状態へ戻す必要がある役職が参照します。
    /// </summary>
    public RoleTypeId PreviousRole { get; private set; } = RoleTypeId.None;

    /// <summary>
    /// 表示名です。
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// 説明文です。既定のヒント表示で使われます。
    /// </summary>
    public virtual string Description => string.Empty;

    /// <summary>
    /// 所属チームです。勝利判定に使われます。インスタンスで指してください。
    /// </summary>
    public virtual CustomTeam Team => null;

    /// <summary>
    /// 土台になるバニラ役職です。
    /// </summary>
    public abstract RoleTypeId BaseRole { get; }

    /// <summary>
    /// スポーン位置です。null ならバニラのスポーン地点を使います。
    /// </summary>
    public virtual Vector3? SpawnPosition => null;

    /// <summary>
    /// スポーン時の向きです。null なら触りません。
    /// </summary>
    public virtual Quaternion? SpawnRotation => null;

    /// <summary>
    /// 体の大きさです。null なら等倍のままです。
    /// 役職が外れるときに等倍へ戻します。
    /// </summary>
    public virtual Vector3? Scale => null;

    /// <summary>
    /// 最大 HP です。null ならバニラのままです。
    /// </summary>
    public virtual float? MaxHealth => null;

    /// <summary>
    /// 支給アイテムです。1 つでも指定するとバニラの初期装備は付きません。
    /// </summary>
    public virtual IReadOnlyList<ItemType> Items => [];

    /// <summary>
    /// 支給するカスタムアイテムの型です。同じ型を複数並べれば、その個数だけ新規生成します。
    /// 文字列キーや ID ではなく <c>typeof(MyItem)</c> で指定してください。
    /// </summary>
    public virtual IReadOnlyList<Type> CustomItems => [];

    /// <summary>
    /// 支給する予備弾薬です。
    /// </summary>
    public virtual IReadOnlyDictionary<ItemType, ushort> Ammo => EmptyAmmo;

    /// <summary>
    /// スポーン時に付与する効果です。
    /// </summary>
    public virtual IReadOnlyList<RoleEffect> Effects => [];

    /// <summary>
    /// ネームプレートに出す追加情報です。null なら変更しません。
    /// </summary>
    public virtual string CustomInfo => null;

    /// <summary>
    /// HUD の役職欄に出す整形済みの文字列です。
    /// 既定はチーム色を巻いた <see cref="Name"/> なので、
    /// 部隊ごとに色や太字を変える役職だけ override します。
    /// </summary>
    public virtual string HudLabel => $"<color={Team?.Color ?? "#ffffff"}>{Name}</color>";

    /// <summary>
    /// HUD の陣営欄に出す文字列です。null ならチームが名乗るものを使います。
    /// 2 つの陣営にまたがる役職 (SCP-076・SCP-3005・Omega-1 など) だけ override します。
    /// </summary>
    public virtual string TeamLabel => null;

    /// <summary>
    /// HUD の目的欄に出す文字列です。null なら陣営の既定を使います。
    /// </summary>
    public virtual string Objective => null;

    /// <summary>
    /// HUD の役職固有欄に出す、いま見せたい状態です。null / 空なら何も出しません。
    /// </summary>
    /// <remarks>
    /// <para>
    /// ネームプレート (<see cref="CustomInfo"/>) とは別物です。あちらは他人が見る名札で、
    /// こちらは本人 (と観戦者) が見る計器です。
    /// </para>
    /// <para>
    /// 既定では <see cref="ShowStatus"/> で置かれた値を、その有効時間の間だけ返します。
    /// 押し込まずに毎回組み立てたい役職は override してください。
    /// HUD ループから毎秒読まれるので、<b>その場合は読むだけにしてください。</b>
    /// </para>
    /// </remarks>
    public virtual string Status => Time.time < statusExpiresAt ? status : null;

    /// <summary>
    /// このプレイヤーに紐づくコルーチン置き場です。
    /// 役職が外れた時点でまとめて止まります。
    /// </summary>
    protected PlayerScope Scope => PlayerScope.Of(Player);

    private static readonly Dictionary<ItemType, ushort> EmptyAmmo = new Dictionary<ItemType, ushort>();

    /// <summary>
    /// 指定プレイヤーの現在のカスタム役職です。持っていなければ null。
    /// </summary>
    public static CustomRole Of(Player player)
    {
        if (player is not null && ActiveRoles.TryGetValue(player, out CustomRole role))
            return role;

        return null;
    }

    /// <summary>
    /// 指定プレイヤーが T の役職かどうか。
    /// </summary>
    public static bool Is<T>(Player player) where T : CustomRole => Of(player) is T;

    /// <summary>
    /// 現在有効なカスタム役職の一覧です。
    /// </summary>
    public static IReadOnlyCollection<CustomRole> Active => ActiveRoles.Values;

    /// <summary>
    /// 指定したアセンブリで定義された役職だけを解除します。
    /// </summary>
    public static void RemoveAssembly(Assembly assembly)
    {
        if (assembly is null) return;

        foreach (KeyValuePair<Player, CustomRole> entry in new List<KeyValuePair<Player, CustomRole>>(ActiveRoles))
        {
            if (entry.Value.GetType().Assembly == assembly)
                Remove(entry.Key);
        }
    }

    /// <summary>
    /// 全役職を解除し、静的イベント購読を解除します。
    /// </summary>
    public static void Shutdown()
    {
        OnRoundRestarted();

        if (!hooked) return;

        PlayerEvents.ChangedRole -= OnChangedRole;
        PlayerEvents.Left -= OnLeft;
        ServerEvents.RoundRestarted -= OnRoundRestarted;
        hooked = false;
    }

    /// <summary>
    /// 新しいインスタンスを作って付与します。呼び出しごとに別インスタンスになります。
    /// </summary>
    public static T Spawn<T>(Player player) where T : CustomRole, new()
    {
        T role = new T();

        return role.Spawn(player) ? role : null;
    }

    /// <summary>
    /// 実行時に決まった型の役職を付与します。スポーンセットや脱出規則など、
    /// 外部データではなく型を値として持ち回る経路で使います。
    /// </summary>
    public static CustomRole Spawn(Type type, Player player)
    {
        if (type is null || !typeof(CustomRole).IsAssignableFrom(type) || type.IsAbstract)
        {
            Logger.Error($"[Sliced] {type?.FullName ?? "null"} はカスタム役職として生成できません。");

            return null;
        }

        try
        {
            CustomRole role = (CustomRole)Activator.CreateInstance(type);

            return role.Spawn(player) ? role : null;
        }
        catch (Exception exception)
        {
            Logger.Error($"[Sliced] カスタム役職 {type.FullName} の生成に失敗しました: {exception}");

            return null;
        }
    }

    /// <summary>
    /// このプレイヤーの役職を解除します。
    /// </summary>
    public static void Remove(Player player)
    {
        if (player is null || !ActiveRoles.TryGetValue(player, out CustomRole role)) return;

        ActiveRoles.Remove(player);
        PlayerScope.Close(player);

        // 基底が書き換えたバニラ状態は基底が戻す。
        // ここに書いていないもの (体力・所持品・効果) は役職変更時にゲーム側が作り直す。
        if (player is { IsDestroyed: false })
        {
            if (role.Scale is not null)
                player.Scale = Vector3.one;

            if (role.CustomInfo is not null)
                player.CustomInfo = string.Empty;
        }

        try
        {
            role.OnRemoved();
        }
        catch (Exception exception)
        {
            Logger.Error($"[Sliced] {role.GetType().Name} の OnRemoved で例外が発生しました: {exception}");
        }
    }

    /// <summary>
    /// この役職をプレイヤーに付与します。
    /// </summary>
    public bool Spawn(Player player) => Spawn(player, out _);

    /// <summary>
    /// この役職をプレイヤーに付与します。失敗したときは理由を返します。
    /// </summary>
    /// <remarks>
    /// 失敗を bool 1 個で返すと、呼び出し側は「対象が悪いのか、役職が悪いのか、
    /// 役職変更を誰かに止められたのか」を区別できません。
    /// コマンドやマップから叩く経路のために理由を残します。
    /// </remarks>
    public bool Spawn(Player player, out string failure)
    {
        failure = null;

        if (player is not { IsDestroyed: false })
        {
            failure = "対象が存在しません。";

            return false;
        }

        if (!player.IsReady)
        {
            failure = $"{player.Nickname} はまだ参加処理が終わっていません (認証待ち)。";

            return false;
        }

        PreviousRole = player.Role;

        // 以前の役職があれば先に片付ける。
        Remove(player);

        Player = player;

        // バニラに任せる分だけフラグを立てる。
        // 装備を自分で配るならバニラの初期装備は要らないし、
        // 位置を指定するならバニラのスポーン地点も要らない。
        RoleSpawnFlags flags = RoleSpawnFlags.None;

        if (Items.Count == 0 && CustomItems.Count == 0)
            flags |= RoleSpawnFlags.AssignInventory;

        if (!SpawnPosition.HasValue)
            flags |= RoleSpawnFlags.UseSpawnpoint;

        player.SetRole(BaseRole, RoleChangeReason.RemoteAdmin, flags);

        if (player.Role != BaseRole)
        {
            // ServerSetRole は PlayerChangingRole が拒否されると何もせずに戻る。
            // 役職が変わっていない = 誰かに止められた、というのがほぼ唯一の筋。
            failure =
                $"{BaseRole} への役職変更が拒否されました " +
                $"(現在: {player.Role})。他のハンドラが PlayerChangingRole を止めている可能性があります。";

            Player = null;

            return false;
        }

        ActiveRoles[player] = this;
        Hook();

        Apply();

        try
        {
            OnSpawned();
            ShowHint();
        }
        catch (Exception exception)
        {
            Logger.Error($"[Sliced] {GetType().Name} のスポーン処理で例外が発生しました: {exception}");
        }

        return true;
    }

    /// <summary>
    /// スポーン記述を実際に反映します。
    /// </summary>
    private void Apply()
    {
        if (SpawnPosition is { } position)
            Player.Position = position;

        if (SpawnRotation is { } rotation)
            Player.Rotation = rotation;

        if (Scale is { } scale)
            Player.Scale = scale;

        if (MaxHealth is { } maxHealth)
        {
            Player.MaxHealth = maxHealth;
            Player.Health = maxHealth;
        }

        if (Items.Count > 0 || CustomItems.Count > 0)
        {
            Player.ClearInventory();

            foreach (ItemType item in Items)
            {
                Player.AddItem(item);
            }

            foreach (Type itemType in CustomItems)
            {
                CustomItem.Give(itemType, Player);
            }
        }

        foreach (KeyValuePair<ItemType, ushort> ammo in Ammo)
        {
            Player.SetAmmo(ammo.Key, ammo.Value);
        }

        foreach (RoleEffect effect in Effects)
        {
            effect.ApplyTo(Player);
        }

        if (CustomInfo is { } info)
            Player.CustomInfo = info;
    }

    /// <summary>
    /// スポーン完了後に呼ばれます。能力やループ処理はここで <see cref="Scope"/> に載せてください。
    /// </summary>
    protected virtual void OnSpawned()
    {
    }

    /// <summary>
    /// 役職が外れたときに呼ばれます。
    /// <see cref="Scope"/> に載せたものは自動で止まるので、ここに書く必要はありません。
    /// </summary>
    protected virtual void OnRemoved()
    {
    }

    /// <summary>
    /// スポーン時に説明を出しておく秒数です。
    /// </summary>
    protected virtual float HintDuration => 5f;

    /// <summary>
    /// 役職の説明を本人に見せます。
    /// </summary>
    protected virtual void ShowHint()
    {
        Player.SendHint($"<size=24>{Name}\n{Description}</size>", HintDuration);
    }

    /// <summary>
    /// このイベントの対象が自分かどうか。
    /// </summary>
    protected bool IsMine(Player other) => other is not null && IsMine(other.ReferenceHub);

    /// <summary>
    /// LabApi に相当イベントが無く EXILED 側を使う箇所のために、ReferenceHub でも比べられるようにします。
    /// </summary>
    protected bool IsMine(ReferenceHub hub) =>
        Player is { IsDestroyed: false } && ReferenceEquals(hub, Player.ReferenceHub);

    /// <summary>
    /// 役職が続くあいだだけイベントを購読します。解除は <see cref="Scope"/> が引き受けるので、
    /// 死亡・役職変更・退出・ラウンド再開のどれでも取り残されません。
    /// </summary>
    protected void Hook(Action subscribe, Action unsubscribe)
    {
        subscribe();
        Scope.OnDispose(_ => unsubscribe());
    }

    /// <summary>
    /// 常時更新するステータス行 (残り時間・レベルなど) を出します。
    /// 説明を出す <see cref="ShowHint"/> とは用途が別です。
    /// </summary>
    /// <remarks>
    /// 中央にヒントを飛ばすのではなく、<see cref="Status"/> に置くだけです。
    /// 画面のどこへ出すかは表示層の都合なので、役職側は「いま何を出したいか」だけを言います。
    /// <paramref name="duration"/> を過ぎると <see cref="Status"/> は null に戻るので、
    /// 更新が止まった役職の古い値が残りません。
    /// </remarks>
    protected void ShowStatus(string text, float duration)
    {
        status = text;
        statusExpiresAt = Time.time + Mathf.Max(0f, duration);
    }

    /// <summary>
    /// ヒュームシールドの最大値を決めて満タンにします。
    /// </summary>
    protected void SetHumeShield(float max)
    {
        Player.MaxHumeShield = max;
        Player.HumeShield = max;
    }

    /// <summary>
    /// ヒュームシールドの回復速度に倍率を掛けます。
    ///
    /// LabApi の <c>HumeShieldRegenRate</c> setter は <see cref="DynamicHumeShieldController"/> へ
    /// 無条件キャストするので、型を確かめてから直接書きます。
    /// </summary>
    protected void BoostHumeShieldRegen(float multiplier)
    {
        if (Player.RoleBase is IHumeShieldedRole { HumeShieldModule: DynamicHumeShieldController controller })
            controller.RegenerationRate *= multiplier;
    }

    private static void Hook()
    {
        if (hooked) return;

        hooked = true;

        PlayerEvents.ChangedRole += OnChangedRole;
        PlayerEvents.Left += OnLeft;
        ServerEvents.RoundRestarted += OnRoundRestarted;
    }

    private static void OnChangedRole(PlayerChangedRoleEventArgs ev)
    {
        // 自分が付けた役職から別の役職に変わったら解除する。
        if (Of(ev.Player) is { } role && ev.NewRole.RoleTypeId != role.BaseRole)
        {
            Remove(ev.Player);
        }
    }

    private static void OnLeft(PlayerLeftEventArgs ev) => Remove(ev.Player);

    private static void OnRoundRestarted()
    {
        foreach (Player player in new List<Player>(ActiveRoles.Keys))
        {
            Remove(player);
        }

        ActiveRoles.Clear();
    }
}
