using System.Collections.Generic;
using System.Linq;
using AntiMeme.Audio;
using AntiMeme.Hud;
using AntiMeme.Items.Armor;
using AntiMeme.Items.Utility;
using AntiMeme.Items.Weapons;
using AntiMeme.Maps;
using AntiMeme.Net;
using AntiMeme.Teams;
using AntiMeme.Teams.Factions;
using CustomPlayerEffects;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using MEC;
using PlayerRoles;
using PlayerStatsSystem;
using Sliced.API.Features;
using UnityEngine;
using Utils;

using ExiledNpc = Exiled.API.Features.Npc;
using ExiledPlayer = Exiled.API.Features.Player;
using Logger = LabApi.Features.Console.Logger;
using Random = UnityEngine.Random;

namespace AntiMeme.GameModes.Modes.Dante;

/// <summary>
/// 「DANTE ─ 業火の指揮者」。手動起動の多段ボス戦です。
///
/// <para>
/// <b>戦闘ロジックは旧 <c>DanteEvent</c> のままです。</b>
/// 幕の進行条件 (HP 66% / 33%)・各幕の攻撃抽選・跳躍確率・酸沼と触手の寸法・
/// 仮想 HP の配分 (2500 + 1200 × 人数)・中央触手の本数と耐久は仕様がコードにしか無いので
/// 数値も分岐もそのまま移しています。載せ替えたのは外側だけです。
/// </para>
/// <list type="bullet">
/// <item>世代カウンタ (<c>EventPID</c>) → <see cref="GameMode.IsCanceled"/></item>
/// <item>コルーチンの手動 Kill → <see cref="GameMode.Scope"/> (ラウンド終了で自動停止)</item>
/// <item>役職配布 → <see cref="DanteBoss"/> / <see cref="DanteSlayer"/> / <see cref="DanteTentacle"/></item>
/// <item>独自勝利グループ → <see cref="DanteTeam"/> (ボス健在の間はラウンドが終わらない)</item>
/// <item>遅延内の <c>Player.Get(id)</c> 再取得 → 生成時に位置を渡すだけ</item>
/// </list>
/// </summary>
public sealed class DanteBattle : RaidMode
{
    private const string SchematicName = "Dante";
    private const string ThemeFile = "dante.ogg";
    private const string ThemeSpeaker = "DanteTheme";

    /// <summary>スキマティックと触手の見た目サイズ。当たり判定 (NPC 本体) とは独立です。</summary>
    private const float VisualScale = 2.6f;

    private const int TentacleCount = 8;
    private const int TentacleSegments = 6;
    private static readonly Color TentacleColor = new Color(.30f, .85f, .12f, 1f);

    /// <summary>増援の最大回数と間隔。無限には湧かず、いずれ自力で討つことになります。</summary>
    private const int MaxWaves = 4;

    private const float WaveInterval = 30f;

    private const int MaxPuddles = 8;
    private const float PuddleLifetime = 6f;
    private static readonly Color PuddleColor = new Color(.20f, .70f, .05f, .9f);

    /// <summary>触手が地形に埋まって到達不能でも必ず無敵を解除する時間 (ソフトロック防止)。</summary>
    private const float ShieldTimeout = 25f;

    private static readonly Color WeakPointColor = new Color(.45f, 1f, .20f, 1f);

    private static readonly string[] Taunts =
    [
        "そのちっぽけな鉛で、業火が消せるとでも?",
        "熱いだろう? これが地獄の入口だ。",
        "まだ立っているのか。見上げた根性だ ── だが無駄だ。",
        "逃げ場などない。地上ごと焼べてやろう。",
        "踊れ、踊れ。炎が貴様らを抱くまで。",
        "私の名を、灰になる前に覚えておけ。",
    ];

    private readonly List<PrimitiveObjectToy[]> tentacles = [];
    private readonly List<SlimePuddle> puddles = [];
    private readonly List<WeakPoint> weakPoints = [];

    private readonly BossBar bossBar = new BossBar
    {
        Title = "DANTE",
        TitleColor = "#ff1a1a",
        BarColor = "#ff3333",
    };

    private ExiledNpc bossNpc;
    private Player boss;
    private RoleWear skin;

    private Vector3 arenaCenter;
    private float maxHp;
    private float hp;
    private int phase;
    private int wavesSpawned;
    private bool invulnerable;
    private float shieldExpiry;
    private bool leaping;
    private bool bodyHidden;
    private float visualMul = 1f;

    public override string Name => "-=[ DANTE ─ 業火の指揮者 ]=-";

    public override string Description => "業火の指揮者が目を覚ます。討伐部隊は 3 幕を凌ぎ、中央触手を断ってコアを暴け。";

    public override int MinimumPlayers => 1;

    /// <summary>抽選には出しません (手動起動専用)。</summary>
    public override int Weight => 0;

    protected override void OnStarted()
    {
        // アリーナ中心は「変換前」の参加者重心から取ります。
        List<Player> initial = Living.Where(player => player.IsPlayer).ToList();
        arenaCenter = initial.Count > 0
            ? AveragePosition(initial)
            : new Vector3(7f, 320f, -55f);

        if (!SpawnBoss())
        {
            StopCurrent();

            return;
        }

        PlayerEvents.Hurting += OnHurting;
        PlayTheme();

        Announce("<size=40><b><color=#ff1a1a>D A N T E</color></b></size>\n" +
                 "<size=22>業火の指揮者が目を覚ます ──</size>", 6f);

        Scope.Track(Timing.RunCoroutine(Prepare()));
    }

