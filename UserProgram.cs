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
    /*
        ユーザ向け雛形
    */

    public partial class PluginFunction
    {

        /****************************************************************
         プラグインの素性             
         必ずGUIツールなどでぶつからない値を設定のこと
        *****************************************************************/
        public const string Plugin_ID = "8E828504-7E55-4E97-85CC-D7E4AF5273BB";

        /****************************************************************
           プラグインのユニークな名前
            * DLLのファイル名部分を設定するのがデフォルトです
            * ファイル名とユニーク名が異なる場合はロードに時間がかかることがあります
        *****************************************************************/
        public const string Plugin_Tag = "Plugin_Prototype";


        /****************************************************************
           バージョン名
        *****************************************************************/
        public const string Plugin_Version = "v1.0";

        /****************************************************************
           表示名（言語を指定するとユーザ言語毎に表示が差し変わります)
        *****************************************************************/
        public string Plugin_Name
        {
            get
            {
                switch (Thread.CurrentThread.CurrentUICulture.Name.Substring(0, 2).ToLower())
                {
                    case "es":
                        return $"Prototype {Plugin_Version}";

                    case "ru":
                        return $"Prototype {Plugin_Version}";

                    case "zh":
                        return $"Prototype {Plugin_Version}";

                    case "ko":
                        return $"Prototype { Plugin_Version}";

                    case "en":
                        return $"Prototype {Plugin_Version}";

                    case "ja":
                    default:
                        return $"プロトタイププラグイン {Plugin_Version}";
                }
            }
        }
        /****************************************************************
           作者名
        *****************************************************************/
        public const string Plugin_AuthorName = "";

        /****************************************************************
           配布URL（https://～)
        *****************************************************************/
        public const string Plugin_ProductURL = "";

        /****************************************************************
           設定画面
        *****************************************************************/
        ConfigWindow cw = new ConfigWindow();

        /****************************************************************
          設定の読み込み
        *****************************************************************/
        private void loadSetting()
        {
            //設定自体は Dictionary<string, object> ConfigData に格納されています    
        }

        /****************************************************************
          設定の保存
        *****************************************************************/
        private void saveSetting()
        {
            //永続化したい設定は Dictionary<string, object> ConfigData にいれてください
        }

        /****************************************************************
         Configを押されたとき             
        *****************************************************************/
        public void onRequestConfigWindow()
        {
            if (cw.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                //設定を保存するならこんな感じで…
                //ConfigData["switch01"] = ～;
                SaveConfig();
            }
        }

       

        /****************************************************************
         音声認識後の補正                 
        *****************************************************************/
        public string PostRecognition(Dictionary<string, object> Message)
        {
            string ID = (string)Message["ID"];
            string Text = (string)Message["Text"];

            bool fixedText = (bool)Message["TextFixed"];
            bool updateTranslation = (bool)Message["UpdateTranslation"];
            bool isDeleted = (bool)Message["isDeleted"];
            /*
              ID                : ユニークな文章ID
              Text              : 音声認識された母国語文章
              fixedText         : 文章が確定したとき
              UpdateTranslation : 翻訳が更新されたとき
              isDeleted         : この文が削除されたとき
            */

            /* ここに処理を差し込む -start */


            /* ここに処理を差し込む -end */

            return Text;
        }

        /****************************************************************
        翻訳前の補正　　                 
        *****************************************************************/
        public Dictionary<string, object> PreTranslation(Dictionary<string, object> Message)
        {
            Dictionary<string, object> returnData = new Dictionary<string, object>();
            string ID = (string)Message["ID"];
            String Text1 = (string)Message["Text1"];
            String Text2 = (string)Message["Text2"];
            String Text3 = (string)Message["Text3"];
            String Text4 = (string)Message["Text4"];
            String Native = (string)Message["LanguageNative"];
            String Lang1 = (string)Message["Language1"];
            String Lang2 = (string)Message["Language2"];
            String Lang3 = (string)Message["Language3"];
            String Lang4 = (string)Message["Language4"];
            bool fixedText = (bool)Message["TextFixed"];
            bool updateTranslation = (bool)Message["UpdateTranslation"];
            bool isDeleted = (bool)Message["isDeleted"];

            /*
              ID                : ユニークな文章ID
              Text1-4           : 翻訳にかける前の母国語文章
              Lang1-4           : 翻訳先言語コード
              Native            : 母国語言語コード
              fixedText         : 文章が確定したとき
              UpdateTranslation : 翻訳が更新されたとき
              isDeleted         : この文が削除されたとき
            */


            /* ここに処理を差し込む -start */


            /* ここに処理を差し込む -end */

            returnData["Text1"] = Text1;
            returnData["Text2"] = Text2;
            returnData["Text3"] = Text3;
            returnData["Text4"] = Text4;
            return returnData;
        }

        /****************************************************************
        翻訳後の補正　　                 
        *****************************************************************/
        public Dictionary<string, object> PostTranslation(Dictionary<string, object> Message)
        {
            Dictionary<string, object> returnData = new Dictionary<string, object>();
            string ID = (string)Message["ID"];
            String Text1 = (string)Message["Text1"];
            String Text2 = (string)Message["Text2"];
            String Text3 = (string)Message["Text3"];
            String Text4 = (string)Message["Text4"];
            String Native = (string)Message["LanguageNative"];
            String Lang1 = (string)Message["Language1"];
            String Lang2 = (string)Message["Language2"];
            String Lang3 = (string)Message["Language3"];
            String Lang4 = (string)Message["Language4"];
            bool fixedText = (bool)Message["TextFixed"];
            bool updateTranslation = (bool)Message["UpdateTranslation"];
            bool isDeleted = (bool)Message["isDeleted"];

            /*
              ID                : ユニークな文章ID
              Text1-4           : 翻訳された文章
              Lang1-4           : 翻訳先言語コード
              Native            : 母国語言語コード
              fixedText         : 文章が確定したとき
              UpdateTranslation : 翻訳が更新されたとき
              isDeleted         : この文が削除されたとき
            */

            /* ここに処理を差し込む -start */


            /* ここに処理を差し込む -end */

            returnData["Text1"] = Text1;
            returnData["Text2"] = Text2;
            returnData["Text3"] = Text3;
            returnData["Text4"] = Text4;
            return returnData;
        }

        /****************************************************************
  　      字幕に変化があったとき           
　       *****************************************************************/
        public void UpdateText(Dictionary<string, object> Message)
        {

            /*
              Text1 か Text6 に母国語が入る（上に表示するなら1、下に表示するなら6）
              Text2 ～ Text5 は翻訳の文章が入る          
            */
            String Lang1 = (string)Message["Language1"];
            String Lang2 = (string)Message["Language2"];
            String Lang3 = (string)Message["Language3"];
            String Lang4 = (string)Message["Language4"];
            String Lang5 = (string)Message["Language5"];
            String Lang6 = (string)Message["Language6"];
            String Text1 = (string)Message["Text1"];
            String Text2 = (string)Message["Text2"];
            String Text3 = (string)Message["Text3"];
            String Text4 = (string)Message["Text4"];
            String Text5 = (string)Message["Text5"];
            String Text6 = (string)Message["Text6"];

            /*
              fixedText         : 文章が確定したとき
              UpdateTranslation : 翻訳が更新されたとき
              isDeleted         : この文が削除されたとき
            */
            bool fixedText = (bool)Message["TextFixed"];
            bool UpdateTranslation = (bool)Message["UpdateTranslation"];
            bool isDeleted = (bool)Message["isDeleted"];

            /* ここに処理を差し込む -start */


            /* ここに処理を差し込む -end */

        }

        /****************************************************************
         トリガーを押されたとき           
        *****************************************************************/
        public void onTrigger(Dictionary<string, object> Message)
        {

        }
        /****************************************************************
          HTTPコール    　                 
        *****************************************************************/
        public void onCommand(Dictionary<string, object> Message)
        {
            /*
              HTTP経由でコールされたときに呼ばれる。
              pluginCommandにコマンドが入る。ほかの検索クエリは分解されて Messageにはいっている
            */
            string pluginName = (string)Message["target"];
            string pluginCommand = (string)Message["command"];
        }

        /****************************************************************
           公開インタフェイス
           *インタフェイスが存在しない場合は null となる。呼び出し時注意
        *****************************************************************/

        //プラグインアイコン
        //public delegate void RegistIcon(string name, byte[] bitmap);

        //内部設定の取得
        //public delegate string GetSettingParam(string name);

        //内部設定の保存
        //public delegate void SetSettingParam(string name, string value);

        //文章の送信 (ユニークな文章ID、文章、文が確定しているかどうか？）
        //public delegate void SendTextdata(string ID, string text, bool isFixed);

        //デバッグログ出力
        //public delegate void WriteLog(string message);

        //翻訳の実行　(戻り値の["text"]に翻訳結果が入る)
        //public delegate Dictionary<string, object> getTranslate(string lang, string _Text)

        //内部設定の取得（リスト）
        //public delegate string GetSettingParamList(string name, string key);

        //内部設定の削除（リスト）
        //public delegate void RemoveSettingParamList(string name, string key);

        //内部設定の存在確認（リスト）
        //public delegate bool GetContainsParamList(string name, string key);

        //内部設定の設定（リスト。Senderには Plugin_Tagを指定する）
        //public delegate void SetSettingParamList(string name, string key, string value, string Sender);
    }
}
