using System;
using System.Reflection;
using AntiMeme.Items;
using HarmonyLib;
using Sliced.API.Features;

using Plugin = Exiled.API.Features.Plugin<AntiMeme.Config>;

namespace AntiMeme;

/// <summary>
/// AntiMeme (新 Slafight) の本体です。
///
/// EXILED プラグインとしてロードされますが、機能の登録は Sliced が引き受けます。
/// <see cref="Sliced.API.Features.EventHandlerBase"/> の派生クラスを書くだけで購読されるため、
/// ここに「機能を登録する呼び出し」を並べてはいけません。
/// 旧 Slafight の Plugin.cs が手書きの Register を 35 個抱えて破綻したのが、
/// この再構築の出発点です。
///
/// ここに書いてよいのは、Sliced の自動登録では表現できない 3 つだけです。
/// 1. Harmony のパッチ適用 / 解除
/// 2. 外部ツール (ffmpeg / yt-dlp) の初期化
/// 3. Server Specific Settings の配布
/// </summary>
public class AntiMemePlugin : Plugin
{
    private Harmony harmony;

    /// <summary>
    /// 実行中のインスタンスです。設定を読みたいだけなら <see cref="Settings"/> を使ってください。
    /// </summary>
    public static AntiMemePlugin Instance { get; private set; }

    /// <summary>
    /// 現在の設定です。プラグイン未読込時は既定値を返します。
    /// </summary>
    public static Config Settings => Instance is { } instance ? instance.Config : new Config();

    public override string Name => "AntiMeme";

    public override string Prefix => "AntiMeme";

    public override string Author => "org.sharp-server.jp.scpsl";

    public override Version Version => new(0, 1, 0, 0);

    public override Version RequiredExiledVersion => new(9, 14, 2);

    public override void OnEnabled()
    {
        Instance = this;

        // Sliced は ServerEvents.PluginsEnabled で AppDomain 全体を走査するので、
        // 本来これは不要 (EXILED プラグインはその時点で既にロード済み)。
        // ただしそれはローダーの実行順に依存しているため、名指しでも登録しておく。
        // 二重に検出されることはない。
        EventHandlerRegistry.RegisterAssembly(Assembly.GetExecutingAssembly());

        // 再読込で id が衝突しないよう毎回変える。
        harmony = new Harmony($"{Name}.{DateTime.UtcNow.Ticks}");
        harmony.PatchAll();

        base.OnEnabled();
    }

    public override void OnDisabled()
    {
        harmony?.UnpatchAll(harmony.Id);
        harmony = null;

        Assembly assembly = Assembly.GetExecutingAssembly();
        EventHandlerRegistry.UnregisterAssembly(assembly);
        ItemRuntime.Shutdown();
        GameMode.StopAssembly(assembly);
        CustomRole.RemoveAssembly(assembly);
        CustomItem.ReleaseAssembly(assembly);

        Instance = null;

        base.OnDisabled();
    }
}