    protected override void OnStopped()
    {
        Cleanup();

        base.OnStopped();
    }

    // ───────────────────────────────────────────────────────────
    //  ボス実体
    // ───────────────────────────────────────────────────────────
    private bool SpawnBoss()
    {
        bossNpc = ExiledNpc.Spawn("DANTE", DanteBoss.BaseRoleType, ignored: true, position: arenaCenter + Vector3.up);

        if (bossNpc is null)
        {
            Logger.Error("[Dante] ボス NPC のスポーンに失敗しました。イベントを中止します。");

            return false;
        }

        boss = Player.Get(bossNpc.ReferenceHub);
        InternalNpcs.Register(boss, InternalNpcKind.TeamNpc);

        return true;
    }

    /// <summary>
    /// NPC のロール適用完了を待ってから Dante 化 → 討伐隊化 → 戦闘開始。
    /// ボスが Dante チームに入った後で全員を討伐隊にするので、
    /// 「生存チームが 1 つだけ」になる隙が生まれず即終了しません。
    /// </summary>
    private IEnumerator<float> Prepare()
    {
        yield return Timing.WaitForSeconds(ExiledNpc.SpawnSetRoleDelay + .1f);

        if (IsCanceled) yield break;

        if (boss is not { IsDestroyed: false })
        {
            Logger.Warn("[Dante] ボス NPC が初期化前に無効化されたため、イベントを中止します。");
            StopCurrent();

            yield break;
        }

        new DanteBoss().Spawn(boss);

        int playerCount = 0;

        foreach (Player player in Player.ReadyList.Where(player => player.IsPlayer).ToArray())
        {
            CustomRole.Spawn<DanteSlayer>(player);
            playerCount++;
        }

        // ロール変更によるテレポートが落ち着くのを待ち、ボスを重心へ寄せて即交戦に。
        yield return Timing.WaitForSeconds(.1f);

        if (IsCanceled) yield break;

        if (boss is not { IsDestroyed: false, IsAlive: true })
        {
            StopCurrent();

            yield break;
        }

        List<Player> targets = Targets();

        if (targets.Count > 0)
        {
            arenaCenter = AveragePosition(targets);
            boss.Position = arenaCenter + new Vector3(0f, 1f, 6f);
        }

        AttachSkin();
        CreateTentacles();

        maxHp = 2500f + 1200f * Mathf.Max(1, playerCount);
        hp = maxHp;
        phase = 1;

        bossBar.MaxValue = maxHp;
        bossBar.Value = hp;
        bossBar.Show();

        Say("danger . unrecognized entity detected on the surface . all units engage .",
            "警告 ── 地上に未確認の存在を検知。全戦力で交戦せよ。");
        Announce("<size=30><color=#ff2a2a><b>第一幕 ─ 業火の序曲</b></color></size>\n" +
                 "<size=20>Inferno Overture</size>", 6f);
        Speak("我が名はDANTE。地獄の業火を指揮する者。精々踊ってみせろ、塵共が。", 7f);

        Scope.Track(Timing.RunCoroutine(BattleLoop()));
        Scope.Track(Timing.RunCoroutine(ReinforcementWaves()));
    }

    /// <summary>
    /// 見た目はスキマティックを NPC へ追従させるだけです (ベストエフォート)。
    /// 付いたときだけ本体モデルを Fade で消し、当たり判定だけを残します。
    /// </summary>
    private void AttachSkin()
    {
        skin = RoleWear.AttachSchematic(boss, SchematicName, scale: Vector3.one * VisualScale);

        if (skin is null)
        {
            Logger.Warn($"[Dante] スキマティック '{SchematicName}' が見つかりません。スキン無しで続行します。");

            return;
        }

        boss.EnableEffect<Fade>(255);
        bodyHidden = true;
    }

    private void PlayTheme()
    {
        SpeakerApi.PlayLoop(
            ThemeFile,
            ThemeSpeaker,
            arenaCenter,
            isSpatial: false,
            maxDistance: 9_999_999f,
            minDistance: .1f,
            volume: 1f);
    }

    // ───────────────────────────────────────────────────────────
    //  メインループ
    // ───────────────────────────────────────────────────────────
    private IEnumerator<float> BattleLoop()
    {
        const float dt = .1f;

        float attackTimer = 0f;
        float hpBarTimer = 0f;
        float puddleTimer = 0f;

        while (true)
        {
            if (IsCanceled) yield break;

            if (boss is not { IsDestroyed: false, IsAlive: true })
            {
                StopCurrent();

                yield break;
            }

            if (hp <= 0f)
            {
                yield return Timing.WaitUntilDone(Timing.RunCoroutine(Finale()));

                yield break;
            }

            // 実 HP は常に満タンへ (流れ弾の保険)。
            boss.Health = DanteBoss.PinnedHealth;

            UpdatePhase();

            (float speed, float interval) = phase switch
            {
                1 => (5.0f, 2.4f),
                2 => (7.5f, 1.6f),
                _ => (10.0f, 1.0f),
            };

            // 無敵 (触手ゲート) 中は中央に据わってリンクを短く保つ。向きだけは追う。
            if (!leaping)
            {
                FaceNearest();

                if (!invulnerable)
                    ChaseNearest(speed * dt);
            }

            AnimateTentacles(Time.time);
            UpdateTentacleShield(Time.time);

            // 酸沼の判定は 0.5 秒間隔 (毎 tick のエフェクト連打を避ける)。
            puddleTimer += dt;

            if (puddleTimer >= .5f)
            {
                puddleTimer = 0f;
                ProcessPuddles();
            }

            attackTimer += dt;

            if (attackTimer >= interval && !leaping)
            {
                attackTimer = 0f;
                PerformAttack();
            }

            UpdateBossBar();

            hpBarTimer += dt;

            if (hpBarTimer >= 1f)
            {
                hpBarTimer = 0f;

                if (bodyHidden)
                    boss.EnableEffect<Fade>(255);
            }

            yield return Timing.WaitForSeconds(dt);
        }
    }

