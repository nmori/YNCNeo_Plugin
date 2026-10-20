using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace YNC_NeoPlugin
{
    /*
        ホスト(ゆかりねっとコネクターNEO)との連携部分
        通常はこのファイルを編集する必要はありません。処理は UserProgram.cs に書いてください。

        ★注意★
        ホストは public メソッドの「名前の完全一致」でフックの有無を判定します。
        PostRecognition / PreTranslation / PostTranslation / UpdateText / CaptionAfterView /
        beforeSendInjection / onCommand / onRequestConfigWindow / onEnabled / onDisabled
        と同じ名前の public メソッドを、補助目的で作らないでください。
        補助のメソッドは private にするのが安全です。
    */
    public partial class PluginFunction
    {
        /****************************************************************
           SDKバージョン名 (ホストは参照しません。雛形の世代を示すだけです)
        *****************************************************************/
        public const string Plugin_SDKVersion = "3";

        /****************************************************************
           内部情報
        *****************************************************************/
        private string ConfigPath = "";
        private string PluginPath = "";
        private string LogPath = "";
        private string HostVersion = "";
        private bool HostDebugMode = false;
        private System.Windows.Threading.Dispatcher HostDispatcher = null;
        private Dictionary<string, object> ConfigData = new Dictionary<string, object>();

        /****************************************************************
           公開インタフェイス (ホストからのコールバック)

           ・ホストは Action / Func 型で詰めて渡してきます (dynamic ではありません)。
           ・型が 1 つでもずれると null になります。型は変更しないでください。
           ・古い本体には存在しないものがあります。呼び出し前に必ず null チェックしてください。
        *****************************************************************/

        // --- 基本 ---
        public Action<string, byte[]> registIcon = null;                          // (name, bitmap)  ※現行本体では何もしません
        public Func<string, string> getSettingParam = null;                       // (name) → value
        public Action<string, string, string> setSettingParam = null;             // (name, value, sender=Plugin_Tag)
        public Action<string> writeLog = null;                                    // (message)

        // --- 字幕の送信 ---
        public Action<string, string, bool> sendTextdata = null;                  // (ID, text, isFixed)
        public Action<string, string, bool, string, string, Dictionary<string, object>> sendTextdataV2 = null;
                                                                                  // (ID, text, isFixed, userId, userName, optionData)

        // --- プラグイン間通信 ---
        public Func<string, string, Dictionary<string, object>, string> sendCommand = null;
                                                                                  // (senderID, targetPlugin_Tag, args) → 相手の onCommand の戻り値

        // --- 翻訳・言語判定 (同期版はフック内で使わないこと) ---
        public Func<string, string, Dictionary<string, object>> getTranslate = null;                  // (lang, text) → ["text"]
        public Func<string, string, Task<Dictionary<string, object>>> getTranslateAsync = null;       // (lang, text)
        public Func<string, Dictionary<string, object>> getLanguage = null;                           // (text)
        public Func<string, Task<Dictionary<string, object>>> getLanguageAsync = null;                // (text)

        // --- 読み上げ ---
        public Func<string, string> sendSpeech = null;                            // (text)
        public Func<string, Dictionary<string, object>, string> sendSpeechV2 = null; // (text, param)
        public Func<string> stopSpeech = null;
        public Action<string> finishCaptionForSpeech = null;                      // (messageID) 読み上げ完了の通知

        // --- AI ---
        public Func<string, string, string> callGPTFunction = null;               // (method, query)
        public Func<string, string> getSentiment = null;                          // (text)
        public Func<string, string, string, int, Task<string>> callJevAsync = null; // (stateJson, questionsJson, model, timeoutMs) → JSON

        // --- 内蔵Webサーバ / レイアウト ---
        public Action<string, string> setWebPathMount = null;                     // (urlPath, filePath)  filePath="" で解除
        public Action<string, string> setAddString = null;                        // (filePath, data)
        public Action<string, string, string, string, string, string, string, bool> addLayoutItem = null;
                                                                                  // (id, title, category, description, iconFile, filePath, urlPath, directPath)
        public Action<string> removeLayoutItem = null;                            // (id)
        public Action reloadLayoutList = null;

        // --- 内部設定 (リスト) ---
        public Func<string, string, string> getSettingParamList = null;           // (name, key)
        public Action<string, string> removeSettingParamList = null;              // (name, key)
        public Func<string, string, bool> getContainsParamList = null;            // (name, key)
        public Action<string, string, string, string> setSettingParamList = null; // (name, key, value, sender=Plugin_Tag)

        /********************************/
        /* プラグインがロードされたとき */
        /********************************/
        // ホストの UI スレッドで同期実行されます。タイムアウトはありません。
        // ここで通信待ちなどをするとアプリ全体が固まるので、重い処理は Task.Run で逃がしてください。
        public void onLoad(Dictionary<string, object> Message)
        {
            ConfigPath = GetValue(Message, "ConfigFolder", "");
            PluginPath = GetValue(Message, "PluginFolder", "");
            LogPath = GetValue(Message, "LogFolder", "");
            HostVersion = GetValue(Message, "Version", "");
            HostDebugMode = GetValue(Message, "DebugMode", false);
            // ホスト側のキー名自体が "Dispacher" (綴りはこのまま)
            HostDispatcher = GetValue<System.Windows.Threading.Dispatcher>(Message, "Dispacher", null);

            registIcon = GetValue<Action<string, byte[]>>(Message, "RegistIcon", null);
            getSettingParam = GetValue<Func<string, string>>(Message, "GetSettingParam", null);
            setSettingParam = GetValue<Action<string, string, string>>(Message, "SetSettingParam", null);
            writeLog = GetValue<Action<string>>(Message, "WriteLog", null);

            sendTextdata = GetValue<Action<string, string, bool>>(Message, "SendTextdata", null);
            sendTextdataV2 = GetValue<Action<string, string, bool, string, string, Dictionary<string, object>>>(Message, "SendTextdataV2", null);
            sendCommand = GetValue<Func<string, string, Dictionary<string, object>, string>>(Message, "SendCommand", null);

            getTranslate = GetValue<Func<string, string, Dictionary<string, object>>>(Message, "GetTranslate", null);
            getTranslateAsync = GetValue<Func<string, string, Task<Dictionary<string, object>>>>(Message, "GetTranslateAsync", null);
            getLanguage = GetValue<Func<string, Dictionary<string, object>>>(Message, "GetLanguage", null);
            getLanguageAsync = GetValue<Func<string, Task<Dictionary<string, object>>>>(Message, "GetLanguageAsync", null);

            sendSpeech = GetValue<Func<string, string>>(Message, "SendSpeech", null);
            sendSpeechV2 = GetValue<Func<string, Dictionary<string, object>, string>>(Message, "SendSpeechV2", null);
            stopSpeech = GetValue<Func<string>>(Message, "StopSpeech", null);
            finishCaptionForSpeech = GetValue<Action<string>>(Message, "FinishCaptionForSpeech", null);

            callGPTFunction = GetValue<Func<string, string, string>>(Message, "CallGPTFunction", null);
            getSentiment = GetValue<Func<string, string>>(Message, "GetSentiment", null);
            callJevAsync = GetValue<Func<string, string, string, int, Task<string>>>(Message, "CallJevAsync", null);

            setWebPathMount = GetValue<Action<string, string>>(Message, "SetWebPathMount", null);
            setAddString = GetValue<Action<string, string>>(Message, "SetAddString", null);
            addLayoutItem = GetValue<Action<string, string, string, string, string, string, string, bool>>(Message, "AddLayoutItem", null);
            removeLayoutItem = GetValue<Action<string>>(Message, "RemoveLayoutItem", null);
            reloadLayoutList = GetValue<Action>(Message, "ReloadLayoutList", null);

            getSettingParamList = GetValue<Func<string, string, string>>(Message, "GetSettingParamList", null);
            removeSettingParamList = GetValue<Action<string, string>>(Message, "RemoveSettingParamList", null);
            getContainsParamList = GetValue<Func<string, string, bool>>(Message, "GetContainsParamList", null);
            setSettingParamList = GetValue<Action<string, string, string, string>>(Message, "SetSettingParamList", null);

            try
            {
                string PresetFile = Path.Combine(ConfigPath, $"{Plugin_Tag}.config");
                if (File.Exists(PresetFile))
                {
                    using (StreamReader sr = new StreamReader(PresetFile, Encoding.UTF8))
                    {
                        ConfigData = JsonConvert.DeserializeObject<Dictionary<string, object>>(sr.ReadToEnd());
                    }
                }
                if (ConfigData == null) { ConfigData = new Dictionary<string, object>(); }

                loadSetting();
            }
            catch (Exception ex)
            {
                Log($"Error>{ex.Message + ex.StackTrace}");
            }
        }

        /************************************/
        /* プラグインがアンロードされたとき */
        /************************************/
        // アプリ終了時は UI スレッドとは別のスレッドから、3 秒のタイムアウト付きで呼ばれます。
        // 「設定の読み込み」ボタンのときは UI スレッドから呼ばれます。
        // 3 秒で打ち切られても困らないよう、保存を一番先に済ませます。
        public void onTerminate(Dictionary<string, object> Message)
        {
            SaveConfig();
            DisposeWindowSafely(cw);
        }

        /************************************/
        /* 設定を保存                       */
        /************************************/
        // UI スレッド以外から呼ばれることがあります。この中で画面 (cw) の値を読まないでください。
        // 画面の値は OK を押したときに ConfigData へ移しておきます。
        public void SaveConfig()
        {
            try
            {
                saveSetting();

                var configSerialData = JsonConvert.SerializeObject(ConfigData);

                string PresetFile = Path.Combine(ConfigPath, $"{Plugin_Tag}.config");
                using (StreamWriter sw = new StreamWriter(PresetFile, false, Encoding.UTF8))
                {
                    sw.Write(configSerialData);
                }
            }
            catch (Exception ex)
            {
                Log($"Error>{ex.Message + ex.StackTrace}");
            }
        }

        /******************************************************************/
        /* 読み上げAPI (sendSpeech) でこのプラグインを有効にしたい場合      */
        /******************************************************************/
        public void enabledFunctionOnSpeech()
        {
            setSettingParamList?.Invoke("Common.sendSpeech.PluginEnabled", Plugin_Tag, Plugin_Tag, Plugin_Tag);
        }

        /******************************************************************/
        /* 読み上げAPI (sendSpeech) でこのプラグインを無効にしたい場合      */
        /******************************************************************/
        public void disabledFunctionOnSpeech()
        {
            removeSettingParamList?.Invoke("Common.sendSpeech.PluginEnabled", Plugin_Tag);
        }

        /******************************************************************/
        /* 翻訳サーバでこのプラグインを有効にしたい場合                    */
        /******************************************************************/
        public void enabledFunctionOnTranslateServer()
        {
            setSettingParamList?.Invoke("Common.Translate.PluginEnabled", Plugin_Tag, Plugin_Tag, Plugin_Tag);
        }

        /******************************************************************/
        /* 翻訳サーバでこのプラグインを無効にしたい場合                    */
        /******************************************************************/
        public void disabledFunctionOnTranslateServer()
        {
            removeSettingParamList?.Invoke("Common.Translate.PluginEnabled", Plugin_Tag);
        }

        /******************************************************************/
        /* プラグイン一覧に状態を表示する                                  */
        /******************************************************************/
        // statusColor: Connected / Disconnected / Idle / Inactive / Normal / Run / Warning / Error / Fail
        // statusText : エラー・注意・未接続・休止中のときに一覧の 2 行目に出る文
        // 実際に状態が変わったとき (接続・切断・失敗・回復) だけ呼んでください。
        private void SetStatus(string statusColor, string statusText = null)
        {
            if (setSettingParam == null) return;
            if (statusText != null)
            {
                setSettingParam("Status", statusText, Plugin_Tag);   // 文を先に送る
            }
            setSettingParam("StatusColor", statusColor, Plugin_Tag);
        }

        /******************************************************************/
        /* ログ出力 (writeLog が無い本体でも落ちないように)                */
        /******************************************************************/
        private void Log(string message)
        {
            try { writeLog?.Invoke($"{Plugin_Tag}: {message}"); } catch { }
        }

        /******************************************************************/
        /* UI スレッドで実行する (待たない)                                */
        /******************************************************************/
        // フック (PostRecognition など) は UI スレッドで呼ばれません。
        // 画面を触るときはこれで UI スレッドへ渡します。
        private void RunOnUIThread(Action action)
        {
            if (action == null) return;
            if (HostDispatcher != null && !HostDispatcher.CheckAccess())
            {
                HostDispatcher.BeginInvoke(action);
            }
            else
            {
                action();
            }
        }

        /******************************************************************/
        /* 例外でホストを巻き込まないバックグラウンド実行                  */
        /******************************************************************/
        // 生の Task.Run や async void は、例外を取りこぼすとアプリごと落ちることがあります。
        //   RunBackground(() => { ... });                                  同期の処理
        //   RunBackground(async () => { await getTranslateAsync(...); });   非同期の処理 (下の Func<Task> 版が使われる)
        private void RunBackground(Action action)
        {
            if (action == null) return;
            Task.Run(() =>
            {
                try { action(); }
                catch (Exception ex) { Log($"Error>{ex.Message + ex.StackTrace}"); }
            });
        }

        // async ラムダ用。Action 版に async ラムダを渡すと async void になり、
        // await の先で起きた例外を捕まえられずアプリごと落ちるため、こちらで待って捕まえる。
        private void RunBackground(Func<Task> action)
        {
            if (action == null) return;
            Task.Run(async () =>
            {
                try { await action().ConfigureAwait(false); }
                catch (Exception ex) { Log($"Error>{ex.Message + ex.StackTrace}"); }
            });
        }

        /******************************************************************/
        /* Message から安全に値を取り出す                                  */
        /******************************************************************/
        // フックに渡される Message は、呼ばれる経路によって入っていないキーがあります。
        // (string)Message["xxx"] のように直接キャストすると例外になることがあるので、これを使ってください。
        private static T GetValue<T>(Dictionary<string, object> message, string key, T defaultValue)
        {
            if (message != null && message.TryGetValue(key, out object value) && value != null)
            {
                if (value is T typed) return typed;
                try
                {
                    // 数値の型違い (int / long / double) や文字列で来た bool などを吸収する
                    return (T)Convert.ChangeType(value, typeof(T), System.Globalization.CultureInfo.InvariantCulture);
                }
                catch
                {
                }
            }
            return defaultValue;
        }

        /******************************************************************/
        /* WinForms の窓を作成元スレッドで片付ける                          */
        /******************************************************************/
        // .NET 10 の WinForms は、作ったスレッド以外からの Dispose を例外で弾きます。
        // onTerminate は別スレッドから呼ばれ、そのとき UI スレッドはこちらを待っているので、
        // Invoke (待つ) ではなく BeginInvoke (待たない) で渡します。
        private static void DisposeWindowSafely(System.Windows.Forms.Form form)
            => OnWindowThread(form, () => form.Dispose());

        // Close は FormClosing を発火させます。後始末をそこでしている場合は Dispose に置き換えないこと。
        private static void CloseWindowSafely(System.Windows.Forms.Form form)
            => OnWindowThread(form, () => form.Close());

        private static void OnWindowThread(System.Windows.Forms.Form form, Action action)
        {
            if (form == null) return;
            try
            {
                if (form.IsHandleCreated && form.InvokeRequired)
                {
                    form.BeginInvoke(action);
                }
                else
                {
                    action();
                }
            }
            catch
            {
                // 終了処理なので失敗しても飲み込む
            }
        }
    }
}
