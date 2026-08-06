using System;
using System.Collections.Generic;
using System.Linq;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using Sliced.API.Enums;
using Sliced.API.Features;
using UserSettings.ServerSpecific;

using Logger = LabApi.Features.Console.Logger;

namespace AntiMeme.Input;

/// <summary>
/// 宣言されている <see cref="InputBinding"/> をすべて集め、
/// Server Specific Settings としてクライアントへ配り、押下を配送します。
///
/// <para>
/// 設定 ID は「見つけた順の連番」で、コード上のどこにも書きません。
/// クライアントは ID しか送ってこないので、こちら側で ID → バインディングの対応を
/// 1 本持っていれば足ります。
/// </para>
/// </summary>
public sealed class InputHandler : EventHandlerBase
{
    private static readonly Dictionary<int, InputBinding> Bindings = new Dictionary<int, InputBinding>();

    private static readonly Dictionary<int, TextSetting> TextSettings = new Dictionary<int, TextSetting>();

    public override HandlerLifetime Lifetime => HandlerLifetime.Manual;

    public override void RegisterEvents()
    {
        Publish();

        ServerSpecificSettingsSync.ServerOnSettingValueReceived += OnSettingReceived;
        PlayerEvents.Left += OnLeft;
        ServerEvents.RoundRestarted += ClearTextSettings;
    }

    public override void UnregisterEvents()
    {
        ServerSpecificSettingsSync.ServerOnSettingValueReceived -= OnSettingReceived;
        PlayerEvents.Left -= OnLeft;
        ServerEvents.RoundRestarted -= ClearTextSettings;

        ClearTextSettings();
        TextSettings.Clear();
        Bindings.Clear();
        ServerSpecificSettingsSync.DefinedSettings = [];
        ServerSpecificSettingsSync.SendToAll();
    }

    /// <summary>
    /// 宣言されているキーを集めて配ります。
    /// </summary>
    private static void Publish()
    {
        Bindings.Clear();

        List<InputBinding> bindings = Discover<InputBinding>();

        if (bindings.Count == 0 && TypeParser.FindTypes<TextSetting>().Count == 0) return;

        List<ServerSpecificSettingBase> settings = [];
        int nextId = 0;

        // 同じ Group のものをまとめ、見出しを 1 つ置く。
        foreach (IGrouping<string, InputBinding> group in bindings
                     .OrderBy(binding => binding.Order)
                     .GroupBy(binding => binding.Group))
        {
            settings.Add(new SSGroupHeader(nextId++, group.Key));

            foreach (InputBinding binding in group)
            {
                int id = nextId++;

                settings.Add(new SSKeybindSetting(
                    id,
                    binding.Label,
                    binding.DefaultKey,
                    preventInteractionOnGui: true,
                    allowSpectatorTrigger: binding.AllowWhileSpectating,
                    hint: binding.Hint));

                Bindings[id] = binding;
            }
        }

        // 打ち込む欄。キーではないので見出しを分ける。
        List<TextSetting> texts = Discover<TextSetting>().OrderBy(setting => setting.Order).ToList();

        if (texts.Count > 0)
        {
            settings.Add(new SSGroupHeader(nextId++, "メモ"));

            foreach (TextSetting text in texts)
            {
                int id = nextId++;

                settings.Add(new SSPlaintextSetting(
                    id,
                    text.Label,
                    placeholder: text.Placeholder,
                    characterLimit: text.MaxLength,
                    hint: text.Hint));

                TextSettings[id] = text;
            }
        }

        ServerSpecificSettingsSync.DefinedSettings = settings.ToArray();
        ServerSpecificSettingsSync.SendToAll();

        Logger.Debug($"[AntiMeme] 入力を {Bindings.Count} 件配布しました。");
    }

    /// <summary>宣言されている入力を生成します。</summary>
    private static List<T> Discover<T>() where T : class
    {
        List<T> found = [];

        foreach (Type type in TypeParser.FindTypes<T>())
        {
            try
            {
                found.Add((T)Activator.CreateInstance(type));
            }
            catch (Exception exception)
            {
                Logger.Error($"[AntiMeme] 入力 {type.FullName} を生成できませんでした: {exception}");
            }
        }

        return found;
    }

    /// <summary>宣言済みの入力欄を型で引きます。</summary>
    public static T Setting<T>() where T : TextSetting =>
        TextSettings.Values.OfType<T>().FirstOrDefault();

    /// <summary>
    /// このキーに割り当てられた Server Specific Settings の ID です。まだ配っていなければ -1。
    /// HUD に実際の割り当てキーを描く (<c>SSKeybindHintParameter</c>) ために要ります。
    /// </summary>
    public static int SettingIdOf<T>() where T : InputBinding
    {
        foreach (KeyValuePair<int, InputBinding> entry in Bindings)
        {
            if (entry.Value is T) return entry.Key;
        }

        return -1;
    }

    private static void ClearTextSettings()
    {
        foreach (TextSetting setting in TextSettings.Values)
            setting.Clear();
    }

    private static void OnLeft(PlayerLeftEventArgs ev)
    {
        foreach (TextSetting setting in TextSettings.Values)
            setting.Forget(ev.Player);
    }

    private static void OnSettingReceived(ReferenceHub hub, ServerSpecificSettingBase setting)
    {
        if (Player.Get(hub) is not { IsDestroyed: false } sender) return;

        if (setting is SSPlaintextSetting plaintext)
        {
            if (TextSettings.TryGetValue(plaintext.SettingId, out TextSetting text))
                text.Set(sender, plaintext.SyncInputText);

            return;
        }

        // 押した瞬間だけ拾う。離したときも同じイベントで来る。
        if (setting is not SSKeybindSetting { SyncIsPressed: true } keybind) return;
        if (!Bindings.TryGetValue(keybind.SettingId, out InputBinding binding)) return;

        try
        {
            binding.OnPressed(sender);
        }
        catch (Exception exception)
        {
            Logger.Error($"[AntiMeme] 入力 {binding.GetType().Name} の処理で例外が発生しました: {exception}");
        }
    }
}