    private void UpdatePhase()
    {
        float ratio = hp / maxHp;
        int desired = ratio > .66f ? 1 : ratio > .33f ? 2 : 3;

        if (desired == phase) return;

        phase = desired;

        if (phase == 2)
        {
            Announce("<size=30><color=#ff7a00><b>第二幕 ─ 紅蓮の軍勢</b></color></size>\n" +
                     "<size=20>Crimson Legion</size>", 5f);
            Speak("第二幕だ ── 紅蓮の軍勢よ、目覚めよ。逃げ惑う姿が見たい。", 6f);
            Nova(24, 11f);
            BeginTentacleShield(3);

            return;
        }

        if (phase != 3) return;

        Announce("<size=34><color=#ff0000><b>第三幕 ─ 終焉のメルトダウン</b></color></size>\n" +
                 "<size=20>FINAL MELTDOWN</size>", 6f);
        Say(".G3 . G3 . meltdown imminent", "<color=#ff0000>まもなくメルトダウン。</color>");
        Speak("もう遊びは終わりだ。貴様らごと、全てを灰に帰してくれる！", 6f);

        foreach (Player target in Targets())
        {
            Shake(target);
        }

        BeginTentacleShield(5);
    }

    // ───────────────────────────────────────────────────────────
    //  移動
    // ───────────────────────────────────────────────────────────
    private void ChaseNearest(float step)
    {
        if (NearestTarget() is not { } target) return;

        Vector3 next = Vector3.MoveTowards(boss.Position, target.Position, step);

        // 直線ではなく左右に蛇行しながら迫る。
        Vector3 toTarget = target.Position - boss.Position;
        toTarget.y = 0f;

        if (toTarget.sqrMagnitude > .01f)
        {
            Vector3 sideways = Vector3.Cross(toTarget.normalized, Vector3.up);
            next += sideways * (Mathf.Sin(Time.time * 6f) * step * .8f);
        }

        next.y = target.Position.y + Mathf.Abs(Mathf.Sin(Time.time * 5f)) * .6f;
        boss.Position = next;
    }

    /// <summary>最寄りの標的を向きます (本体・スキン・触手の向きが揃います)。</summary>
    private void FaceNearest()
    {
        if (NearestTarget() is not { } target) return;

        Vector3 direction = target.Position - boss.Position;
        direction.y = 0f;

        if (direction.sqrMagnitude > .01f)
            boss.Rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
    }

    // ───────────────────────────────────────────────────────────
    //  増援 (戦死した討伐隊を上限回数まで戻す)
    // ───────────────────────────────────────────────────────────
    private IEnumerator<float> ReinforcementWaves()
    {
        while (wavesSpawned < MaxWaves)
        {
            yield return Timing.WaitForSeconds(WaveInterval);

            if (IsCanceled || boss is not { IsDestroyed: false, IsAlive: true }) yield break;

            List<Player> reinforcements = Player.ReadyList
                .Where(player => player.IsPlayer && player.Role is RoleTypeId.Spectator)
                .ToList();

            // 全員生存中なら波を温存する。
            if (reinforcements.Count == 0) continue;

            wavesSpawned++;

            Say("reinforcement squad has arrived", $"討伐隊 増援 第 {wavesSpawned} 波 到着。");
            Announce($"<size=28><color=#39ff14><b>増援部隊 第{wavesSpawned}波 到着！</b></color></size>\n" +
                     "<size=18>戦線を立て直せ</size>", 5f);

            foreach (Player reinforcement in reinforcements)
            {
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;

                // 位置は役職に持たせる。旧実装のような「ロール適用待ち → id で取り直し」は要らない。
                new DanteSlayer
                {
                    Spot = arenaCenter +
                           new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 12f +
                           Vector3.up,
                }.Spawn(reinforcement);
            }
        }

        Announce("<size=24><color=#ffcc00><b>これ以上の増援は無い。自力で討て。</b></color></size>", 6f);
    }

    // ───────────────────────────────────────────────────────────
    //  跳躍 (標的へ放物線で飛び、着地でスラム)
    // ───────────────────────────────────────────────────────────
    private IEnumerator<float> LeapAttack(Vector3 targetPosition)
    {
        if (leaping) yield break;

        leaping = true;
        Speak("墜ちろッ！", 2.5f);

        Vector3 start = boss.Position;
        const float duration = .75f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (IsCanceled || boss is not { IsDestroyed: false, IsAlive: true })
            {
                leaping = false;

                yield break;
            }

            elapsed += Time.deltaTime;

            float progress = Mathf.Clamp01(elapsed / duration);
            Vector3 position = Vector3.Lerp(start, targetPosition, progress);
            position.y += Mathf.Sin(progress * Mathf.PI) * 10f;
            boss.Position = position;

            AnimateTentacles(Time.time);

            yield return 0f;
        }

