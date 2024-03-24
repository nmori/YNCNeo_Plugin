using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace YNC_NeoPlugin
{
    public partial class PluginFunction
    {
        /****************************************************************
           SDKバージョン名
        *****************************************************************/
        public string Plugin_SDKVersion = "2";

        /****************************************************************
           内部情報
        *****************************************************************/
        private string ConfigPath = "";
        private string PluginPath = "";
        private Dictionary<string, object> ConfigData = new Dictionary<string, object>();

        /****************************************************************
           公開インタフェイス
        *****************************************************************/
        public dynamic registIcon = null;
        public dynamic getSettingParam = null;
        public dynamic setSettingParam = null;
        public dynamic sendTextdata = null;
        public dynamic writeLog = null;
        public dynamic getTranslate = null;
        public dynamic getSettingParamList = null;
        public dynamic removeSettingParamList = null;
        public dynamic getContainsParamList = null;
        public dynamic setSettingParamList = null;

        /********************************/
        /* プラグインがロードされたとき */
        /********************************/
        public void onLoad(Dictionary<string, object> Message)
        {
            ConfigPath = (string)Message["ConfigFolder"];
            PluginPath = (string)Message["PluginFolder"];
            ConfigData["set"] = "";

            try
            {
                if (Message.ContainsKey("RegistIcon")) { registIcon = Message["RegistIcon"]; }
                if (Message.ContainsKey("GetSettingParam")) { getSettingParam = Message["GetSettingParam"]; }
                if (Message.ContainsKey("SetSettingParam")) { setSettingParam = Message["SetSettingParam"]; }
                if (Message.ContainsKey("SendTextdata")) { sendTextdata = Message["SendTextdata"]; }
                if (Message.ContainsKey("WriteLog")) { writeLog = Message["WriteLog"]; }

                if (Message.ContainsKey("GetTranslate")) { getTranslate = Message["GetTranslate"]; }

                if (Message.ContainsKey("GetSettingParamList")) { getSettingParamList = Message["GetSettingParamList"]; }
                if (Message.ContainsKey("RemoveSettingParamList")) { removeSettingParamList = Message["RemoveSettingParamList"]; }
                if (Message.ContainsKey("GetContainsParamList")) { getContainsParamList = Message["GetContainsParamList"]; }
                if (Message.ContainsKey("SetSettingParamList")) { setSettingParamList = Message["SetSettingParamList"]; }
            }
            catch (Exception ex)
            {

            }

            try
            {
                string PresetFile = System.IO.Path.Combine(ConfigPath, $"{Plugin_Tag}.config");
                if (File.Exists(PresetFile))
                {
                    using (StreamReader sr = new StreamReader(PresetFile, Encoding.UTF8))
                    {
                        ConfigData = JsonConvert.DeserializeObject<Dictionary<string, object>>(sr.ReadToEnd());
                    }
                }

                loadSetting();
            }
            catch (Exception ex)
            {
                writeLog($"{Plugin_Tag}: Error>{ex.Message + ex.StackTrace}");

            }
        }

        /************************************/
        /* プラグインがアンロードされたとき */
        /************************************/
        public void onTerminate(Dictionary<string, object> Message)
        {
            SaveConfig();
            cw.Dispose();
        }

        /************************************/
        /* 設定を保存                       */
        /************************************/
        public void SaveConfig()
        {
            try
            {
                saveSetting();

                var configSerialData = Newtonsoft.Json.JsonConvert.SerializeObject(ConfigData);

                string PresetFile = System.IO.Path.Combine(ConfigPath, $"{Plugin_Tag}.config");
                using (StreamWriter sw = new StreamWriter(PresetFile, false, Encoding.UTF8))
                {
                    sw.Write(configSerialData);
                    sw.Close();
                }
            }
            catch (Exception ex)
            {
                writeLog($"{Plugin_Tag}: Error>{ex.Message + ex.StackTrace}");

            }
        }

        /******************************************************************/
        /*読み上げAPIでこのプラグインを有効にしたい場合                   */
        /*****************************************************************/
        public void enabledFunctionOnSpeech()
        {
            if (setSettingParamList != null)
            {
                setSettingParamList("Common.sendSpeechV2.PluginEnabled", Plugin_Tag, Plugin_Tag, Plugin_Tag);
            }
        }

        /******************************************************************/
        /*読み上げAPIでこのプラグインを無効にしたい場合                   */
        /*****************************************************************/
        public void disabledFunctionOnSpeech()
        {
            if (removeSettingParamList != null)
            {
                removeSettingParamList("Common.sendSpeechV2.PluginEnabled", Plugin_Tag);
            }
        }

        /******************************************************************/
        /*翻訳サーバでこのプラグインを有効にしたい場合                   */
        /*****************************************************************/
        public void enabledFunctionOnTranslateServer()
        {
            if (setSettingParamList != null)
            {
                setSettingParamList("Common.Translate.PluginEnabled", Plugin_Tag, Plugin_Tag, Plugin_Tag);
            }
        }

        /******************************************************************/
        /*翻訳サーバでこのプラグインを無効にしたい場合                   */
        /*****************************************************************/
        public void disabledFunctionOnTranslateServer()
        {
            if (removeSettingParamList != null)
            {
                removeSettingParamList("Common.Translate.PluginEnabled", Plugin_Tag);
            }
        }
    }
}
