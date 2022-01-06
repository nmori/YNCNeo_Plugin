using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YNC_NeoPlugin
{

    /*

        *** ゆかりねっとコネクター  Neo プラグイン開発用ひな形 ***
        version 1.1

        ① 使用するフレームワークは .Net Framework 4.8 です。
        ② 64bit デコンパイルをしてください。
        ③ 生成されたファイルは、ゆかりねっとコネクターNeoのPluginフォルダ内に
           自分でフォルダを作って、その中に生成物をいれてください。
        ④ うまく生成できていれば、ゆかりねっとコネクターNeoを起動したときに
           自動的にサーチされ、ファイルが読み込まれます。

    */

    public class PluginFunction
    {
        ConfigWindow cw = new ConfigWindow();
        private string ConfigPath = "";
        private string PluginPath = "";
        private Dictionary<string, object> ConfigData = new Dictionary<string, object>();
        
        /*
            //呼び出しアイコンの登録（押されたら、onTrigger()がよばれる予定…将来用）
            public delegate void RegistIcon(string name, byte[] bitmap);
            
            //内部設定値の設定・取得関数…将来用
            public delegate string GetSettingParam(string name);
            public delegate void SetSettingParam(string name, string value);
            
            //音声認識の代わりにテキストを送信するコールバック
            //ID - Guid.NewGuid().ToString()で生成した文字をを渡す。行毎のユニークな値。K
            //text - 母国語文字列
            //isFixed - 文章行が確定となったか否か（確定＝true)
            public delegate void SendTextdata(string ID, string text, bool isFixed);
            
            //デバッグログへ転記する
            public delegate void WriteLog(string message);
            
        */
        
        public dynamic registIcon = null;
        public dynamic getSettingParam = null;
        public dynamic setSettingParam = null;
        public dynamic sendTextdata = null;
        public dynamic writeLog = null;

        /********************************/
        /* プラグインの素性             */
        /********************************/
        /// <summary>
        /// プラグインの識別ID
        /// GUIDツールなどでぶつからない値を設定のこと
        /// </summary>
        public string Plugin_ID = "8E828504-7E55-4E97-85CC-D7E4AF5273BB";

        /// <summary>
        /// プラグインの表示名、バージョン
        /// (コネクターNeoに表示される文字列)
        /// </summary>
        public string Plugin_Name = "プラグイン v1.0";

        /// <summary>
        /// 設定ファイルなどの識別に使われるタグ
        /// プラグインを自作する場合は  「Plugin_作者識別ニックネーム_ツール識別名」をベースに命名してください
        /// </summary>
        public string Plugin_Tag = "Plugin_User_Prototype";


        /// <summary>
        /// プラグインがロードされたとき
        /// </summary>
        /// <param name="Message"></param>
        public void onLoad(Dictionary<string, object> Message)
        {
            ConfigPath = (string)Message["ConfigFolder"];
            PluginPath = (string)Message["PluginFolder"];
            ConfigData["set"] = "";

            try
            {
                //コールバックの設定
                if (Message.ContainsKey("RegistIcon")) { registIcon = Message["RegistIcon"]; }
                if (Message.ContainsKey("GetSettingParam")) { getSettingParam = Message["GetSettingParam"]; }
                if (Message.ContainsKey("SetSettingParam")) { setSettingParam = Message["SetSettingParam"]; }
                if (Message.ContainsKey("RegistIcon")) { sendTextdata = Message["SendTextdata"]; }
                if (Message.ContainsKey("WriteLog")) { writeLog = Message["WriteLog"]; }
            }
            catch (Exception ex)
            {
                if(writeLog!=null)
                {
                    writeLog($"■エラー：{ex.Message}");
                }

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

            }
            catch (Exception ex)
            {
                if(writeLog!=null)
                {
                    writeLog($"■エラー：{ex.Message}");
                }
            }
        }

        /// <summary>
        /// プラグインがアンロードされたとき
        /// </summary>
        /// <param name="Message"></param>
        public void onTerminate(Dictionary<string, object> Message)
        {
            try
            {
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
                Console.WriteLine("-------------------------------------");
                Console.WriteLine($"■エラー：{ex.Message}");
            }
            cw.Dispose();
        }

        /// <summary>
        ///  Configを押されたとき 
        /// </summary>
        public void onRequestConfigWindow()
        {
            //cw.OBSAddr.Text = ConfigData.ContainsKey("OBSAddr") ? (string)ConfigData["OBSAddr"] : "127.0.0.1";

            if (cw.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                //ConfigData["OBSAddr"] = cw.OBSAddr.Text;
            }
        }

        /// <summary>
        /// 字幕の更新通知
        /// </summary>
        /// <param name="Message">字幕関連データ</param>
        public void UpdateText(Dictionary<string, object> Message)
        {
            if ((bool)Message["TextFixed"]) /*確定判断*/
            { 
                //文章が確定されたとき

                if ((bool)Message["UpdateTranslation"])
                {
                    //Message["Text2"]～Message["Text5"]：翻訳された文章
                    //Message["Language1"]～Message["Language6"] ：言語名
                    //Message["ID"] ：行ごとにつくユニークなID
                }
                else
                {
                    //Message["Text1"] ->  母国語  （表示位置が下の場合は Message["Text6"] に格納されることがある）
                    //Message["Language1"]～Message["Language6"] ：言語名
                    //Message["ID"] ：行ごとにつくユニークなID
                }
            }
            else
            {
                //文章が未確定な状態で更新されたとき
                if ((bool)Message["UpdateTranslation"])
                {
                    //Message["Text2"]～Message["Text5"]：翻訳された文章
                    //Message["Language1"]～Message["Language6"] ：言語名
                    //Message["ID"] ：行ごとにつくユニークなID
                }
                else
                {
                    //Message["Text1"] ->  母国語  （表示位置が下の場合は Message["Text6"] に格納されることがある）
                    //Message["Language1"]～Message["Language6"] ：言語名
                    //Message["ID"] ：行ごとにつくユニークなID
                }

            }
        }

        /// <summary>
        /// トリガーを押されたとき（将来実装予定）
        /// </summary>
        /// <param name="Message">パラメータ</param>
        public void onTrigger(Dictionary<string, object> Message)
        {
            /* reserved : 将来実装予定 */
        }

        /// <summary>
        /// プラグインが有効になったとき
        /// </summary>
        /// <param name="Message">パラメータ</param>
        public void onEnabled(Dictionary<string, object> Message)
        {
            /*  対応：v1.60～ */
        }

        /// <summary>
        /// プラグインが無効になったとき
        /// </summary>
        /// <param name="Message">パラメータ</param>
        public void onDisabled(Dictionary<string, object> Message)
        {
            /*  対応：v1.60～ */

        }

        /// <summary>
        /// 音声認識後の補正
        /// </summary>
        /// <param name="Message">認識された文字列</param>
        /// <returns>補正後の文字列</returns>
        public string PostRecognition(Dictionary<string, object> Message)
        {
            string Text = (string)Message["Text"];

            /* ここに処理を差し込む -start */


            /* ここに処理を差し込む -end */

            return Text;
        }

        /// <summary>
        /// 翻訳前の補正  
        /// </summary>
        /// <param name="Message">文字データ</param>
        /// <returns></returns>
        public Dictionary<string, object> PreTranslation(Dictionary<string, object> Message)
        {
            Dictionary<string, object> returnData = new Dictionary<string, object>();
            String Text1 = (string)Message["Text1"];
            String Text2 = (string)Message["Text2"];
            String Text3 = (string)Message["Text3"];
            String Text4 = (string)Message["Text4"];
            String Native = (string)Message["LanguageNative"];
            String Lang1 = (string)Message["Language1"];
            String Lang2 = (string)Message["Language2"];
            String Lang3 = (string)Message["Language3"];
            String Lang4 = (string)Message["Language4"];

            /* ここに処理を差し込む -start */


            /* ここに処理を差し込む -end */

            returnData["Text1"] = Text1;
            returnData["Text2"] = Text2;
            returnData["Text3"] = Text3;
            returnData["Text4"] = Text4;
            return returnData;
        }

        /// <summary>
        ///  翻訳後の補正
        /// </summary>
        /// <param name="Message">文字データ</param>
        /// <returns></returns>
        public Dictionary<string, object> PostTranslation(Dictionary<string, object> Message)
        {
            Dictionary<string, object> returnData = new Dictionary<string, object>();
            String Text1 = (string)Message["Text1"];
            String Text2 = (string)Message["Text2"];
            String Text3 = (string)Message["Text3"];
            String Text4 = (string)Message["Text4"];
            String Native = (string)Message["LanguageNative"];
            String Lang1 = (string)Message["Language1"];
            String Lang2 = (string)Message["Language2"];
            String Lang3 = (string)Message["Language3"];
            String Lang4 = (string)Message["Language4"];
            
            /* ここに処理を差し込む -start */


            /* ここに処理を差し込む -end */

            returnData["Text1"] = Text1;
            returnData["Text2"] = Text2;
            returnData["Text3"] = Text3;
            returnData["Text4"] = Text4;
            return returnData;
        }

    }
}
