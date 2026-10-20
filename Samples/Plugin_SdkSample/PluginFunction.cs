using System;
using System.Collections.Generic;
using YNC_PluginSdk;

namespace Plugin_SdkSample
{
    /*
        YNC_PluginSdk を使った最小プラグインの例

        ・ホストは FullName が ".PluginFunction" で終わる型を探すため、クラス名は必ず PluginFunction にする
        ・PostRecognition などのフックは PluginBase にありません。override ではなく、
          通常の public メソッドとして同じ名前で書きます (使わないフックは書かない)
    */
    public class PluginFunction : PluginBase
    {
        private ConfigStore<Dictionary<string, object>> _configStore;
        private Dictionary<string, object> _config = new Dictionary<string, object>();

        public PluginFunction() : base(
            pluginId: "3F0C5E1A-7B2D-4C8E-9A61-2D4B7E9F1C35",   // ★必ず自分で生成した GUID に置き換える
            pluginTag: "Plugin_SdkSample",                      // ★DLL 名 (AssemblyName) と揃える
            pluginName: "SDKサンプル v1.0",
            pluginVersion: "v1.0")
        {
        }

        // onLoad の後に呼ばれる。UI スレッドで同期実行されるので、ここで通信待ちなどをしないこと
        protected override void OnLoaded()
        {
            _configStore = new ConfigStore<Dictionary<string, object>>(Host.ConfigPath, Plugin_Tag);
            _config = _configStore.Load(ex => Log($"設定の読み込みに失敗しました {ex.Message}"));
        }

        // アプリ終了時は別スレッドから 3 秒で打ち切られることがある。保存を先に済ませる
        public override void onTerminate(Dictionary<string, object> message)
        {
            _configStore?.Save(_config, ex => Log($"設定の保存に失敗しました {ex.Message}"));
        }

        // 音声認識後の補正 (発話のたびに呼ばれる)
        public string PostRecognition(Dictionary<string, object> message)
        {
            // キーは経路によって欠けていることがあるので TryGetValue で読む
            string text = message.TryGetValue("Text", out object t) ? t as string ?? "" : "";
            bool isFixed = message.TryGetValue("TextFixed", out object f) && f is bool b && b;

            if (isFixed && text.Length > 0)
            {
                // 時間のかかる処理は裏へ逃がし、フック自体はすぐに返す
                SafeTask.Run(() =>
                {
                    Log($"確定文: {text}");
                }, ex => Log($"Error> {ex.Message}"));
            }

            return text;
        }

        // HTTP の /api/command?target=Plugin_SdkSample&command=ping や、他プラグインの SendCommand から呼ばれる
        public string onCommand(Dictionary<string, object> message)
        {
            string command = message.TryGetValue("command", out object c) ? c as string ?? "" : "";
            return command == "ping" ? "{\"result\":\"pong\"}" : "";
        }

        private void Log(string text)
        {
            try { Host?.WriteLog?.Invoke($"{Plugin_Tag}: {text}"); } catch { }
        }
    }
}
