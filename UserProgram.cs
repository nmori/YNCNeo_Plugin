using System;
using System.Collections.Generic;
using System.Threading;
namespace YNC_NeoPlugin
{
    /*
        ユーザ向け雛形

        ★フックについて★
        ホストは「メソッドが存在するかどうか」でフックを呼ぶか決めます。
        PostRecognition / PreTranslation / PostTranslation / UpdateText / CaptionAfterView は
        発話のたびに呼ばれるため、中身が空でも存在するだけで処理コストがかかります。
        使わないフックはメソッドごと削除してください。

        ★時間のかかる処理について★
        フックはプラグインごとの専用スレッドで直列に実行され、8 秒でタイムアウトします。
        3 回続けてタイムアウトすると、そのフックは 120 秒間呼ばれなくなります。
        通信・ファイル I/O・同期版のホスト API (getTranslate など) はフックの中で待たず、
        RunBackground(() => { ... }) で裏へ逃がして、フック自体はすぐに値を返してください。
    */

    public partial class PluginFunction
    {

        /****************************************************************
         プラグインの素性
         必ずGUIツールなどでぶつからない値を設定のこと
         (public フィールドである必要があります。プロパティにすると読み込まれません)
        *****************************************************************/
        public string Plugin_ID = "8E828504-7E55-4E97-85CC-D7E4AF5273BB";

        /****************************************************************
         プラグインのユニークな名前
         DLLのファイル名部分を設定するのがデフォルトです (csproj の AssemblyName と揃える)
         ファイル名とユニーク名が異なる場合はロードに時間がかかることがあります
         (public フィールドである必要があります)
        *****************************************************************/
        public string Plugin_Tag = "Plugin_Prototype";


        /****************************************************************
           バージョン名
        *****************************************************************/
        public string Plugin_Version = "v1.0";

        /****************************************************************
           表示名（言語を指定するとユーザ言語毎に表示が差し変わります)
           (public プロパティである必要があります。フィールドにすると DLL 名が表示されます)
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
                        return $"Prototype {Plugin_Version}";

                    case "en":
                        return $"Prototype {Plugin_Version}";

                    case "fr":
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
        public string Plugin_AuthorName = "";

        /****************************************************************
           配布URL（https://～)
        *****************************************************************/
        public string Plugin_ProductURL = "";

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
            //例: string url = GetValue(ConfigData, "url", "");
        }

        /****************************************************************
          設定の保存
        *****************************************************************/
        private void saveSetting()
        {
            //永続化したい設定は Dictionary<string, object> ConfigData にいれてください
            //UI スレッド以外から呼ばれることがあるので、ここで画面 (cw) の値を読まないでください
        }

        /****************************************************************
         プラグインが有効になったとき（「使う」をオンにしたとき・起動時に有効だったとき）
        *****************************************************************/
        // UI スレッドで同期実行されます。タイムアウトはありません。待つ処理は書かないでください。
        public void onEnabled(Dictionary<string, object> Message)
        {
            //接続が必要なプラグインなら、つなぐ前に「未接続」を出しておく
            //SetStatus("Disconnected", "接続を待っています");
        }

        /****************************************************************
         プラグインが無効になったとき（「使う」をオフにしたとき）
        *****************************************************************/
        // UI スレッドで同期実行されます。タイムアウトはありません。
        // 通信やデバイスの停止 (Stop / Dispose / Disconnect など) は RunBackground で裏へ逃がし、すぐに返してください。
        public void onDisabled(Dictionary<string, object> Message)
        {
        }

        /****************************************************************
         Configを押されたとき
        *****************************************************************/
        // UI スレッドで呼ばれます。
        public void onRequestConfigWindow()
        {
            //画面へ値を反映するならこんな感じで…
            //cw.textBox1.Text = GetValue(ConfigData, "switch01", "");

            if (cw.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                //設定を保存するならこんな感じで…
                //ConfigData["switch01"] = cw.textBox1.Text;
                SaveConfig();
            }
        }



        /****************************************************************
         音声認識後の補正
        *****************************************************************/
        public string PostRecognition(Dictionary<string, object> Message)
        {
            string ID = GetValue(Message, "ID", "");
            string Text = GetValue(Message, "Text", "");

            bool fixedText = GetValue(Message, "TextFixed", false);
            bool updateTranslation = GetValue(Message, "UpdateTranslation", false);
            bool isDeleted = GetValue(Message, "isDeleted", false);
            string talkerID = GetValue(Message, "talkerID", "");
            string talkerName = GetValue(Message, "talkerName", "");
            bool isOwnersTalkData = GetValue(Message, "isOwnersTalkData", false);
            /*
              ID                : ユニークな文章ID
              Text              : 音声認識された母国語文章
              fixedText         : 文章が確定したとき
              UpdateTranslation : 翻訳が更新されたとき
              isDeleted         : この文が削除されたとき
              talkerID/Name     : 話者
              isOwnersTalkData  : 配信者本人の発話かどうか
              (ほかに LanguageNative, ShowCaptionMode, ReadingPhrase などが入ることがあります)
            */

            /* ここに処理を差し込む -start */


            /* ここに処理を差し込む -end */

            return Text;
        }