        boss.Position = targetPosition;

        // 着地スラム: 放射ノヴァ + 至近への直接ダメージ + 画面揺れ。
        Nova(phase == 3 ? 28 : 18, 12f);

        if (Random.value < .5f)
            FlashStorm(2);

        foreach (Player target in Targets())
        {
            float distance = Vector3.Distance(target.Position, targetPosition);

            if (distance < 14f)
            {
                target.Damage(35f, "DANTE SLAM");
                Shake(target);
            }
            else if (distance < 26f)
            {
                target.Damage(15f, "DANTE SLAM");
            }
        }

        leaping = false;
    }

    // ───────────────────────────────────────────────────────────
    //  触手 (プリミティブのカプセル節をうねらせる)
    // ───────────────────────────────────────────────────────────
    private void CreateTentacles()
    {
        DestroyTentacles();

        for (int index = 0; index < TentacleCount; index++)
        {
            PrimitiveObjectToy[] segments = new PrimitiveObjectToy[TentacleSegments];

            for (int segment = 0; segment < TentacleSegments; segment++)
            {
                segments[segment] = CreateSegment(TentacleColor);
            }

            tentacles.Add(segments);
        }
    }

    private void DestroyTentacles()
    {
        foreach (PrimitiveObjectToy[] tentacle in tentacles)
        {
            foreach (PrimitiveObjectToy segment in tentacle)
            {
                Destroy(segment);
            }
        }

        tentacles.Clear();
    }

    /// <summary>触手を時間ベースのうねりで再配置します。</summary>
    private void AnimateTentacles(float time)
    {
        if (boss is not { IsDestroyed: false } || tentacles.Count == 0) return;

        Vector3 basePosition = boss.Position;
        Quaternion bossRotation = boss.Rotation;
        float scale = VisualScale * visualMul;
        float rootRadius = 1.1f * scale;
        float segmentLength = 1f * (scale / 2.6f);

        for (int index = 0; index < tentacles.Count; index++)
        {
            PrimitiveObjectToy[] segments = tentacles[index];
            float yaw = 360f / tentacles.Count * index;
            float phaseOffset = index * 1.37f;

            // 本体の向きを基準に放射状の根本を決める。
            Vector3 outward = bossRotation * Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            Vector3 root = basePosition + outward * rootRadius + Vector3.up * (.4f * scale);
            Vector3 direction = (outward * .5f + Vector3.up).normalized;
            Vector3 position = root;

            for (int segment = 0; segment < segments.Length; segment++)
            {
                float bend = Mathf.Sin(time * 4.5f + phaseOffset + segment * .85f) * 24f;
                Vector3 axis = Vector3.Cross(direction, Vector3.up);

                if (axis.sqrMagnitude < .0001f)
                    axis = Vector3.Cross(direction, outward);

                direction = (Quaternion.AngleAxis(bend, axis.normalized) * direction).normalized;

                Vector3 next = position + direction * segmentLength;
                float thickness = .6f * scale / 2.6f * (1f - segment * .08f);
                OrientSegment(segments[segment], position, next, thickness);
                position = next;
            }
        }
    }

    /// <summary>キューブ (長軸 Z) を 2 点間に橋渡しします。</summary>
    private static void OrientSegment(PrimitiveObjectToy segment, Vector3 from, Vector3 to, float thickness)
    {
        if (segment is not { IsDestroyed: false }) return;

        Vector3 delta = to - from;
        float length = delta.magnitude;

        if (length < .001f)
        {
            segment.Scale = Vector3.zero;

            return;
        }

        segment.Position = (from + to) * .5f;
        segment.Rotation = Quaternion.LookRotation(delta / length, Vector3.up);
        segment.Scale = new Vector3(thickness, thickness, length);
    }

    private static PrimitiveObjectToy CreateSegment(Color color)
    {
        // Customize してから Spawn する (SpawnMessage がスケールを含むので再送が要らない)。
        PrimitiveObjectToy segment = PrimitiveObjectToy.Create(
            Vector3.zero, Quaternion.identity, Vector3.one * .1f, networkSpawn: false);

        segment.Type = PrimitiveType.Cube;
        segment.Color = color;
        segment.Flags = AdminToys.PrimitiveFlags.Visible; // 当たり判定は持たせない
        segment.Spawn();

        return segment;
    }

    private static void Destroy(PrimitiveObjectToy toy)
    {
        if (toy is { IsDestroyed: false })
            toy.Destroy();
    }

    // ───────────────────────────────────────────────────────────
    //  攻撃ディスパッチ
    // ───────────────────────────────────────────────────────────
    private void PerformAttack()
    {
        List<Player> targets = Targets();

        if (targets.Count == 0) return;

        if (Random.value < .3f)
            Speak(Taunts[Random.Range(0, Taunts.Length)], 4f);

        // 幕が上がるほど跳躍が増える。無敵中は据わるので跳ばない。
        float leapChance = phase switch { 1 => .3f, 2 => .45f, _ => .55f };

        if (!leaping && !invulnerable && Random.value < leapChance)
        {
            Vector3 destination = targets[Random.Range(0, targets.Count)].Position;
            Scope.Track(Timing.RunCoroutine(LeapAttack(destination)));

            return;
        }

        switch (phase)
        {
            case 1:
                int roll1 = Random.Range(0, 4);

                if (roll1 == 0) AcidPuddles(targets, 2);
                else if (roll1 == 1) GrenadeRain(targets[Random.Range(0, targets.Count)].Position, 6, 4f);
                else if (roll1 == 2) SkyTentacleRain(targets, 2);
                else LobAt(NearestTarget());

                break;

            case 2:
                int roll2 = Random.Range(0, 5);

                if (roll2 == 0) Nova(16, 9f);
                else if (roll2 == 1) SlimeBalls(14);
                else if (roll2 == 2) AcidPuddles(targets, 3);
                else if (roll2 == 3) SkyTentacleRain(targets, 3);
                else StickyEngulf();

                break;

            default:
                int roll3 = Random.Range(0, 5);

                if (roll3 == 0) Nova(24, 10f);
                else if (roll3 == 1) AcidPuddles(targets, 4);
                else if (roll3 == 2) SlimeBalls(12);
                else if (roll3 == 3) SkyTentacleRain(targets, 4);
                else
                {
                    StickyEngulf();
                    GrenadeRain(targets[Random.Range(0, targets.Count)].Position, 8, 5f);
                }

                if (Random.value < .15f)
                    FlashStorm(2);

                break;
        }
    }

    /// <summary>頭上から降り注ぐグレネードの雨。</summary>
    private static void GrenadeRain(Vector3 center, int count, float radius)
    {
        for (int index = 0; index < count; index++)
        {
            Vector2 offset = Random.insideUnitCircle * radius;
            Throw(ItemType.GrenadeHE, center + new Vector3(offset.x, 16f, offset.y), Vector3.down * 18f, 1.1f);
        }
    }

    /// <summary>ボスを中心とした放射状グレネード環。</summary>
    private void Nova(int count, float speed)
    {
        Vector3 origin = boss.Position + Vector3.up * 1.2f;

        for (int index = 0; index < count; index++)
        {
            float angle = 360f / count * index * Mathf.Deg2Rad;
            Vector3 direction = new Vector3(Mathf.Cos(angle), .35f, Mathf.Sin(angle));
            Throw(ItemType.GrenadeHE, origin, direction.normalized * speed, 2.6f);
        }
    }

    /// <summary>分裂粘塊。跳ね回る SCP-018 を「ちぎれた粘体」としてばら撒きます。</summary>
    private void SlimeBalls(int count)
    {
        Speak("我が身を分かつ ── 喰らえ、粘塊！", 3f);

        Vector3 origin = boss.Position + Vector3.up * 1.5f;

        for (int index = 0; index < count; index++)
        {
            Vector3 direction = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(.2f, .8f),
                Random.Range(-1f, 1f));

            Throw(ItemType.SCP018, origin, direction.normalized * Random.Range(8f, 14f), 0f);
        }
    }

    /// <summary>ボス周囲に閃光手榴弾を撒いて視界を奪います。</summary>
    private void FlashStorm(int count)
    {
        for (int index = 0; index < count; index++)
        {
            Vector2 offset = Random.insideUnitCircle * 9f;
            Throw(ItemType.SCP2176, boss.Position + new Vector3(offset.x, 1.5f, offset.y), Vector3.up * 2f, 1.3f);
        }
    }

    /// <summary>最寄りの標的へ山なりに投擲します。</summary>
    private void LobAt(Player target)
    {
        if (target is null) return;

        Vector3 origin = boss.Position + Vector3.up * 1.5f;
        Vector3 flat = target.Position - origin;
        flat.y = 0f;

        Throw(ItemType.GrenadeHE, origin, flat.normalized * 12f + Vector3.up * 6f, 2.2f);
    }

    /// <summary>射出ヘルパー: 生成 → 信管 → 初速。</summary>
    private static void Throw(ItemType type, Vector3 position, Vector3 velocity, float fuse)
    {
        TimedGrenadeProjectile projectile =
            TimedGrenadeProjectile.SpawnActive(position, type, null, fuse > 0f ? fuse : -1.0);

        if (projectile?.Rigidbody is { } body)
            body.linearVelocity = velocity;
    }

    // ───────────────────────────────────────────────────────────
    //  スライム系 (酸の沼・粘着)
    // ───────────────────────────────────────────────────────────
    private void AcidPuddles(List<Player> targets, int count)
    {
        if (targets.Count == 0) return;

        Speak("沼に沈め。骨まで溶かしてやる。", 3f);

        for (int index = 0; index < count; index++)
        {
            Player target = targets[Random.Range(0, targets.Count)];
            Vector2 jitter = Random.insideUnitCircle * 2.5f;
            SpawnPuddle(target.Position + new Vector3(jitter.x, .05f, jitter.y), Random.Range(2.4f, 3.4f));
        }
    }

    private void SpawnPuddle(Vector3 center, float radius)
    {
        // 上限を超えたら古いものから消す。
        while (puddles.Count >= MaxPuddles)
        {
            RemovePuddle(puddles[0]);
        }

        PrimitiveObjectToy visual = PrimitiveObjectToy.Create(
            center,
            Quaternion.Euler(90f, 0f, 0f),
            new Vector3(radius * 2f, radius * 2f, .12f),
            networkSpawn: false);

        visual.Type = PrimitiveType.Cube;
        visual.Color = PuddleColor;
        visual.Flags = AdminToys.PrimitiveFlags.Visible;
        visual.Spawn();

        puddles.Add(new SlimePuddle
        {
            Visual = visual,
            Center = center,
            Radius = radius,
            Expiry = Time.time + PuddleLifetime,
        });
    }

    /// <summary>0.5 秒間隔。寿命切れの除去と、沼内プレイヤーへの腐食・鈍足・DOT。</summary>
    private void ProcessPuddles()
    {
        for (int index = puddles.Count - 1; index >= 0; index--)
        {
            SlimePuddle puddle = puddles[index];

            if (Time.time >= puddle.Expiry || puddle.Visual is not { IsDestroyed: false })
            {
                RemovePuddle(puddle);

                continue;
            }

            foreach (Player target in Targets())
            {
                Vector3 flat = target.Position - puddle.Center;
                flat.y = 0f;

                if (flat.sqrMagnitude > puddle.Radius * puddle.Radius) continue;

                // 1 秒持続で付与 (0.5 秒間隔の再付与でも連打にならない)。
                target.EnableEffect<Corroding>(1, 1f);
                target.EnableEffect<Slowness>(40, 1f);
                target.Damage(5f, "DANTE ACID");
            }
        }
    }

    private void RemovePuddle(SlimePuddle puddle)
    {
        puddles.Remove(puddle);
        Destroy(puddle.Visual);
    }

    private void DestroyPuddles()
    {
        foreach (SlimePuddle puddle in puddles)
        {
            Destroy(puddle.Visual);
        }

        puddles.Clear();
    }

    /// <summary>粘着捕縛。周囲を鈍足化・汚染し、軽く引き寄せます (完全拘束はしません)。</summary>
    private void StickyEngulf()
    {
        Speak("逃がさん。粘体が貴様を捉えた。", 3f);

        Vector3 bossPosition = boss.Position;

        foreach (Player target in Targets())
        {
            Vector3 toBoss = bossPosition - target.Position;
            toBoss.y = 0f;

            if (toBoss.sqrMagnitude > 16f * 16f) continue;

            target.EnableEffect<Slowness>(60, 2.5f);
            target.EnableEffect<Stained>(1, 2.5f);
            target.Damage(8f, "DANTE SLIME");

            if (toBoss.magnitude > 4f)
                target.Position += toBoss.normalized * 2f;
        }
    }

    // ───────────────────────────────────────────────────────────
    //  上空からの触手降らし
    // ───────────────────────────────────────────────────────────
    private void SkyTentacleRain(List<Player> targets, int count)
    {
        if (targets.Count == 0) return;

        Speak("天より来たれ、我が触腕。", 3f);

        for (int index = 0; index < count; index++)
        {
            Player target = targets[Random.Range(0, targets.Count)];
            Vector2 jitter = Random.insideUnitCircle * 4f;
            Vector3 impact = target.Position + new Vector3(jitter.x, 0f, jitter.y);
            Scope.Track(Timing.RunCoroutine(SkyTentacleStrike(impact)));
        }
    }

    private IEnumerator<float> SkyTentacleStrike(Vector3 impact)
    {
        // 落下する緑の触手柱。
        PrimitiveObjectToy column = PrimitiveObjectToy.Create(
            impact + Vector3.up * 32f,
            Quaternion.identity,
            new Vector3(.9f, 14f, .9f),
            networkSpawn: false);

        column.Type = PrimitiveType.Cube;
        column.Color = TentacleColor;
        column.Flags = AdminToys.PrimitiveFlags.Visible;
        column.Spawn();

        Vector3 start = impact + Vector3.up * 32f;
        Vector3 end = impact + Vector3.up * 7f;
        const float fall = .5f;

        for (float elapsed = 0f; elapsed < fall; elapsed += Time.deltaTime)
        {
            if (IsCanceled || column is not { IsDestroyed: false })
            {
                Destroy(column);

                yield break;
            }

            column.Position = Vector3.Lerp(start, end, elapsed / fall);

            yield return 0f;
        }

        column.Position = end;

        // 着弾 AOE (半径 3m)。
        foreach (Player target in Targets())
        {
            Vector3 flat = target.Position - impact;
            flat.y = 0f;

            if (flat.sqrMagnitude >= 9f) continue;

            target.Damage(40f, "DANTE TENTACLE");
            ExplosionUtils.ServerSpawnEffect(target.Position, ItemType.GrenadeHE);
        }

        Throw(ItemType.GrenadeHE, impact + Vector3.up * .5f, Vector3.zero, .3f);

        yield return Timing.WaitForSeconds(.6f);

        Destroy(column);
    }

    // ───────────────────────────────────────────────────────────
    //  中央触手 (壊すまでコアは無敵)
    // ───────────────────────────────────────────────────────────
    private void BeginTentacleShield(int count)
    {
        if (invulnerable) return;

        invulnerable = true;
        shieldExpiry = Time.time + ShieldTimeout;

        Announce("<size=30><color=#39ff14><b>コアは無敵だ！</b></color></size>\n" +
                 "<size=20>中央につながる触手を破壊せよ</size>", 6f);
        Speak("無駄だ。我が核に触れたくば、まずこの触腕を引きちぎってみせろ。", 6f);
        Say("core is now protected", "コアが保護されました。", noise: false);

        // 触手はボス (壁際の可能性がある) ではなく参加者の重心周りに出す (到達性優先)。
        List<Player> around = Targets();
        Vector3 center = around.Count > 0 ? AveragePosition(around) : boss.Position;

        for (int index = 0; index < count; index++)
        {
            float angle = 360f / count * index * Mathf.Deg2Rad;
            Vector3 spot = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 10f + Vector3.up;

            ExiledNpc npc = ExiledNpc.Spawn("Tentacle Core", DanteTentacle.BaseRoleType, ignored: true, position: spot);

            if (npc is null) continue;

            Player core = Player.Get(npc.ReferenceHub);
            InternalNpcs.Register(core, InternalNpcKind.Tentacle);

            PrimitiveObjectToy[] link = new PrimitiveObjectToy[4];

            for (int segment = 0; segment < link.Length; segment++)
            {
                link[segment] = CreateSegment(WeakPointColor);
            }

            weakPoints.Add(new WeakPoint { Npc = npc, Player = core, Link = link });

            // NPC のロール適用が終わってから弱点として立たせる (位置は役職が持つ)。
            Scope.Delay(ExiledNpc.SpawnSetRoleDelay + .1f, () =>
            {
                if (IsCanceled || core is not { IsDestroyed: false }) return;

                new DanteTentacle { Spot = spot }.Spawn(core);
            });
        }

        // 1 つも湧かなかったときは無敵を解除する (保険)。
        if (weakPoints.Count == 0)
            invulnerable = false;
    }

    /// <summary>リンクを更新し、死んだ弱点を除去します。全滅でコア露出。</summary>
    private void UpdateTentacleShield(float time)
    {
        if (!invulnerable) return;

        // 到達不能などで詰まないよう、制限時間で強制解除する。
        if (time >= shieldExpiry)
        {
            Speak("チッ…まあいい。じきに磨り潰す。", 4f);
            EndTentacleShield();

            return;
        }

        Vector3 center = boss.Position + Vector3.up * (1.5f * VisualScale * visualMul);

        for (int index = weakPoints.Count - 1; index >= 0; index--)
        {
            WeakPoint weakPoint = weakPoints[index];

            if (weakPoint.Player is not { IsDestroyed: false, IsAlive: true })
            {
                weakPoints.RemoveAt(index);
                DestroyWeakPoint(weakPoint);

                continue;
            }

            // コア中心から弱点へ、軽くうねらせて橋渡しする。
            Vector3 from = center;
            Vector3 to = weakPoint.Player.Position + Vector3.up;
            Vector3 side = Vector3.Cross((to - from).normalized, Vector3.up);

            for (int segment = 0; segment < weakPoint.Link.Length; segment++)
            {
                float start = segment / (float)weakPoint.Link.Length;
                float end = (segment + 1) / (float)weakPoint.Link.Length;
                float wobble = Mathf.Sin(time * 5f + index * 1.3f + segment) * .5f;

                OrientSegment(
                    weakPoint.Link[segment],
                    Vector3.Lerp(from, to, start) + side * wobble,
                    Vector3.Lerp(from, to, end) + side * wobble,
                    .35f);
            }
        }

        if (weakPoints.Count == 0)
            EndTentacleShield();
    }

    private void EndTentacleShield()
    {
        if (!invulnerable) return;

        invulnerable = false;
        DestroyWeakPoints();

        Announce("<size=32><color=#ffd000><b>コア露出！ 今だ、叩け！</b></color></size>", 5f);
        Speak("ぐっ…触腕が…ええい、忌々しい羽虫共め！");
    }

    private void DestroyWeakPoints()
    {
        foreach (WeakPoint weakPoint in weakPoints)
        {
            DestroyWeakPoint(weakPoint);
        }

        weakPoints.Clear();
    }

    private static void DestroyWeakPoint(WeakPoint weakPoint)
    {
        foreach (PrimitiveObjectToy segment in weakPoint.Link)
        {
            Destroy(segment);
        }

        InternalNpcs.Unregister(weakPoint.Player);
        weakPoint.Npc.Destroy();
    }

    // ───────────────────────────────────────────────────────────
    //  撃破フィナーレ
    // ───────────────────────────────────────────────────────────
    private IEnumerator<float> Finale()
    {
        Vector3 center = boss.Position;

        SpeakerApi.TryDestroy(ThemeSpeaker);
        bossBar.Hide();

        Announce("<size=36><color=#ffd000><b>DANTE 撃破</b></color></size>\n" +
                 "<size=22>業火の指揮者は沈黙した</size>", 8f);
        Speak("馬鹿な…この私の業火が…消え……る…", 8f);
        Say("entity neutralized . the surface is secured .", "対象を無力化。地上を確保しました。");

        // 断末魔: 触手を暴れさせながら見た目を縮め、連続爆裂で散らす。
        leaping = true;
        const int waves = 5;

        for (int wave = 0; wave < waves; wave++)
        {
            if (IsCanceled || boss is not { IsDestroyed: false, IsAlive: true }) break;

            Nova(20, 12f);

            foreach (Player target in Targets())
            {
                Shake(target);
            }

            float shrinkFrom = 1f - wave / (float)waves;
            float shrinkTo = 1f - (wave + 1) / (float)waves;

            for (float elapsed = 0f; elapsed < .45f; elapsed += Time.deltaTime)
            {
                if (IsCanceled || boss is not { IsDestroyed: false, IsAlive: true }) break;

                visualMul = Mathf.Max(.02f, Mathf.Lerp(shrinkFrom, shrinkTo, elapsed / .45f));

                if (skin?.Schematic is { } schematic)
                    schematic.Scale = Vector3.one * (VisualScale * visualMul);

                AnimateTentacles(Time.time);

                yield return 0f;
            }
        }

        // 生存者への褒賞。
        foreach (Player target in Targets())
        {
            target.Health = target.MaxHealth;
        }

        Throw(ItemType.GrenadeHE, center + Vector3.up, Vector3.zero, .1f);

        // ボスが消えると Dante チームが空になり、討伐側 (カオス) の勝利で終わる。
        StopCurrent();
    }

    // ───────────────────────────────────────────────────────────
    //  表示
    // ───────────────────────────────────────────────────────────
    private void UpdateBossBar()
    {
        bossBar.MaxValue = maxHp;
        bossBar.Value = hp;
        bossBar.Subtitle = phase switch
        {
            1 => "業火の序曲",
            2 => "紅蓮の軍勢",
            _ => "終焉のメルトダウン",
        };

        bossBar.StateText = invulnerable
            ? $"<size=24><color=#39ff14><b>★ コア無敵 ★</b> 中央触手 残り {weakPoints.Count}</color></size>"
            : string.Empty;
    }

    /// <summary>DANTE のセリフ。</summary>
    private static void Speak(string line, float duration = 5f)
    {
        HudExtensions.NotifyAll(
            "<size=22><color=#ff2a2a><b>DANTE</b></color></size>\n" +
            $"<size=18><i><color=#ffb3b3>「{line}」</color></i></size>",
            duration);
    }

    private static void Announce(string message, float duration) => HudExtensions.NotifyAll(message, duration);

    /// <summary>核爆発の画面揺れを 1 人へ送ります (LabApi に同等の入口が無いので EXILED 側)。</summary>
    private static void Shake(Player target)
    {
        if (ExiledPlayer.Get(target.ReferenceHub) is { } exiled)
            exiled.SendWarheadExplosionEffect();
    }

    // ───────────────────────────────────────────────────────────
    //  被弾フック (仮想 HP)
    // ───────────────────────────────────────────────────────────
    private void OnHurting(PlayerHurtingEventArgs ev)
    {
        if (boss is not { IsDestroyed: false } || !ReferenceEquals(ev.Player, boss)) return;

        float incoming = ev.DamageHandler is StandardDamageHandler damage ? damage.Damage : 0f;

        // ボスは実 HP を一切失わない (幕進行と撃破は仮想 HP だけで決める)。
        if (ev.DamageHandler is StandardDamageHandler pinned)
            pinned.Damage = 0f;

        // 仮想 HP を削れるのは実プレイヤーの攻撃だけ。自爆の巻き込みは通さない。
        if (ev.Attacker is not { IsDestroyed: false } attacker || !attacker.IsPlayer) return;

        if (invulnerable)
        {
            if (Random.value < .25f)
                attacker.Notify("<color=#39ff14>コアは無敵だ ── 中央の触手を破壊しろ！</color>", 1.5f);

            return;
        }

        if (hp > 0f)
            hp = Mathf.Max(0f, hp - incoming);
    }

    // ───────────────────────────────────────────────────────────
    //  標的選定
    // ───────────────────────────────────────────────────────────
    /// <summary>
    /// 標的の一覧です。ボスと中央触手はこちらが生成した NPC なので
    /// <see cref="InternalNpcs"/> で 1 回に除けます (旧実装の「SCP を除く」判定は不要)。
    /// </summary>
    private static List<Player> Targets() =>
        Living.Where(player => !InternalNpcs.IsManaged(player)).ToList();

    private Player NearestTarget()
    {
        Vector3 origin = boss.Position;

        return Targets()
            .OrderBy(player => (player.Position - origin).sqrMagnitude)
            .FirstOrDefault();
    }

    private static Vector3 AveragePosition(IReadOnlyCollection<Player> players)
    {
        Vector3 sum = Vector3.zero;

        foreach (Player player in players)
        {
            sum += player.Position;
        }

        return sum / players.Count;
    }

    // ───────────────────────────────────────────────────────────
    //  後始末 (撃破・中断・ラウンド再開で共通)
    // ───────────────────────────────────────────────────────────
    private void Cleanup()
    {
        PlayerEvents.Hurting -= OnHurting;

        bossBar.Hide();
        DestroyTentacles();
        DestroyPuddles();
        DestroyWeakPoints();
        SpeakerApi.TryDestroy(ThemeSpeaker);

        skin?.Dispose();
        skin = null;

        if (bossNpc is null) return;

        InternalNpcs.Unregister(boss);
        bossNpc.Destroy();
        bossNpc = null;
        boss = null;
    }

    /// <summary>酸の沼 1 つ。</summary>
    private sealed class SlimePuddle
    {
        public PrimitiveObjectToy Visual;
        public Vector3 Center;
        public float Radius;
        public float Expiry;
    }

    /// <summary>中央触手 1 本 (弱点 NPC + コアへ伸びるリンク)。</summary>
    private sealed class WeakPoint
    {
        public ExiledNpc Npc;
        public Player Player;
        public PrimitiveObjectToy[] Link;
    }
}
