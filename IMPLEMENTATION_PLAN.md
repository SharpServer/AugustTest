# Slafight → Sliced 全面再構築

`Slafight_Plugin_EXILED` (597 ファイル / 89,539 行) を、LabAPI ベースの `Sliced` API の上で
機能ごと再構築する計画と進捗。**移植ではなく再設計。**

- 新プラグインは `AntiMeme`(= 新 Slafight)。アセンブリ名・namespace はそのまま
- `AntiMeme` は **EXILED プラグイン**(`%APPDATA%\EXILED\Plugins\7778`)
- `Sliced` は **LabAPI プラグイン**(`%APPDATA%\SCP Secret Laboratory\LabAPI\plugins\7778`)
- 本番 7777 と `Slafight_Plugin_EXILED` リポジトリは**完成・切替まで触らない**

```powershell
dotnet build .\AugustTest.slnx --configuration Release
```

---

## 設計原則

### 1. 型がアイデンティティ

振る舞いを持つものは、**型そのものを同一性にする**。別名の string キーも、対応する enum も、
それを引くレジストリも DefinitionSource も作らない。

```csharp
// チームが「誰を含むか」と「どう勝つか」を自分で持つ
public sealed class ScpTeam : CustomTeam
{
    public override string Name  => "SCP";
    public override string Color => "#e02020";
    public override bool Includes(Player player) => player.IsSCP;
    public override VictoryCondition Victory => VictoryCondition.LastStanding(priority: 10);
}

// 役職はチームをインスタンスで指す
public sealed class Scp049 : CustomRole
{
    public override CustomTeam Team => CustomTeam.Get<ScpTeam>();
}
```

旧構造で「SCP が勝った」を判定するには
`DefinitionSource → RoundVictoryRule → ToGroup() → RoundVictoryGroup → RoundVictorySystem →
GetVictoryTeam(player)` と 5 ホップ必要だった。

外部から文字列で指す必要がある箇所(RA コマンド・JSON 保存)は
`TypeParser.TryCreate<CustomRole>("Scp049")` が型名で解決する。
**各クラスに `UniqueKey` プロパティを生やさない。**

**やり過ぎない線引き:**

| インスタンスにする | enum / 値のままにする |
|---|---|
| 振る舞いを持つもの(チーム・役職・アイテム・アビリティ・ゲームモード・勝利条件) | ゲーム本体の enum(`RoleTypeId` / `ItemType` / `AmmoType`) |
| 他から参照されるもの(役職 → チーム) | 振る舞いを持たない閉じた値集合(`Season` / `SpecificFlagType`) |

インスタンスの置き場は `static readonly` フィールドか `CustomTeam.Get<T>()` で十分。
**ID 採番・登録 API・解決キャッシュを持つ「レジストリ」を新設しない。**
作った瞬間に旧構造へ逆戻りする。

### 2. 冗長な防御を持ち込まない

旧コードの実測値(89,539 行に対して):

| パターン | 実測 | 新コードでの扱い |
|---|---:|---|
| null 比較 | **1,934** | **46 行に 1 回**。null になり得る境界で 1 回だけ確かめ、以降は非 null として扱う |
| `?.` | **981** | `a?.b?.c?.d` は「どこで null だったか」を隠すだけで安全にはしない |
| `??` / `??=` | **274** | 意味のある既定があるときだけ |
| null 免除 `!` | **115** | nullable 無効なので書かない |
| `try` / `catch` | **413 / 402** | 例外を制御フローに使わない。外部 I/O と後始末ループだけ |
| 遅延内の `Player.Get(id)` 再取得 | **65** | `PlayerScope` に載せる |
| Exiled/LabApi の alias using | **163** | LabAPI ファーストで大半が消える |
| `Player.List` + 手書きフィルタ | **188** | `Player.ReadyList` を既定に |
| `IBootstrapHandler` | 41 実装 | 全廃。`EventHandlerBase` の自動登録に置換 |

**判断基準: その防御は実際に起きた不具合に対応しているか、それとも「念のため」か。**
後者は書かない。旧コードの null チェックは「これを消したら何が壊れるか」を
答えられない限り移植しない。

### 3. nullable は無効のまま

`Sliced` の現状(csproj に `<Nullable>` を書かない)を `AntiMeme` にも適用する。
EXILED / LabAPI / ゲーム本体はすべて null 非注釈なので、`enable` にしても
効いてほしい場所には効かず、`?` と `!` の装飾だけが全ファイルに乗る。
参照型に `?` を付けず、`is not null` / `is { } x` のパターンで書く。

### 4. プレイヤーの生存確認

LabAPI の `Player` が持つプロパティで済ませる。**netId キーや世代カウンタを player 層に持ち込まない。**

- `player is { IsDestroyed: false }` … `IsDestroyed` は `!(UnityEngine.Object)ReferenceHub`
- `IsOnline` / `IsOffline` は `[Obsolete("Use !IsDestroyed instead.")]` なので**使わない**
- 遅延処理は `PlayerScope` に載せる(退出・ラウンド再開で自動 `Dispose`)

`netId` を使うのは**プレイヤーではない Mirror オブジェクト**だけ
(装着物追跡・可視性管理・ロールシンクの送信先スロットリング)。

### 5. イベント購読

Sliced の `RegisterEvents()` / `UnregisterEvents()` をそのまま使う。
スコープや購読ラッパーのような仕掛けは入れない。
解除漏れは `HandlerLifetime` と `Owner` による自動 `Dispose` が受け止める。

---

## 検証済みの技術的前提

**EXILED ロードのプラグインでも Sliced の自動登録は効く。** 逆コンパイルで確認:

1. `MEC.Timing.RunCoroutine` は `prewarm: true` で初回 `MoveNext()` を**同期実行**する
   (`Assembly-CSharp-firstpass.dll` / `MEC.Timing`)
2. `Exiled.Loader.LoaderPlugin.Enable()` は `Timing.RunCoroutine(new Loader().Run())` のみ。
   `Loader.Run()` の正常系に `yield return` は無く、
   `LoadDependencies()` → `LoadPlugins()` → `EnablePlugins()` が初回 MoveNext で完走する
3. `LabApi.Loader.PluginLoader.Initialize()` は `LoadAllPlugins()` の**後**に
   `ServerEvents.OnPluginsEnabled()` を発火する
4. `LoadPriority` は昇順で低い値が先(Highest=64 … Lowest=192)。`Plugin.Priority` の既定は
   Medium=128、EXILED の `LoaderPlugin` は **255**
   → 有効化順は **Sliced → EXILED → AntiMeme**

→ `EventHandlerRegistry.Scan()` の時点で `AntiMeme.dll` は AppDomain に存在する。
ただしローダーの実行順に依存するため、`EventHandlerRegistry.RegisterAssembly()` で
名指し登録する経路も用意している(二重に検出されない)。

### バージョン(現物確認済み)

- `ExMod.Exiled 9.14.2` が `Exiled.* 9.14.2` + **`LabApi 1.1.7`** +
  `Assembly-CSharp-Publicized` + `CommandSystem.Core` + `NorthwoodLib` + `YamlDotNet 11.1.3` を供給
- `Lib.Harmony 2.2.2`
- `$(SL_References)` = `D:\RiderWorks\SL_References`(machine 環境変数)

---

## Phase 0 — 基盤整備(直列)

### 0-A. AntiMeme を EXILED プラグイン化 [完了]

- `AntiMeme.csproj`: 配備先を `EXILED\Plugins\7778` へ。`$(SL_References)` から
  `HintServiceMeow-Exiled` / `SNAPI-HSM` / `Snake` を追加。BCL バックポート 11 個を移植。
  `LangVersion latest`、`<Nullable>` は書かない
- `AntiMemePlugin`: `Exiled.API.Features.Plugin<Config>` へ。
  `OnEnabled` に書いてよいのは Harmony・外部ツール初期化・Server Specific Settings だけ
- `Config`: `Exiled.API.Interfaces.IConfig` 実装
- 配備確認済み。依存 DLL は Slafight と同一ハッシュなので共有 `dependencies` は実質無変更

### 0-B1. Sliced 既存ファイルの改修 [完了]

| ファイル | 内容 |
|---|---|
| `CommandBase` | 権限ノードのバグ修正(下記)、`TryGetArgument` を追加 |
| `CustomTeam` | 非機能スタブを、`Includes` / `Victory` / `Allies` / `FindWinner` を持つ形へ全面書き直し |
| `CustomRole` | per-player インスタンス + 宣言的スポーン記述へ全面書き直し |
| `SpawnSet` / `SpawnSetRoleDefinition` | 型指定ファクトリ化(下記) |
| `EventHandlerRegistry` | `RegisterAssembly` 追加、`Scan` を冪等化、`PendingAssemblies` で順序非依存に |
| `TypeParser` | `FindTypes<TBase>()` を追加(チーム一覧などの「全部集める」用途) |

### 0-B2. Sliced 新規コア [完了]

| ファイル | 行数 | 内容 |
|---|---:|---|
| `API/Features/CustomItem.cs` | 371 | カスタムアイテム基底。**アイテム 1 個 = 1 インスタンス**、シリアル追跡。**全イベントが LabAPI**(EXILED 不要) |
| `API/Features/AbilityBase.cs` | 232 | アビリティ基底。寿命は `PlayerScope` に相乗り。入力方式は持たない |
| `API/Features/GameMode.cs` | 205 | 旧 `SpecialEvent`。**打ち切り判定 `IsCanceled` を基底が持つ**ので各モードが世代カウンタを自前で持たない |
| `API/Features/RoundScope.cs` | 178 | **単一のラウンド後始末入口**。旧の 45 ファイル個別購読を 1 本化 |
| `API/Features/PlayerScope.cs` | 227 | 旧 `CRoleRuntime`(340行)を一般化。文字列キーの値バッグは入れない |
| `API/Features/VictoryCondition.cs` | 75 | 旧 `RoundVictoryRule`(154行)+ `RoundVictoryGroup` を 1 型に統合 |
| `API/Features/NetGuards.cs` | 54 | 3 重複していた `IsReadyClient` を 1 本に。適用範囲は生 Mirror 送信だけ |
| `API/Structs/RoleEffect.cs` | 42 | 効果を型で宣言(`RoleEffect.Of<MovementBoost>(20)`) |