        /****************************************************************
        翻訳前の補正
        *****************************************************************/
        // 削除 (isDeleted) のときは呼ばれません。
        public Dictionary<string, object> PreTranslation(Dictionary<string, object> Message)
        {
            Dictionary<string, object> returnData = new Dictionary<string, object>();
            string ID = GetValue(Message, "ID", "");
            string Text1 = GetValue(Message, "Text1", "");
            string Text2 = GetValue(Message, "Text2", "");
            string Text3 = GetValue(Message, "Text3", "");
            string Text4 = GetValue(Message, "Text4", "");
            string Native = GetValue(Message, "LanguageNative", "");
            string Lang1 = GetValue(Message, "Language1", "");
            string Lang2 = GetValue(Message, "Language2", "");
            string Lang3 = GetValue(Message, "Language3", "");
            string Lang4 = GetValue(Message, "Language4", "");
            bool fixedText = GetValue(Message, "TextFixed", false);
            bool updateTranslation = GetValue(Message, "UpdateTranslation", false);
            double keepTime = GetValue(Message, "KeepTime", 0.0);
            long captureTimeMs = GetValue(Message, "CaptureTimeMs", 0L);

            /*
              ID                : ユニークな文章ID
              Text1-4           : 翻訳にかける前の母国語文章
              Lang1-4           : 翻訳先言語コード
              Native            : 母国語言語コード
              fixedText         : 文章が確定したとき
              UpdateTranslation : 翻訳が更新されたとき
              keepTime          : この字幕の表示時間（秒）
              captureTimeMs     : 発話をキャプチャした時刻（Unix ミリ秒）
            */


            /* ここに処理を差し込む -start */


            /* ここに処理を差し込む -end */

            // Text1～Text4 は必ず入れて返すこと
            returnData["Text1"] = Text1;
            returnData["Text2"] = Text2;
            returnData["Text3"] = Text3;
            returnData["Text4"] = Text4;
            //returnData["KeepTime"] = 6.0;        // この字幕だけ表示時間（秒）を変えたいとき
            //returnData["ProcessAbort"] = true;   // この発話の処理を打ち切りたいとき（字幕は表示されません）
            return returnData;
        }

        /****************************************************************
        翻訳後の補正
        *****************************************************************/
        // 削除 (isDeleted) のときは呼ばれません。
        public Dictionary<string, object> PostTranslation(Dictionary<string, object> Message)
        {
            Dictionary<string, object> returnData = new Dictionary<string, object>();
            string ID = GetValue(Message, "ID", "");
            string Text1 = GetValue(Message, "Text1", "");
            string Text2 = GetValue(Message, "Text2", "");
            string Text3 = GetValue(Message, "Text3", "");
            string Text4 = GetValue(Message, "Text4", "");
            string Native = GetValue(Message, "LanguageNative", "");
            string Lang1 = GetValue(Message, "Language1", "");
            string Lang2 = GetValue(Message, "Language2", "");
            string Lang3 = GetValue(Message, "Language3", "");
            string Lang4 = GetValue(Message, "Language4", "");
            bool fixedText = GetValue(Message, "TextFixed", false);
            bool updateTranslation = GetValue(Message, "UpdateTranslation", false);

            /*
              ID                : ユニークな文章ID
              Text1-4           : 翻訳された文章
              Lang1-4           : 翻訳先言語コード
              Native            : 母国語言語コード
              fixedText         : 文章が確定したとき
              UpdateTranslation : 翻訳が更新されたとき
            */

            /* ここに処理を差し込む -start */


            /* ここに処理を差し込む -end */

            // Text1～Text4 は必ず入れて返すこと
            returnData["Text1"] = Text1;
            returnData["Text2"] = Text2;
            returnData["Text3"] = Text3;
            returnData["Text4"] = Text4;
            //returnData["NativeText"] = "…";      // 母国語の文章も差し替えたいとき（空白だけの文字列は無視されます）
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
            string ID = GetValue(Message, "ID", "");
            string Lang1 = GetValue(Message, "Language1", "");
            string Lang2 = GetValue(Message, "Language2", "");
            string Lang3 = GetValue(Message, "Language3", "");
            string Lang4 = GetValue(Message, "Language4", "");
            string Lang5 = GetValue(Message, "Language5", "");
            string Lang6 = GetValue(Message, "Language6", "");
            string Text1 = GetValue(Message, "Text1", "");
            string Text2 = GetValue(Message, "Text2", "");
            string Text3 = GetValue(Message, "Text3", "");
            string Text4 = GetValue(Message, "Text4", "");
            string Text5 = GetValue(Message, "Text5", "");
            string Text6 = GetValue(Message, "Text6", "");
            string NativeText = GetValue(Message, "NativeText", "");

            /*
              fixedText         : 文章が確定したとき
              UpdateTranslation : 翻訳が更新されたとき
              isDeleted         : この文が削除されたとき
            */
            bool fixedText = GetValue(Message, "TextFixed", false);
            bool UpdateTranslation = GetValue(Message, "UpdateTranslation", false);
            bool isDeleted = GetValue(Message, "isDeleted", false);

            /* ここに処理を差し込む -start */


            /* ここに処理を差し込む -end */

        }

        /****************************************************************
          字幕の表示が終わったあと (必要な場合だけコメントを外す)
        *****************************************************************/
        //public void CaptionAfterView(Dictionary<string, object> Message)
        //{
        //    // UpdateText とほぼ同じキーに加え、DrawParam (表示に使った内容) が入る
        //}

        /****************************************************************
          コマンド (HTTP の /api/command・他のプラグインの sendCommand から)
        *****************************************************************/
        public string onCommand(Dictionary<string, object> Message)
        {
            /*
              http://localhost:<ポート>/api/command?target=<Plugin_Tag>&command=xxx&...
              のように呼ばれたとき、または他のプラグインが sendCommand で呼んだときに呼ばれる。
              command にコマンドが入る。ほかの検索クエリは分解されて Message にはいっている。

              戻り値は呼び出し元へ返る。HTTP の場合、JSON として読める文字列なら
              application/json で返される。
            */
            string pluginName = GetValue(Message, "target", "");
            string pluginCommand = GetValue(Message, "command", "");

            switch (pluginCommand)
            {
                //case "hello":
                //    return "{\"result\":\"ok\"}";

                default:
                    return "";
            }
        }
    }
}
