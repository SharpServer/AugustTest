using System;
using System.Collections.Generic;
using Sliced.API.Features;

using Logger = LabApi.Features.Console.Logger;

namespace AntiMeme.Items;

/// <summary>
/// アイテム層が持つ静的イベント購読の後始末を 1 か所に集めます。
/// 利用側プラグインだけを再読込しても、古いアセンブリの delegate を残しません。
/// </summary>
public static class ItemRuntime
{
    private static readonly List<Action> Cleanups = [];

    /// <summary>
    /// 静的イベント購読と対になる解除処理を登録します。
    /// </summary>
    internal static void Register(Action cleanup)
    {
        if (cleanup is null)
            throw new ArgumentNullException(nameof(cleanup));

        Cleanups.Add(cleanup);
    }

    /// <summary>
    /// 登録と逆順にすべての静的購読を解除します。
    /// </summary>
    public static void Shutdown()
    {
        for (int index = Cleanups.Count - 1; index >= 0; index--)
        {
            try
            {
                Cleanups[index]();
            }
            catch (Exception exception)
            {
                Logger.Error($"[AntiMeme] アイテム層の購読解除に失敗しました: {exception}");
            }
        }

        Cleanups.Clear();
    }
}

/// <summary>
/// Sliced 全体が停止する経路でも <see cref="ItemRuntime"/> を確実に片付けます。
/// </summary>
public sealed class ItemRuntimeHandler : EventHandlerBase
{
    protected override void OnDisabled() => ItemRuntime.Shutdown();
}