※ `RoleIdentity` は独立ファイルにせず `CustomRole` の static に畳んだ
(`CustomRole.Of(player)` / `Is<T>` / `Active`)。概念を 1 つ減らすため。

### 0-C. Phase 0 完了ゲート [完了]

クリーンビルド green / 警告 0。Sliced は 1,200 行 → **3,100 行**。

| 領域 | 旧 Slafight | 新 Sliced |
|---|---:|---:|
| アイテム基盤 | `CItem` 1,377 + `CItemWeapon` 752 ほか ≈ 2,400 | `CustomItem` 371 |
| 役職基盤 | `CRole` 1,457 + `CRoleRuntime` 340 | `CustomRole` 310 + `PlayerScope` 227 |
| チーム / 勝利判定 | `Teams\` + `RoundVictory\` ≈ 2,500 | `CustomTeam` 142 + `VictoryCondition` 75 |
| アビリティ | `AbilityBase` 494 + `AbilityManager` 214 | `AbilityBase` 232 |
| ゲームモード | `SpecialEvent` 223 + `SpecialEventsHandler` 523 | `GameMode` 205 |

**重要な発見: LabAPI 1.1.7 はアイテムイベントを完全に持っている。**
`PickingUpItem` / `PickedUpItem` / `DroppingItem` / `DroppedItem` / `UsingItem` / `UsedItem` /
`ShootingWeapon` / `ShotWeapon` / `ChangingItem` / `ChangedItem` / `ReloadingWeapon` /
`ThrowingItem` / `InspectingKeycard` / `SearchingPickup` など。
旧プラグインが EXILED を使っていたのは当時 LabAPI に無かったからで、
**アイテム層は EXILED 参照ゼロで組める。**

### 0-B で直した旧実装の実バグ

1. **`SpawnSet` がカスタム役職インスタンスを使い回していた** —
   `role.CustomRole.Spawn(target)` は定義が持つ 1 個のインスタンスに `Player` を上書きするため、
   `RoleAllowedCount > 1` で per-player 状態が壊れる。
   `SpawnSetRoleDefinition.Custom<T>()` で割り当てごとに新インスタンスにした。
   併せて `RoleTypeId.CustomRole` センチネルと `IsValidDefinition` が不要になり削除
2. **`Scp3005.SpawnPosition` がフィールド初期化子だった** —
   マップ生成前に評価されて `Vector3.zero` に落ちる。都度評価に変更
3. **`CommandBase` の権限ノードが壊れていた** —
   `Assembly.GetExecutingAssembly()` は常に `Sliced` を返し、`AssemblyName.ToString()` で
   `"Sliced, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"` になっていた。
   `GetType().Assembly.GetName().Name` に修正
4. `RoleCommand` が `[CommandHandler]` とサブコマンド登録の両方を持っていた/
   `Arguments[0]` が未ガードだった

---

## Phase 1 — 並列再構築(エージェント)

Phase 0 の build が緑になってから起動する。

### 進捗

| # | ドメイン | 状態 |
|---|---|---|
| 1 | アイテム基盤 `AntiMeme\Items\Bases\` | **[完了]** `CustomWeapon` 213 / `CustomArmor` 171 / `CustomKeycard` 139 / `CustomUsable` 100 = **623 行**(旧 `CItemWeapon` 752 + `CItemKeycard` 249 + `CItemUsable` 178 + `CItemArmor` 393 = 1,572 行)。**EXILED 参照ゼロ** |
| 1' | SCP-914 `AntiMeme\Items\Scp914\` | **[完了]** `Scp914Rule` 177 / `Scp914Context` 94 / `Scp914Handler` 66 / `Scp914RuleSet` 51 / `IScp914Upgradable` 27 = **415 行**(旧 516 行)。**LabAPI の `Scp914Events` で完結**、EXILED 不要 |
| 1'' | `CustomNvg` | 保留(担当 4 とセット)。~~LabAPI 1.1.7 に SCP-1344 のサーバイベントが無い~~ → **誤り。`PlayerEvents.DetectedByScp1344` は実在する**(逆コンパイルで確認)。**Harmony も EXILED も要らない** |
| 2 | キーカード / Access Tuner / Data Cell `AntiMeme\Items\Keycards\` | **[完了]** 19 ファイル **773 行**(旧 1,110 行)。`AccessTuner` のレベルを **mutable なフィールド 1 本**にしたので、旧の「Lv ごとに別カスタムアイテムを立てて `SerialTracker.ForceRegister` で登録型を差し替える」機構(`Services` 辞書 / `GetOrCreateService` / `NormalizeAccessLevel`)が丸ごと消えた |
| 3 | 銃器 `AntiMeme\Items\Weapons\` | **[ほぼ完了]** 22 ファイル **1,015 行**(旧 22 丁 1,829 行)。22 丁中 20 丁。`TunedWeapon` 81 / `HybridWeapon` 76 が土台。**保留 2 丁**: `GunRecoilRampRevolver`(310・反動調整のデバッグ用)/ `GunSoundTestbench`(78・音声層 = 担当 15 待ち) |
| 6 | SCP 役職 `AntiMeme\Roles\Scps\` | **[完了]** `ScpRole` 共通土台 + 12 役職 = **1,341 行**。`Scp076`(459)と `Scp096Anger`(821)は他ドメインに跨るため後続 |
| 13 | マップ **基盤** `AntiMeme\Maps\` | **[完了]** 8 ファイル **1,891 行**(旧基盤 3,349 行)。**LabAPI + ProjectMER のみ**、EXILED 参照ゼロ |
| 13b | ObjectPrefab **具象** + マップ機能 `AntiMeme\Maps\Objects\`, `Maps\Features\` | 進行中。旧 `ObjectPrefabs\` 19 種 3,775 行 + `Features\` 系 2,700 行 + `Core\DoorAccess`/`FemurBreaker`/`SurfaceGate` 618 行。**担当 13 の [完了] は基盤層だけを指していた** |

| 10 | チーム / 勝利判定 `AntiMeme\Teams\` | **[完了]** 5 ファイル **275 行**(旧 `Teams\` 1,383 + `RoundVictory\` 1,127 = **2,510 行**)。脱出ルールのみ後続 |
| 9 | アビリティ `AntiMeme\Abilities\` | **[ほぼ完了]** 17 ファイル **991 行**(旧 16 ファイル 941 行 + `AbilityManager` ほか)。**保留 4 種**: `AllowEscape`(脱出ルール待ち)/ `GenerateWeapon`(担当 3 待ち)/ `MemeWave` / `Scp035Tentacle`(Tentacle prefab = 担当 13b 待ち) |
| 16 | Harmony パッチ / 可視性 / NPC `AntiMeme\Patches\`, `Net\` | **[完了]** **10 ファイル**(旧 15 パッチ 1,624 行 + `NetworkVisibilityManager` ほか)。**残り 1 件のみ**: `Scp049InitiativeSensePatch`(Initiative 役職 = 担当 8 待ち)。`ChaosKeycardSnakeOverride` は担当 15、`Scp1344NvgBlindness` は担当 4 へ移管。`InsanityDeathReason` / `VisualSinkhole` / `VisualTraumatized` は `CustomEffects` 依存で **Phase 2** |
| 11 | スポーン / ウェーブ `AntiMeme\Spawning\` | **[完了]** 9 ファイル。ラウンド開始割り当ては `FirstRolesHandler` + `FirstRoles*`(下記) |
| 17 | **入力層** `AntiMeme\Input\` | **[完了]** 3 ファイル。旧 `ServerSpecifics` 291 + `ServerSpecificUserSettings` 332 + `ServerSpecificsHandler` 368 = **991 行** → **入力 1 つ = クラス 1 つ** |
| 18 | 脱出 `AntiMeme\Teams\EscapeSystem.cs` | **[完了]** 旧 `CTeamEscapeSystem` 157 + `DefaultTeamEscapeRuleSource` 65 + `EscapeHandler` 270。**規則 1 本 = クラス 1 つ**(`EscapeRule` を `TypeParser` で解決)。**性能修正済み**: 規則インスタンスを毎回の脱出で作り直していたのをキャッシュに変更 |

### ラウンド開始の役職割り当て

旧 `FirstRolesHandler`(168 行)は重み表(`WeightedRoleEntry(object Role, float Weight)`)と
上限表(`RoleLimitManager`)を別々に持ち、役職を `object` で入れていた。
**両方 `SpawnSetRoleDefinition` の 1 行に畳んだ** — `weight` が出やすさ、`count` がそのまま上限。

```csharp
SpawnSetRoleDefinition.Custom<Scp173>(),                 // 重み 1・1 人まで
SpawnSetRoleDefinition.Custom<Scp035>(weight: 0.4f),     // 出にくい
SpawnSetRoleDefinition.Vanilla(RoleTypeId.ClassD, count: 99, weight: 4f),
```

`SpawnSet` 側は行の選び方を重み付き抽選に変えただけ(重みが全部同じなら従来どおり等確率)。
バニラの割り当ては `RoleChangeReason.RoundStart` を常に拒否して止める。
旧実装はフラグで開閉していたが、開始時の割り当ては完全にこちらが持つので条件は要らない
(遅参は `LateJoin` なので影響しない)。

### 入力層で消えた構造

旧実装は `SettingKey` enum + `SettingDefinition` のリスト + 受信側の巨大な switch の 3 点セット。
新実装は **`InputBinding` を継承したクラスを書くだけ**で、設定 ID はコードのどこにも書かない
(`InputHandler` が見つけた順に採番し、ID → バインディングの対応を 1 本持つ)。

```csharp
public sealed class UseAbilityKey : InputBinding
{
    public override string Label       => "アビリティ使用";
    public override KeyCode DefaultKey => KeyCode.LeftAlt;
    public override void OnPressed(Player player) => ...;
}
```

配線済み: アビリティ使用 / 切り替え / オプション前後 / 近接チャット / アイテムモード切り替え。

### チーム / 勝利判定で消えた構造

旧構造は 1 チームあたり **DefinitionSource → CTeamDefinition(7 引数)→ RoundVictoryRule →
RoundEndDefinition(9 引数)** の 4 段で、`CTeam` enum と `CTeamRegistry` と
`RoundVictorySystem` / `RoundEndSystem` / `RoundEndDefinitions` / `RoundEndDefinitionKey` が
それを支えていた。新構造は**チーム 1 個 = クラス 1 個**:

```csharp
public sealed class ScpTeam : FactionTeam
{
    public override string Name       => "SCP";
    public override string CassieName => "SCP";
    public override string Color      => "#c50000";
    public override VictoryCondition Victory => VictoryCondition.LastStanding(priority: 40);
    protected override bool IncludesVanilla(Player player) => player.IsSCP;
}
```

所属判定は `FactionTeam` に 1 本化した。**カスタム役職を持っていればその役職が指すチーム、
持っていなければバニラ役職からの判定**。役職 ID → チームの表は要らない。

**旧 SCP 勝利条件の除外述語が丸ごと消えた。** 旧実装は
`GetVictoryTeam(player) == CTeam.SCPs && role is not (Scp3005 or Scp999)` と書いていた。
役職 → チームを表で引いていたので、例外を述語側に書くしかなかった。
新実装では **SCP-3005 が `FifthistTeam` を、SCP-999 が null を名乗るだけ**で済む
(しかも第五教会の旧勝利条件が `|| role == Scp3005` を持っていたので、こちらの方が定義が 1 か所になる)。

勝利判定を回す場所も 1 つ。ゲーム側が「終わってよいか」を訊いてくる
`ServerEvents.RoundEndingConditionsCheck` に乗るので自前タイマーは無い。
旧 `requiresVanillaEndLock` は「独自終了の陣営が生きている間は `CanEnd = false`」に置き換えた。

**後続:** 脱出ルール(`CTeamEscapeSystem` 157 行 + `DefaultTeamEscapeRuleSource` 65 行)は
`FifthistPriest` / `GoCOperative` / `FifthistConvert` など未実装の役職に依存するため保留。
陣営プロファイル(`CTeamProfileDefinition` 420 行)は Facility Termination 用なのでゲームモード担当と一緒に。
実体のない 9 チーム(AWCY / BlackQueen / BrokenGodChurch / Moderators / Null / O5 / Sarkic /
SerpentsHand / UIU)は旧実装でも 13 行のスタブだったので作っていない。

現在: `Sliced` 3,232 行 / `AntiMeme` 4,667 行(チーム層追加前の計測)。

### 決定: ObjectPrefab の JSON 永続化は復活させず、マーカーへ移行する

旧 `ObjectPrefabPersistence`(264 行)と `LoadMapStaggered("aaa")` は作り直さない。
配置データは ProjectMER マップの `object_prefab_markers:` へ移す。

**移行はすでに大半が済んでいた。** `MapWorks\Maps\aaa.yml` には
`object_prefab_markers:` セクションがあり、12 個(`AuthDoor` / `HIDTurretObject` /
`AccessTunerBox` x2 / `document` x2 / `usefuldoorbutton` x2 / `vent` x2 / `UsefulDoor` /
`EzShelterElevator`)が既に稼働している。`aaa.json` の 18 件だけが旧経路に取り残されていた。

内訳は `Document` 9 件 + `Trashbox` 9 件。変換結果は
**`AugustTest\migration\aaa-object-prefab-markers.yml`** に生成済み。

**変換の根拠(逆コンパイルで確認):**
- ProjectMER の `room:` は EXILED の `RoomType` 名(`RoomExtensions.FindRoomTypeName()` が
  `FindRoomType().ToString()` を返す)。`MapGeneration.RoomName` ではない。
  `aaa.json` の 14 種の部屋名はすべて `RoomType` に存在する
- 位置の意味も一致する。旧 `TrySpawnPrefab` は
  `worldPos = room.Position + room.Rotation * LocalPosition`、
  ProjectMER の非屋外ルームは `room.Transform.TransformPoint(position)` で同じ

**残る作業 2 点:**
1. **`Surface` の 4 件は実行時変換が要る。** ProjectMER の `IsOutsideRoomId` は
   `Unknown` / `Surface` / `Outside` を屋外扱いし、`position` を**絶対座標**として使う
   (稼働中の `HIDTurretObject` マーカーが `y=307.569` なのがその証拠)。
   一方 `aaa.json` の Surface 4 件は室内相対値(`y` が -49 / -72 / -39 / +6)。
   `absolute = SurfaceRoom.Position + SurfaceRoom.Rotation * local` を実機で 1 回計算して差し替える
2. **`options:` のキー名は新プレハブ実装時に確定する。** 旧 JSON は
   `DocumentType` + `ShowModel`、稼働中のマーカーは `DocumentType` + `ModelSchematicName` で
   食い違っている(ObjectPrefab v1/v2 の差)。`Document` / `Trashbox` を再構築するときに合わせる

**MapWorks への書き込みは未実施。** 別リポジトリなので、上記 2 点が片付いてから
独立したコミットとして入れる。

### Phase 1 で判明した Sliced の不備(修正済み)

1. **`CustomItem.Spawn<T>` が二重スポーンしていた。** `Pickup.Create` は既定
   (`networkSpawn: true`)で `NetworkServer.Spawn` まで済ませるのに、その後さらに
   `pickup.Spawn()` を呼んでいた。`networkSpawn: false` で作り、`Customize(pickup)` の後に
   `Spawn()` する順に修正。Mirror の SpawnMessage はスケールを含むので、
   この順序なら各アイテムが UnSpawn → 変更 → Spawn を書かずに済む
2. **拾得イベントは種類ごとに別。** `PickedUpItem` だけではアーマーと SCP-330 キャンディを
   取りこぼす。`PickedUpArmor` / `PickedUpScp330` も Sliced が購読するようにした
3. **使用完了は `UsedItem` では捕まらない。** `ItemUsageEffectsApplying` が
   バニラ効果を差し替えられる唯一の地点で、そこで `IsAllowed = false` にすると
   `UsedItem` は発火しない。`OnUseCompleting(ev)` を追加した

4. **`TypeParser.TryParse` が同名型を黙って選んでいた。** `type.Name` 一致の `FirstOrDefault` なので、
   別々の名前空間に同名の役職/アイテムがあると RA コマンドやマップデータの指す先が不定になる
   (実際に `Scp3005` がサンプルと新実装で重複した)。**曖昧なときは失敗させる**ようにし、
   完全修飾名で 1 つに絞れる場合だけ通す
5. **型引数を取らない生成 API がなかった。** 文字列から型を引く経路(マップデータ・RA コマンド)で
   `MakeGenericMethod` が必要になっていた。`CustomItem.Give(Type, Player)` /
   `Spawn(Type, ...)` / `Adopt(Type, ushort)` を追加し、マップ層のリフレクションを削除
6. **`CustomRole` の宣言的記述に `SpawnRotation` / `Scale` が無く、書き換えたバニラ状態を戻す口も無かった。**
   両方を追加し、`Scale` と `CustomInfo` は役職が外れるときに基底が戻すようにした
   (基底が書いたものは基底が戻す)。`HintDuration` も追加

### マップ層で落とした旧実装(抜粋)

利用実態を実測してから削除している点が重要:

- **`ObjectPrefabRegistry` 全体(345 行)** — Key/DisplayName/Aliases/fuzzy 解決/Descriptor。型で解決できる
- **`Option` / `OptionPart`(274 行)** — `new Option(...)` の利用者は `AccessTunerBox` **1 クラスだけ**。
  素の int/bool プロパティで書ける
- **半径フォールバック配送(`ToySearchRadius`)** — 設定箇所 **0 件**の死んだ経路
- **`OnRoundStarted` / `OnRoundRestarting` フック** — override が **0 件 / 1 件**。
  後者は `RoundScope` の破棄で足りる
- **`MapFlags` の Vector3 フィールド 30 個 + 100 行の switch** — `MapPoints` の辞書に置換
- **`IObjectPrefab` / `IControllableObject` / `ISpawnableObject`** — 実装が 1 つずつしかない飾り
- 利用者 0 のメンバー 9 種(`GetBlockComponent` / `DestroyBlock` / `SetManagedSchematic` ほか)

**直した旧実装のバグ:** `ObjectPrefabInstances.ClearAll()` が全 Prefab を `Destroy()` した後で
辞書を `Clear()` していた。`Destroy()` 側も `Unregister` するので二重管理で、
片方だけ呼ぶと索引が壊れる。生存リスト 1 本にして解消。

### SCP 役職で落とした旧実装(抜粋)

- **`RoleSpawnTimings` の待ち時間と `WaitAndTeleport` の 10 秒ポーリング**
  (173/682/3005/999/3114 で重複)— `CustomRole.SpawnPosition` が一度で決める
- **SCP-999 の「173 でスポーン → 遅延で Tutorial へ差し替え → 位置と装備を再適用」三段構え**
  — 旧パイプラインの上書き対策。最初から Tutorial にすれば要らない
- **静的辞書と手動クリーンアップ** — `Scp106` の 2 段辞書 + 死亡/役職変更/切断 3 経路の掃除、
  `Scp035` の「インスタンス辞書 + 全体辞書 + FrozenPlayers」3 重管理。
  すべてインスタンスのフィールドと `PlayerScope` に置換
- `IsSafePlayerTarget` の `try/catch` 包み(173/682/3005 に同じコードが 3 回)

**直した旧実装のバグ:** SCP-3114 の擬態時間を `Disguised` イベント内で書き換えていたが、
ゲーム側は状態変化時に `_disguiseDurationSeconds` を読んでカウントを開始するので**後から書いても効かない**。
スポーン時に 1 回設定する形に修正。

### Harmony パッチで落とした旧実装

**`DummyActionContextPatch` は移植しない。** 旧リポジトリ全体を検索した結果、
`DummyActionContextPatch` → `DummyActionInvocation` → `DummyActionProvider` →
`DummyActionExtensions` の連鎖は**互いを参照し合うだけで、外からの利用者が 1 人もいない**。
RemoteAdmin の `action` コマンドに押下者と選択 Dummy を渡す仕組みだが、
それを受け取るコールバックがどこにも登録されていない。丸ごと死んだコード。

`InteractableToyColliderPatch`(16 行)と `ScpCrossFactionCombatPatch`(227 行)は移植済み。
後者は「バニラ上は同じ SCP 陣営だが、こちらの分けでは敵同士(SCP 対 第五教会)」を
成立させるために当たり判定・ダメージ・SCP-173 スナップ・SCP-096 攻撃の 4 か所に当てている。
陣営の判定が**チーム自身に訊くだけ**になったので、旧実装の役職 ID → 陣営の対応表は消えた。

### キーカードで直した旧実装のバグ

1. **`AccessTunerBase.TryHackDoor` の「ハック不要」判定が壊れていた。**
   `KeycardAccessLevels.FromPermissions` の 3 軸がすべて 0 でも、`ScpOverride` などのフラグが立った扉は
   「権限が要る」ままだったので通していた。`KeycardLevels.HighestLevelValue == 0` と
   `Door.Permissions == None` の両方で弾くようにした
2. **`ApplyDataCell` のポイント移行が下向きにしか働かなかった。**
   `TunePoints = Math.Min(current.TunePoints, GetMaxPoints(targetLevel))` なので
   Lv1(上限 25)→ Lv3(上限 100)へ強化しても 25 のまま。
   「Lv.3 にしたのに 20 必要な特殊扉が開いたり開かなかったりする」症状になる。
   ヒント文が「ポイントが最大になりました」なので、上限まで充填するのが本来の意図。そちらに直した
3. `DataCellBase.ConsumePickup` が `pickup` を二重取得したうえ、`Timing.CallDelayed(0.1f)` の後に
   破棄済み Pickup を触っていた。イベント側で `Destroy()` すれば足りる
4. **`KeycardA.cs` / `KeycardChaosIntruder.cs` の `item.As<Keycard>()?.Permissions = ...` は
   `?.` の左辺代入で、C# としてコンパイルが通らない。**
   旧ソースのこの部分は実際にはビルドされていない可能性が高い
   (`GunDisarmerRifle.cs` の `ev.Player?.CurrentItem = ev.Item;` も同じ形)

**意図した仕様変更(見た目に出る):** `KeycardChaosIntruder` と `AccessTuner` の土台を
`KeycardChaosInsurgency` → `KeycardCustomSite02` に変えた。権限は Detail に焼かれる方式で、
バニラカードの Detail は差し替えられないため、**権限を自前で決めるカードはカスタムカードでないと成立しない**。

### 銃器で消えた構造

旧 `CItemHybrid` は親アイテムが `BuildSubModes()` で子インスタンスの一覧を持ち、
親子でシリアルを張り替える作りだった。新 `HybridWeapon` は**モードそのものがアイテム**で、
各モードが `NextMode` で次を指して輪になる。親という概念も一覧も無い。

**`Mindblaster` が 411 行 → 121 行になった。** 旧実装は「発射可能カード」と「チャージ中カード」を
別アイテムとして持ち替えていたため、`RuntimeState` 辞書 / `TryChangeInventoryMode` /
`_pendingRuntimeState` / `SerialTracker.ForceUnregister` とシリアル張り替えだけで大半を使っていた。
アイテム 1 個 = 1 インスタンスなので、チャージ状態は `bool` 1 つ、見た目は
`CustomKeycard.Refresh()` の呼び直しで済む。**持ち替えが要らないので張り替えも消えた。**

同じ理由で `GunXE11KMR` のグレネードランチャーモードの連射制限も、
`static Dictionary<Player,int>` + 1 秒ごとに全員分を減算するコルーチンから `float` 1 つになった。

**旧実装の再帰防止フラグが不要になった。** GoC レールガン 2 種は
「ダメージを 0 にしてから `Hurt` で撃ち直す」形だったので自分のダメージイベントを自分で踏み、
それを防ぐ `_isProcessing` を持っていた。`StandardDamageHandler.Damage` をその場で書き換えれば
撃ち直しも再帰もフラグも要らない。

**入力層が無いのでモード切替は投擲操作に割り当てた。** 旧実装はキー入力
(`EnableKeyModeSwitch`)を使っていたが、Server Specific Settings は Phase 1 の担当割りに無い。
入力層が入ったら `HybridWeapon.SwitchMode()` をそこから呼ぶだけで移せる。

**保留(記録):** ピックアップの発光(`PickupLightEnabled` / `PickupLightColor`)と
ピックアップのスキマティック差し替え(`PickupSchematicName`)は全銃で未実装。
どちらも `CustomWeapon.Customize(Pickup)` の 1 か所に足せば全丁へ一様に効くので、
中途半端に一部だけ入れていない。

**旧実装の食い違い(未修正・要判断):** `GunGoCRailgunFull` は説明文が「最大 15000 ダメージ」だが
コードは 5000。コード側の値をそのまま移した。

### アイテム基盤で落とした旧実装(抜粋)

- `CItemUsable` の `Dictionary<ushort,int> _remainingUsesBySerial` → **ただの `int` フィールド**。
  アイテム 1 個 = 1 インスタンスなので辞書が要らない
- **EXILED#718 の回避は不要**と結論。あのバグは `Exiled.CustomItems.CustomKeycard` 固有で、
  Sliced の `CustomItem` は最初から自前シリアル追跡なので原因側が存在しない
- `CItemArmor.OnPickingUp` の「拾得をキャンセルして `Give` で差し替える」ハック → 不要。
  ゲーム側 `ArmorSearchCompletor.Complete()` が `ServerAddItem(..., TargetPickup.Info.Serial, ...)` で
  **シリアルを引き継ぐ**ことを確認
- `ReloadAmmoMultiplier` と決定論的リロード上書き(約 60 行)→ 除外。ゲーム本体の
  `MagazineModule.ServerLoadAmmoFromInventory` が `AmmoMax` を見るので、
  カスタム容量だけならバニラのリロードで正しく動く
- 銃器サウンド上書き機構(約 240 行)→ 音声層(担当 15)の再構築後にその上へ載せる
- 利用者 0 の設定 6 種(`ClearAttachmentsBeforeApplying` / `RequireAmmoDrainAvailableToShoot` /
  `AllowEffectAfterUsesDepleted` / `OnUsesDepleted` / `DamageReductionOverrides` ほか)

**直した旧実装のバグ:** 旧 `ApplyFirearmStats` は `CustomizeItem` のたびに `InitialMagazineAmmo` を
焼いていたため、**銃を落として拾い直すと弾が満タンに戻っていた**。生成時 1 回だけに限定。

### SCP-914 で消えた構造

旧 `Scp914Rule` は `Scp914RuleKind` enum + 7 つの nullable フィールドを持つ**タグ付き共用体**で、
`Scp914Dispatcher`(266 行)側に巨大な switch があった。
新実装は**規則そのものが処理を持つ**(各ファクトリが自分の振る舞いを閉じ込める)ので、
enum も switch も存在しない。

規則の出どころも 2 つに減った。
- カスタムアイテム → `IScp914Upgradable` を実装して**自分で持つ**
- バニラアイテム → `Scp914Handler.VanillaRules` の表

これで旧 `Scp914Registry`(100 行、ID による登録・解決)が丸ごと不要になった。

### 全エージェント共通の契約

1. **`Sliced/` は変更禁止。** 不足を見つけたら実装せず報告する
2. **自分の担当フォルダ以外を触らない**
3. 旧実装を**読んで仕様を抽出**するが、構造は引き写さない。**移植ではなく再構築**
4. API は記憶で書かず、`$(SL_References)` の実アセンブリか `ilspycmd` で実在を確認する
5. LabAPI で可能なものは LabAPI。`Mirror` / `ReferenceHub` / `PlayerRoleManager` / Harmony を
   触る箇所はフレームワーク非依存なのでほぼそのまま移す
6. 完了条件は `dotnet build .\AugustTest.slnx --configuration Release` が通ること
7. `Config.cs` にフィールドを足さない。1 機能でしか使わない値は所有クラスの
   `const` / `static readonly`
8. 上記「設計原則」に従う。**迷ったら短い方**

### 担当割り

| # | ドメイン | 新規配置 | 旧実装(仕様の出所) | 規模 |
|---|---|---|---|---|
| 1 | アイテム基盤 + SCP-914 ルール | `AntiMeme/Items/Bases/`, `Items/Scp914/` | `API\Features\CItem*.cs`, `API\Features\Scp914\` | 2.4k |
| 2 | キーカード + Access Tuner + Data Cell | `AntiMeme/Items/Keycards/` | `SlafightApiItems\Keycard*.cs`, `AccessTunerBase.cs`, `DataCells.cs` | 1.4k |
| 3 | 銃器 | `AntiMeme/Items/Weapons/` | `SlafightApiItems\Gun*.cs`, `Mindblaster.cs` ほか | 2.6k |
| 4 | 医療・ユーティリティ・SNAV・NVG・電池 | `AntiMeme/Items/Utility/` | Usable 系, `ScpcbBatteries.cs`, `NvgManager.cs` | 2.2k |
| 5 | 近接・投擲・アーマー | `AntiMeme/Items/Melee/`, `Items/Armor/` | `Spear/BattleAxe/ThrowKnife/…`, `Armor*.cs` | 1.1k |
| 6 | 役職基盤 + SCP 役職 | `AntiMeme/Roles/Scps/` | `API\Features\CRole*.cs`, `CustomRoles\SCPs\` | 3.9k |
| 7 | 財団戦力 | `AntiMeme/Roles/Foundation/` | `CustomRoles\FoundationForces\`(26) | 1.0k |
| 8 | カオス・第五主義・GoC・季節戦士ほか | `AntiMeme/Roles/Factions/` | `CustomRoles\` 残り | 2.7k |
| 9 | アビリティ | `AntiMeme/Abilities/` | `Abilities\`(16), `API\Features\Ability*.cs` | 1.6k |
| 10 | チーム / 勝利判定 / 脱出 ⚠**設計原則の主戦場** | `AntiMeme/Teams/` | `Teams\`, `RoundVictory\`, `EscapeHandler.cs` | 2.5k |
| 11 | スポーンシステム / ウェーブ | `AntiMeme/Spawning/` | `SpawnSystem.cs`, `SpawningHandler.cs` ほか | 2.0k |
| 12 | ゲームモード | `AntiMeme/GameModes/` | `SpecialEvents\`(**廃止 5 種は除く**) | 4.9k |
| 13 | マップ / ObjectPrefab / ProjectMER | `AntiMeme/Maps/` | `CustomMaps\`, `ObjectPrefab*.cs`, `UDoor.cs` | 13k |
| 14 | HUD / ヒント / CASSIE(HSM) | `AntiMeme/Hud/` | `Hints\`, `BossBar.cs`, `CassieHelper.cs` | 3.9k |
| 15 | 音声 / 近接チャット / Snake(SNAPI) | `AntiMeme/Audio/`, `Snake/` | `Speaker*.cs`, `Ffmpeg*.cs`, `Snake*.cs` ほか | 7.5k |
| 16 | Harmony パッチ / 可視性 / NPC | `AntiMeme/Patches/`, `Net/` | `Patches\`(15), `NetworkVisibilityManager.cs` ほか | 3.4k |

**依存順:** 1・6・13 を先行させ、完了後に残り 13 を並列起動する。

---

## Phase 2 — 統合・大型機能 [完了]

| 項目 | 状態 |
|---|---|
| カスタム効果 `AntiMeme\Effects\` | **[完了]** 14 ファイル。**Insanity は 2,900 行超**(Content 1,538 / Layers 538 / Trip 500 / Effects 185 / 本体 168)。フェーズ定義・テキストバンク・3 層描画・バースト種別・悪化プロファイルは**旧実装のまま** |
| DANTE Battle `GameModes\DanteBattle.cs` | **[完了]** 1,124 行(旧 1,405 行) |
| 脱出 `Teams\EscapeSystem.cs` | **[完了]**。規則 1 本 = クラス 1 つ |
| Insanity 依存パッチ 3 種 | **[完了]** `VisualSinkholePatch` / `VisualTraumatizedPatch`(SCP-106 攻撃の transpiler 含む)/ `InsanityDeathReasonPatch` |
| Discord 連携 `Net\Discord.cs` + `DiscordHandler.cs` | **[完了]**。**LabAPI のイベントだけで組めた**(`PlayerEvents.Kicked` / `ReportedCheater` / `ReportedPlayer` / `ServerEvents.BanIssued`)。旧実装が Kick と Ban の二重発火を「次の Kick を抑制するセット」で捌いていた回避策は不要になった |
| `Changes\` レイヤー | **[完了]** SCP-914 変換表は `Items\Scp914\VanillaScp914Rules.cs`、キャンディプールは `Patches\CandyPoolPatch.cs` として各ドメインへ分散。旧のような横断レイヤーは作らなかった |

### Insanity を補完したときの注意

エージェントが Layers と Content を書いた時点でセッションが切れ、**駆動部が丸ごと欠けていた**
(効果が動作しない状態)。本体・付随エフェクト管理・進行コルーチンを後から補完した。
移植で新 API に合わせたのは 2 点だけ:

- `PlayerDisplay.Get` は LabAPI の `Player` を直接受けるオーバーロードがあるのでそれを使う
  (EXILED 版の `GetPlayerDisplay()` 拡張は EXILED `Player` 専用)
- EXILED 拡張の `RemoveUnityRichTextTag` は自前の `StripTags` に置換

---

## Phase 2 の元計画(参考)

1. コマンド層(`CommandBase` に `ParentCommand` 基底と自動登録を追加)
2. カスタム効果 — **Insanity(3,341 行)のフェーズ定義・テキストバンク・3 層描画は現行を尊重**し、
   購読とコルーチンだけ `PlayerScope` に載せ替える
3. DANTE Battle(1,405 行) — 同上。3 幕進行・触手・BossBar のロジックは保持
4. `Changes\` レイヤー(SCP-914 変更表、ロビー設定、キャンディプール、季節変更)
5. Discord 連携
6. プラグイン結線と最終ビルド

### 切り捨てるもの

- 廃止済み SpecialEvent 5 種(`Old_DeltaWarhead` / `RevolverBattles` / `OperationBlackout` /
  `DailyFoundation` / `ClassicEvent`)
- `Exiled.CustomItems` / `Exiled.CustomRoles` 残骸(派生クラスは既にゼロ、
  `CustomItemsManager.cs` は空スタブ、`Plugin.cs` の呼び出しは死んでいる)
- 空スタブ(`VoiceRecordingApi.cs` 8 行 / `VoiceRecordingCommand.cs` 0 行)

デバッグ専用アイテム(`GunRecoilRampRevolver` / `GunSoundTestbench`)は**残す**。

---

## 検証

```powershell
dotnet build D:\RiderWorks\AugustTest\AugustTest.slnx --configuration Release
```

自動配備先:
- `%APPDATA%\SCP Secret Laboratory\LabAPI\plugins\7778\Sliced.dll`
- `%APPDATA%\EXILED\Plugins\7778\AntiMeme.dll`

手動で置くもの(いずれも `D:\RiderWorks\SL_References` が正)。**配置済み**:
- `HintServiceMeow-Exiled.dll` / `SNAPI-HSM.dll` / `Snake.dll` → `%APPDATA%\EXILED\Plugins\7778`
- `ProjectMER.dll` / `MEROptimizerLabAPI.dll` → `LabAPI\plugins\7778`

**注意: LabAPI 側に `AntiMeme.dll` を置かないこと。**
Phase 0 で EXILED プラグイン化する前の古い DLL が `LabAPI\plugins\7778` に残っていた。
そのままだと LabAPI と EXILED が**同一 AppDomain に AntiMeme を二重ロード**し、
ハンドラも Harmony パッチも二重になる。削除済み。
LabAPI 側に置くのは `Sliced.dll` だけ。

### 実機確認(未実施)

1. ポート 7778 で起動し `%APPDATA%\SCP Secret Laboratory\LocalAdminLogs\7778\` を確認
2. **最優先**: `[Sliced] 自動登録対象を検出しました: 常駐 N 件 / ラウンド M 件` で
   N・M が 0 でないこと(= EXILED ロードのアセンブリが走査されている証拠)
3. **ラウンド開始の役職割り当て**: SCP 枠が人数に応じて 1〜4 になり、
   バニラの割り当てが二重に走っていないこと
4. **入力層**: 設定画面にキーが並び、アビリティ使用・切り替え・近接チャットが効くこと
5. RA コマンドで役職スポーン・アイテム付与・イベント実行
6. ラウンド再開を 2 回以上回し、`HandlerLifetime.Round` のハンドラが破棄・再生成されること、
   静的辞書が残留しないことを確認
7. **7777 と旧リポジトリが無変更であること**を `git status` で確認

### 実機確認が要る挙動変更

- **Mindblaster のダメージ量**。旧実装は毎フレーム当たり判定していて、半径 1m 内に居続けると
  0.8 秒間 30 ダメージ + 効果付与が連続で入っていた。1 人 1 回に修正したので実質の威力が変わる
- **ラウンド開始の役職分布**。重み付き抽選に変えたので、旧の重み表と体感が揃っているか
- **Surface のマーカー 4 件**(`migration\aaa-object-prefab-markers.yml`)。
  絶対座標への変換が要る

---

## 引き継ぐ不変則(旧 AGENTS.md 由来)

- 生 Mirror 送信では `connection.isReady` だけでは不十分。実クライアントは
  `hub.Mode == ClientInstanceMode.ReadyClient` も必要
  (LabAPI の `Player.IsReady` は `InstanceMode != Unverified && NickSet` であって
  `ReadyClient` を見ていない)→ `NetGuards` に集約。適用範囲は Snake 表示とロールシンクだけ
- `IsNPC` フィルタを非プレイヤー向けシステムに安易に足さない。多数のカスタム役職・タレット・
  ヒットボックス・スキマティック相互作用が**意図的に NPC を使う**。
  判定はヒューリスティックでなく「自分で生成した NPC を覚えておく」実行時セットで行う
- HSM がヒント配信の最終防衛線。HUD ループごとに NPC チェックを撒かない
- 登録/解除・購読/解除・パッチ/アンパッチ・開始/停止の対称性を保つ
- ProjectMER はフォーク。upstream の例ではなく
  `D:\RiderWorks\SL_References\ProjectMER.dll` を見る
  (`SchematicObject.GetPrefabManagedBlocks()` / `SchematicBlock.ObjectPrefabKey` はフォーク固有)

## 触らないもの

- `D:\RiderWorks\Slafight_Plugin_EXILED\` — 完成・切替まで読み取り専用
- ポート 7777 の実行時フォルダ
- `%APPDATA%\SCP Secret Laboratory\LabAPI\configs\ProjectMER`(MapWorks の Git 作業ツリー)

---

## Phase 1 の棚卸し(2026-08-05)

Phase 1 の担当 16 ドメインは**全部書き終わっている**(棚卸し時点で `AntiMeme` 20,928 行 /
`Sliced` 2,890 行)。
ただし引き継いだ時点で `dotnet build` が**赤**だった。詰まっていたのは 2 件だけで、どちらも
「型名の衝突」= コンパイラが 1 回でも通っていれば出ないもの。

1. `Spawning\FirstWaveScheduler.cs` — `Exiled.API.Features` と `LabApi.Features.Wrappers` を
   両方 using していて `Player` が CS0104。使っていたのは `Respawn` 1 つだけだったので
   using を落として `Exiled.API.Features.Respawn` に完全修飾
2. `Spawning\SpawnSystem.cs` — `System` と `UnityEngine` の `Random` が CS0104。
   `UnityEngine.Random.Range` に完全修飾

### 設計原則との突き合わせ(実測)

| 指標 | 旧 89,539 行 | 新 20,928 行 | 判定 |
|---|---:|---:|---|
| null 比較 | 46 行に 1 回 | **68 行に 1 回** | 目標より良い |
| `try` / `catch` | 217 行に 1 回 | **343 行に 1 回** | 目標より良い |
| `Player.List` + 手書きフィルタ | 188 箇所 | **3 箇所** | ほぼ `ReadyList` へ移行済み |

残っている `Player.List` 3 箇所のうち `Net\NetworkVisibility.cs` と `Items\Utility\Snav.cs` は
NPC を含めたい側なので意図的。

### 未解消の設計逸脱(次に直す)

1. **`Spawning\SpawnContextRegistry`** — 「レジストリを新設しない」に真っ向から反する。
   string キー・`Register` / `TryGet` / `SetActive` を持つ。中身は 2 個
   (`Default` / `FacilityTerminationCustom`) しかないので、`SpawnContext` を型にして
   `GameMode` 側が直接指せば辞書も文字列比較も消える
   (`ActiveContextName != "Default"` の文字列比較が 4 箇所ある)
2. **`SpawnSystem.Disable` が `public static new bool`** — 基底 `EventHandlerBase.Disable()`
   (インスタンスメソッド)を隠している。外から `spawnSystem.Disable()` が書けない。
   `IsSuspended` へ改名する
3. **プレイヤーをキーにした static Dictionary が 2 本**
   (`Abilities\FpcPush.Running` / `Items\Utility\SuspiciousTablet.PendingPlayers`)。
   退出時に消える保証が無いので `PlayerScope` に載せ替える
4. `SpawnSystem.RemoveNextSpawnOverride` / `ClearNextSpawnOverrides` の `reason` 引数が未使用

### 仕様の厚みが落ちている箇所(要判断)

再構築なので行数の圧縮自体は正しいが、以下は**演出が丸ごと落ちている可能性**がある。
実機で回して判断する。

| 旧 | 行数 | 新 | 行数 |
|---|---:|---|---:|
| `WaterWarriorsAttack` | 803 | `GameModes.cs` 内 | 33 |
| `CandyWarriorsAttack` | 221 | 同上 | 30 |
| `MainHandlers\WearsHandler` | 966 | `Maps\RoleWear.cs` | 137 |

---

## Phase 2 — 進捗

### 2-1. コマンド層 [完了]

**`Sliced`:**

| ファイル | 内容 |
|---|---|
| `API/Features/ParentCommandBase.cs` (新規 102) | 子コマンドを**自分で集める**親コマンド。`ClearCommands()` → `TypeParser.FindTypes<CommandBase>()` → `Parent` がこの型のものを `RegisterCommand`。同名は例外(`CommandHandler.RegisterCommand` は `Dictionary.Add`)なので先に弾く |
| `API/Features/CommandBase.cs` | `Parent` / `Usage` / `IsAllowedFor(sender)` を追加。`Permission` を `protected` → `public`(親がカタログを絞り込むため)。引数ヘルパ `TryGetPlayer` / `TryGetInt` / `TryGetFloat` / `GetRemainder` を追加 |
| `API/Features/AbilityBase.cs` | `Give(Type, Player)` を追加。`CustomItem.Give(Type, Player)` と同じ理由(文字列から型を引く経路で `MakeGenericMethod` を書かせない) |

**登録の一覧は親に書かない。** 旧 `RootCommand` は `RegisterCommand` を 30 行並べていた。
新実装は子側が `public override Type Parent => typeof(RootCommand);` と書くだけ。

**`AntiMeme/Commands/`** — 11 ファイル 600 行(旧 `Commands\` 5,506 行):

| コマンド | 内容 |
|---|---|
| `am` | 引数なしで、送信者が実行できる子コマンドのカタログ |
| `am help <cmd>` | 使い方・別名・権限ノード・実行可否。権限不要 |
| `am list roles\|items\|abilities\|teams\|modes\|prefabs [絞り込み]` | **型名**を列挙する。ここに出る名前がそのまま他コマンドの引数 |
| `am status` | ラウンド / モード / 予約 / スポーン文脈 / ウェーブ上書き / 初回ウェーブ / 役職・アイテム・Prefab 数 / **自動登録ハンドラ数** |
| `am spawn <役職> [対象]` | 旧 `RoleCommand` を置き換え(`role` は別名として残す) |
| `am give <アイテム\|ItemType> [対象]` | カスタム優先、無ければバニラ `ItemType` |
| `am ability <アビリティ> [対象] [use]` | 付与、`use` でその場発動 |
| `am mode <run\|queue\|stop\|clear\|roll> [モード]` | |
| `am wave <now\|next\|clear> [SpawnTypeId] [mini]` | |
| `am prefab <spawn\|list\|clear> [型名] [名札]` | 足元に生成。本番配置は ProjectMER のマーカーが持つので、これは当たりを付ける道具 |
| `am restart [fast]` | |

権限ノードは `AntiMeme.<コマンド名>`(旧 `slperm.<コマンド名>`)。

**旧 DevTools から落としたもの:** `HitboxCommand`(1,261)/ `ObjectPrefabTools`(1,268)/
`FilmmakerAnimationCommand`(201)/ `WaypointStreamCommand`(110)/ `PlayAudioHere`(298)。
合計 3,138 行。**制作用ツールであってゲームの機能ではない**ので、必要になった時点で
必要なものだけ足す。`PlayOmegaWarhead` / `PlaySurfaceAttack` / `PlayWaterWarriorsFlood` /
`RunEvent` / `ReRollSpecial` は `am mode run` に吸収された。

### 2-2. SCP-914 変換表 [完了]

**引き継いだ時点で `Scp914Handler.VanillaRules` が空だった。** 器だけあって表が無く、
カスタムアイテム側も 6 個しか `IScp914Upgradable` を実装していなかったので、
**SCP-914 は実質バニラ挙動のままだった**。旧 `Changes\Scp914Changes.cs`(855 行)から
仕様を移した。

| ファイル | 行数 | 内容 |
|---|---:|---|
| `Items/Scp914/VanillaScp914Rules.cs` (新規) | 287 | バニラ 25 種の変換表 + O5 ワイルドカード(全アイテム 0.2%)+ 床限定ワイルドカード(1/42 で SCP-513 / カピバラミサイル)+ `WithO5` |
| `Items/Scp914/Scp914Detonation.cs` (新規) | 27 | 「入れた本人ごと吹き飛ぶ」当たり枠。旧実装は HE ピックアップを 5 個作って `Explode()` していたが、実体を作る意味が無いので `ExplosionUtils.ServerExplode` を 5 回 |
| `Items/Scp914/Scp914Handler.cs` | 96 | ワイルドカードを表より先に判定。`OnScp914ProcessingPlayer` で VeryFine 25% ゾンビ化(見た目・効果は `Roles\Scps\Zombified` が持つ) |
| カスタムアイテム 36 クラス | — | `IScp914Upgradable` を実装(既存 6 と合わせて 42)。**規則はアイテム自身が持つ**ので登録も ID も無い |

旧実装との差:

- 旧 `Scp914Registry`(100 行、ID による登録・解決)と `Scp914Dispatcher`(266 行、巨大 switch)は
  復活させていない。規則そのものが処理を持つ形のまま
- 旧 `RuleSet.All`(未指定の設定の既定値)は無い。畳んで各設定に展開した
- 旧のゾンビ化は `UniqueRole` 文字列 + `SetCustomInfo` + `Handler.CanUsePlayers` 手書きだった。
  `CustomRole.Spawn<Zombified>()` に置き換えたので、役職としての後始末が基底に乗る

### 2-3 以降(未着手)

| # | 内容 | 旧の出所 | 規模 |
|---|---|---|---:|
| 3 | カスタム効果 — Insanity | `CustomEffects\Insanity*.cs` | 2,875 |
| 3' | 同 — `DamageBoost` / `FloodDrowning` / `NaturalHeal` / `VisualSinkhole` / `VisualTraumatized` / `CustomStatusEffectsRegistry` | `CustomEffects\` 残り | 553 |
| 4 | DANTE Battle | `SpecialEvents\Events\DanteEvent.cs` | 1,193 |
| 5 | `Changes\` の残り — ロビー設定 / キャンディプール / 季節変更 / 施設ライト / SCP-1509 / SCP 権限 | `Changes\`(`Scp914Changes` と `EscapeHandler` を除く) | 1,058 |
| 6 | `MainHandlers\` の残り — Round / Moderation / ServerSpecifics / RPName / Badge / Abel / SpecificFlags / Preload | `MainHandlers\`(移植済みを除く) | 1,430 |
| 7 | Discord 連携 | — | — |
| 8 | プラグイン結線と最終ビルド | — | — |

**6 の `ServerSpecificsHandler`(317 行)は先に片付ける価値がある。**
入力層が無いせいで `HybridWeapon` のモード切替が投擲操作に割り当てられたままになっており、
`AbilityBase.TryUse` を呼ぶ口も無い。

---

## 最終レビュー: 基底クラス階層と冗長性の整理 [完了]

「TeamRole のような中間クラスは要るのか」という問いから、全ドメインの中間クラス・
配置・死にコードを横断で監査した。

### 1. `TeamRole` を廃止し、中身を `Sliced` の `CustomRole` へ移した

`TeamRole` が持っていた 5 つのヘルパー (`IsMine` ×2 / `Hook` / `ShowStatus` /
`SetHumeShield` / `BoostHumeShieldRegen`) は、いずれもこのサーバー固有ではなく
**どのカスタム役職にも当てはまる**。`CustomRole` に置くのが正しい層だった。

- `AntiMeme/Roles/TeamRole.cs` — 削除
- 陣営基底 12 個は `CustomRole` へ直接付け替え
- 陣営基底そのものは**残す**。1 行で 2〜32 役職の所属を決めており、空の中継ではない

### 2. 派生 1 個の中間クラスを畳んだ

| 畳んだもの | 理由 |
|---|---|
| `TunedWeapon` → `CustomWeapon` | `CustomWeapon` の直接派生が `TunedWeapon` だけだった。`OnHit` / `OnReloading` / `OnReloaded` / `SetRecoil` を `CustomWeapon` へ移し、購読も既存の `Hook()` に合流 |
| `InitiativeRole` → `InitiativeWolf` | 派生が 1 個だけの陣営基底。ファイル名 (`InitiativeRoles.cs`) も中身と不一致だった |

### 3. `RecoilRampMode` の 12 派生を表に戻した

`GunRecoilRampRevolver` は「調整対象 (6) × 向き (2)」の 12 段を、
`HybridWeapon` のモードクラス 12 個の輪として表現していた。
だが 12 モードすべてが同じ `ItemType.GunRevolver` を土台にしており、
切り替えのたびに**同じリボルバーを破棄して配り直すだけの空回り**だった。

振る舞いを持たない 3 要素の組にクラスを与えるのは、「型がアイデンティティ」の
適用先を間違えている。`static readonly Step[]` の 12 行の表に戻した。

- 231 行 / 15 型 → 158 行 / 1 型 (`Field` enum と `Step` struct は private ネスト)
- `HybridWeapon` を継承しなくなったので、アイテム差し替えも共有状態も不要になった

### 4. `CustomItem.OnDropping` を追加し、10 ファイルの定型を消した

投擲操作を入力として使うアイテムが 10 個あり、**全員が同じ**
`static bool hooked` + `Hook()` + `ItemRuntime.Register` + 静的ディスパッチャを
手書きしていた。`CustomItem` は既に `OnDropped` (事後) を持っていたので、
対になる `OnDropping` (事前・キャンセル可) を足すのが素直な場所。

`PandraBreaker` / `Mindblaster` / `HybridWeapon` は購読がこれだけだったので、
`hooked` フィールドとコンストラクタごと消えた。

### 5. 配置の誤りを直した

| 対象 | 直した内容 |
|---|---|
| `ModeratorsTeam` | `Roles/Others/` にチームが紛れていた → `Teams/Factions/` |
| `AdminMissiles.cs` | キーカードでない管理者用アイテム 2 種 + 付随スキマティック 2 種が `Items/Keycards/` に同居 → `Items/Admin/` へ 2 ファイルに分割 |
| DANTE 一式 (5 ファイル) | `GameModes/Modes/` 直下に付随型が並んでモード一覧が読めなかった → `GameModes/Modes/Dante/` |
| `FirstRoles.cs` | スポーン宣言 2 個が 1 ファイル → `FirstRolesScps.cs` / `FirstRolesHumans.cs` |
| `GunXE11KMR.cs` | 武器 2 個が 1 ファイル → 分割 |
| `FemurBreaker.cs` | 中身は `FemurBreakerController` → ファイル名を型名に合わせた |

### 監査で見つかった**未結線の機能** (未着手・要判断)

整理の過程で、実装はあるのに**どこからも呼ばれていない**ものが 3 件見つかった。
死にコードではなく、ドメインを分けて再構築した際の結線漏れなので、消さずに残してある。

| 対象 | 状態 | 旧実装での呼び出し元 |
|---|---|---|
| `Effects/FloodDrowning.cs` | 完成しているが誰も付与しない。`DefaultIntensity` / `DefaultDuration` は「浸水イベント側が使う既定」と書かれている | `SpecialEvents\Events\WaterWarriorsAttack.cs` が水位で判定して付与。新 `WaterWarriorsAttack` は一律 3 ダメージの簡易版で置き換わっており、水位の概念自体が無い |
| `Maps/Features/FacilityFunctions.cs` の 2 機能 | `FacilityControlRoom.Add()` を誰も呼ばないので機能一覧が常に空 | `CustomMaps\Features\FacilityControlRoomFunctions\` + `MainHandlers\LabApiHandler.cs` |
| `Maps/Core/SpecialDoorAccess.cs` / `SurfaceGate.cs` | 規則を登録する側が無い**空の器**。扉のロック、Access Tuner Lv3 による突破、破壊防止のいずれも実装されていない | `CustomMaps\Core\DoorAccess\SpecialDoorAccessController.cs` (O4 扉の "3125" コード等) |

なお `SpecialDoorAccess` は `Dictionary<string, Rule>` の文字列キー登録所になっており、
実装するなら「型がアイデンティティ」に沿った形へ設計し直すのが筋。

---

## 中間クラスの再点検と、未実装機能の完成 [完了]

### 1. `ConsumableBase` を廃止し、`CustomUsable` へ統合した

派生 14 個を持っていたが、足していたのは 3 つだけで、しかも 2 つは
<b>`CustomUsable` 側にあるべき修正</b>だった。分けていたことで実害が出ていた:

| 症状 | 原因 |
|---|---|
| `CloakGenerator` / `GoCRecruitPaper` の `DestroyWhenDepleted => false` が効かない | `ConsumableBase.OnDepleted` が `DestroyWhenDepleted` を見ずに常に破棄していた |
| `SuspiciousTablet` (素体アドレナリン) だけ使用ストップウォッチが残る | `_useStopwatch` の後始末が `ConsumableBase` にしか無く、`CustomUsable` 直下の派生に届いていなかった |

`OnDepleted` は `DestroyWhenDepleted` → `CancelVanillaUse` の順で見るように直し、
`_useStopwatch` の処理は素体が `Consumable` かを確かめたうえで `CustomUsable` 本体へ移した。

### 2. `Items/Utility` の整理

- 電池サブシステム (7 ファイル) → `Items/Utility/Battery/`。契約 `IRechargeableBatteryTarget` も同居
- S-Nav サブシステム (6 ファイル) → `Items/Utility/Snav/`
- `IPandraBreakerTarget` を `PandraBreaker.cs` から独立ファイルへ (実装側は `Roles/Scps/Scp076`)
- `IRechargeableBatteryTarget.cs` は 7 行のインターフェースに無関係な using が 14 行付いていたので書き直し
- `ModeratorUtil` を `Items/Weapons/` → `Items/Admin/` へ

### 3. 未実装のまま残っていた機能をすべて実装した

| 機能 | 直前の状態 | 実装後 |
|---|---|---|
| **アンチミーム耐性** | `UtilityPhase2Boundary` の空メソッド 3 つ。記憶補強剤を飲んでも何も起きなかった | `Effects/AntiMemeResistance.cs`。クラス X は 60 秒・クラス Z は永続。SCP-3005 と第五教会司祭の攻撃、および「第五の音」を無効化する |
| **コーラのダメージ上昇** | 同じく空メソッド。`Effects/DamageBoost.cs` は実装済みなのに誰も呼んでいなかった | Copi/Papsi の両方が `DamageBoost` を 10 秒付与 |
| **記憶処理剤の改宗解除** | `UtilityPhase2Boundary` 経由 | `ClassBMemoryRemovePill` 本体へ移設。境界クラスごと削除 |
| **施設管理者制御室** | 機能一覧が常に空。コンソールに触る手段自体が無かった | `FacilityControlRoom` を作り直し。キーカードを持って調べる→投げて切替→もう一度調べて実行。権限・使用回数・クールダウンを判定 |
| **アンチミームプロトコル** | 生存者全員に `Poisoned` を付けるだけの簡易版 | SCP-3005 / SCP-3125 のみを対象に。第五教会は起動不可、初回のみ体力付与、CASSIE 通達 3 種を復元 |
| **爆撃要請** | 音を 1 回鳴らすだけ | 3 波 × 155 発の絨毯爆撃。開始通達・警報ループ・300 秒クールダウン |
| **特殊扉** | `Dictionary<string, Rule>` の空の器。扉は 1 枚も登録されていなかった | 扉 1 枚 = クラス 1 つ。観察室 O1〜O4 (コード 1217/1979/1236/3125)、隔離区画 (0727)、Omega Warhead 区画 (専用アイテム)。Access Tuner Lv3 による突破と破壊防止も実装 |
| **パスコード入力** | 存在しなかった (特殊扉の前提) | `Input/Passcode.cs`。`InputHandler` がキーバインドと一緒に Server Specific Settings へ配る |
| **Femur Breaker** | 23 行の空の器 | `Maps/Objects/FemurBreaker.cs`。捕縛→扉が下降→ボタンで 28 秒後に処刑。SCP-106 が居れば再収容成立として CASSIE が変わる |
| **地上ゲートの車止め** | 未参照のコントローラだけ | `Maps/Objects/SurfaceGateBarrier.cs`。カオスのウェーブで自動開放、手元のボタンでも開放 |
| **ターミナル・リフト** | 28 行の空の器 | `Maps/Objects/TerminalRift.cs`。HCZ 試験室の端末で 28.5m 降下し、7.5 秒後に戻る。降下中の落下死を無効化 |
| **洪水の溺死** | `Effects/FloodDrowning.cs` は完成していたが誰も付与しなかった。モード側は一律 3 ダメージの仮実装 | 水位を絶対高度 1 値で表現し、施設最下層から 198 秒かけて上昇。水面より下にいる非・水の戦士が `FloodDrowning` を受ける |

### 4. 併せて直したもの

- `CustomItem.OnDropping` の追加で、`ObjectPrefab.MoveTo` と同様に「同じ処理を 3 箇所で書く」状態を解消
- `AudioRuntime` の `role.GetType().Name == "AraOrun"` という<b>型名の文字列比較</b>を型判定へ
- `MapAudio` に `Loop` / `Stop` を追加 (一発再生しかできず、警報が表現できなかった)
- `ObjectPrefab.MoveTo` を追加。`Loop` は自力で止まらないため、素朴に書くと到達後も回り続ける
- 未参照のまま残っていた `SnakeMediaClip` / `MapToyInteractionRouter` / 旧 `SpecialDoorAccess` 一式を削除

### 現状

- `dotnet build AugustTest.slnx --configuration Release` — 成功 / 0 警告 / 0 エラー
- AntiMeme 489 ファイル / 37,193 行、Sliced 23 ファイル
- 未参照クラス 0 件、`TODO` / `Phase 2` の残置 0 件
- `D:\RiderWorks\Slafight_Plugin_EXILED` は無変更

### 実機検証 (ポート 7778) — 未実施

今回追加したもののうち、以下はマップ側の前提に依存するため実機確認が要る:

- 制御室コンソールのマーカー `AntiMemeButton`、Femur Breaker の `FemurBreaker_JoinPoint` /
  `FemurBreaker_CapybaraPoint`、特殊扉の `CDoor_O1`〜`O4` / `SQ_Door` / `OWJoin`
- スキマティック `FemurBreaker_Door` / `Surface_CarStopper_Bar` / `Rift` と、
  その中の `Button` インタラクタブル名
- 洪水の水位 (最下層から Y=325 まで) が実際のマップで妥当か

---

## 移行漏れの一斉補完 [完了]

「SCP-3005 のスポーン地点がおかしい」「HUD の見た目が違う」「GameMode の挙動が違う」
「`Changes/WaitingForPlayersChanges` とか他にもいろいろ」という指摘を受けて、
旧実装と全ドメインを機械的に突き合わせ、欠落を洗い出して埋めた。

### 監査で判明した欠落度 (着手前)

| 領域 | 旧 | 新 | 状態 |
|---|---:|---:|---|
| ロビー演出 | 476 | 0 | 未移植 |
| 施設ライト / 季節 / SCP-1509 / イースターエッグ / SCP権限 / SCPチーム / Abel / RPネーム | 806 | 0 | 未移植 |
| HUD | 3,136 | 1,230 | 枠組みのみ |
| ゲームモード 6 本 | 2,315 | 369 | 骨組みのみ |
| コマンド | 5,930 | 819 | 開発ツールが欠落 |

### 1. 役職の欠落

- **SCP-3005 のスポーン地点** — 旧はマーカー `Scp3005SpawnPoint`、新は
  `SpawnPoints.InRoom(RoomName.Hcz939)` という別物に化けていた。マーカー参照へ戻した
- **同種の欠落 4 件** — SCP-173 / SCP-682 / FacilityManager / SupplyManager は
  `SpawnPosition` 自体が無く、バニラ地点に出ていた
- **SCP-3005 の落ちていた仕様** — 開始 HP (`MaxHealth - 1`)、オーラの除外対象
  (第五教会・反ミームゴーグル)、反射の例外、死体を食べたときの操り人形化、
  アンチミームプロトコル連動 (鈍足/加速/毎周期ダメージ)
- **SCP 終了放送が全滅** — 旧は 14 役職が `CassieHelper.AnnounceTermination` を
  手書きしていたが、新はどこからも呼ばれず `FacilityAnnouncer.Terminate` が未使用だった。
  `ScpRole.CassieName` を 1 行宣言する形にして復元。
  購読は <b>単一ハンドラ</b> (`ScpTerminationAnnouncer`) に置いた。
  全 SCP が `OnSpawned` を `base` 呼び出しなしで override しているため、
  基底に相乗りすると書き忘れた役職だけ黙って放送されなくなる

### 2. ロビー演出 (`Maps/Features/Lobby.cs` + `Maps/Objects/LobbyRoom.cs`)

`OldMenuRoom` の看板 3 枚 (人数 / 次イベント / 残り時間)、本人にだけ聞こえる BGM の
フェードイン、開始直前の outro 差し替えと移動＋暗転、開始の足止め。

`FirstRolesHandler` に合流処理を足した。ロビーは全員を Tutorial にするが、
`SpawnSet.TargetPlayers` は未割り当て (None / Spectator) しか拾わないため、
**そのままでは開始時に誰も役職を得られない**。

### 3. HUD の中身

旧 `ScpStatusHints` (1,040 行) の実体は<b>陣営ごとの味方一覧</b>だった。
新 `BuildRoster` がその役割を担っていたので、欠けていた 3 点を足した。

- 味方までの距離
- **陣営ごとのフッター** — 旧は「チャンネル」という別レジストリに登録する作りだったが、
  何を出すかは陣営自身が知っているので `FactionTeam.RosterFooter` で名乗る形にした
  (第五教会 = 標的の位置とプロトコル状況 / 戦士 = 部隊名と核 / SCP = 発電機の状態)
- **SCP 個別のゲージ** — 079 の Tier/AUX に加えて 096 の激昂・標的数、106 の Vigor、
  049 の感知状態

部屋名の日本語訳が発狂表示と S-Nav で重複していたので `Maps/RoomNames.cs` に 1 本化した。

### 4. ゲームモード

雪とお菓子は<b>呼称・色・落ちてくる物・とどめの文言</b>しか違わなかったので、
進行を `GameModes/WarriorRaid.cs` に 1 本化し、各モードは宣言だけになった。

| モード | 復元した内容 |
|---|---|
| Snow / Candy Warriors | 4 段の進行 (宣言 → 掌握 → 作業時間 1,000 秒 → 成否判定)、消灯演出、落下物、成功時の全滅と失敗時の財団勝利放送 |
| Chaos Insurgency Raid | 同上 + 消灯時に SCP へ暗視を配る (一方的に有利にならないよう)、脱出猶予とその締め切り |
| Facility Termination | SCP の彫刻化、陣営通知、区画ごとの封鎖と除染 (LCZ → HCZ → EZ)、DELTA WARHEAD |
| SCP-1509 Battlefield | 4 つの戦場定義。境目の扉だけ閉じて区画に封じ込める |
| Water Warriors | 水位を絶対高度で表現し、水面下の非・水の戦士が `FloodDrowning` を受ける |

危険物の扱い (テスラ・除染) はモード自身が `AllowsTesla` / `AllowsDecontamination` で
名乗る形にした。`GameModeRoundHandler` にモード型を並べた switch があったのを畳んでいる。

### 5. その他の復元

| 対象 | 置き場 | 備考 |
|---|---|---|
| 施設ライト | `Changes/FacilityLight.cs` | 区画ごとの配色と核起動時の赤 |
| キャンディ | `Changes/CandyChanges.cs` | 重み付き抽選 (ピンク 25% / レア 35%) と 6 色の特殊効果 |
| SCP-1509 蘇生 | `Changes/Scp1509Resurrection.cs` | 蘇生先は `FactionTeam.Resurrection` が名乗る。旧の 9 分岐 switch は消えた |
| イースターエッグ | `Changes/EasterEggs.cs` | 核格納庫の隠し BGM |
| SCP の扉通過 / 相互安全 | `Changes/ScpDoorAccess.cs` / `ScpMutualSafety.cs` | |
| Pandra's Box 派遣 | `Spawning/PandraBoxDispatch.cs` | 核後に 1/3 で、観戦者が 2 人揃ったら |
| RP ネーム | `Input/RpName.cs` | 入力欄を `TextSetting` として一般化 (パスコードもこれに載せ替え) |
| 季節 | `Season.cs` + `Config.Season` | 運営が設定で決める値なので Config に置いた |
| 仕掛けの手動起動 | `Commands/TriggerCommand.cs` | 旧はコマンドクラスを仕掛けごとに作っていた |

### 旧実装でも死んでいたので移していないもの

- `Changes/ChristmasChanges.cs` (148 行) — `GrantingGift` の購読がコメントアウトされており、
  何も起きない状態だった
- `MainHandlers/BadgeHandler.cs` (30 行) — steamId を parse するだけで何もしない
- `Extensions/LockerExtensions.cs` (290 行) — 外部からの呼び出しが 1 件も無い
- `MainHandlers/WearsHandler.cs` (1,151 行) — 実際の呼び出しは 8 箇所で、
  すべて基本形のみ。残り 30 以上のオーバーロードは未使用。
  新 `RoleWear` (163 行) で機能的には足りている

### 現状

- `dotnet build AugustTest.slnx --configuration Release` — 成功 / 0 警告 / 0 エラー
- AntiMeme 507 ファイル / 39,301 行
- 未参照クラス 0 件、`TODO` / `Phase 2` の残置 0 件
- `D:\RiderWorks\Slafight_Plugin_EXILED` は無変更

### 残っているもの

**開発ツール系コマンド 2 本のみ。** どちらもマップ製作用で、ゲーム進行には影響しない。

| 対象 | 旧 | 内容 |
|---|---:|---|
| `Commands/DevTools/ObjectPrefabTools.cs` | 1,510 | ObjectPrefab の配置・編集ツール |
| `Commands/DevTools/HitboxCommand.cs` | 1,475 | 当たり判定の可視化ツール |

### 実機検証 (ポート 7778) — 未実施

今回の変更はマップ側の前提に強く依存するため、以下は実機確認が必要:

- ロビー: `OldMenuRoom` スキマティックと `PlayerCountText` / `NextEventText` /
  `RemainingTimeText` のブロック名、待機座標 (246.92, 198.50, -60.89)
- マーカー: `Scp3005SpawnPoint` / `Scp173SpawnPoint` / `Scp682SpawnPoint` /
  `FacilityManagerSpawnPoint` / `SupplyManagerSpawnPoint`
- スキマティック: `Xmas_Nuke` / `Candy_Nuke` / `Nuke`
- 洪水の水位 (施設最下層から Y=325) が実際のマップで妥当か
