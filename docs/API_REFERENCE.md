# プラグイン API リファレンス（ゆかりねっとコネクターNEO v3.0）

ゆかりねっとコネクターNEO v3.0（.NET 10 版）の本体が、プラグインをどう読み込み、どのフックをどう呼ぶか、
どんな値・コールバックを渡すかをまとめたものです。雛形（`PluginFunction.cs` / `UserProgram.cs`）はこの仕様に合わせてあります。

- [1. 読み込みの契約](#1-読み込みの契約)
- [2. フック一覧](#2-フック一覧)
- [3. フックの呼ばれ方（タイムアウトとブレーカ）](#3-フックの呼ばれ方タイムアウトとブレーカ)
- [4. onLoad で渡される値とコールバック](#4-onload-で渡される値とコールバック)
- [5. 各フックに渡される Message](#5-各フックに渡される-message)
- [6. フックの戻り値](#6-フックの戻り値)
- [7. getSettingParam で取れる値](#7-getsettingparam-で取れる値)
- [8. setSettingParam の特別な名前](#8-setsettingparam-の特別な名前)
- [9. 内部設定（リスト）](#9-内部設定リスト)
- [10. 字幕を入力として送る（SendTextdataV2）](#10-字幕を入力として送るsendtextdatav2)
- [11. 翻訳・言語判定・読み上げ・AI](#11-翻訳言語判定読み上げai)
- [12. プラグイン間通信と HTTP コマンド](#12-プラグイン間通信と-http-コマンド)
- [13. plugin_prop.json](#13-plugin_propjson)

---

## 1. 読み込みの契約

本体は `Plugin\<フォルダ>\*.dll` を `Assembly.LoadFrom` で読み込み、次の手順でプラグインを認識します。

1. DLL の中から、`FullName` が **`.PluginFunction` で終わる型**を 1 つ探す（名前空間は自由。クラス名は `PluginFunction` にする）。
2. 引数なしのコンストラクタでインスタンスを作る。
3. リフレクションで次を読む。

| 名前 | 形 | 説明 |
|---|---|---|
| `Plugin_ID` | **フィールド**（string） | 必須。GUID。空だと読み込まれない。他のプラグインとぶつかると正常に読み込まれない |
| `Plugin_Tag` | **フィールド**（string） | 識別タグ。設定ファイル名（`{Plugin_Tag}.config`）などに使われる。無いと `Plugin_ID` が使われる |
| `Plugin_Name` | **プロパティ**（string） | 一覧に出る名前。フィールドにすると読めず、DLL のファイル名が表示される |
| `Plugin_ApiVersion` | フィールド（int・任意） | SDK を使ったプラグインだけが持つ。本体の対応上限（現在 `1`）を超えると読み込みを拒否する。無いプラグインは従来通り読み込まれる |

4. `Type.GetMethods()` で列挙される public メソッドの**名前の完全一致**で、フックの有無を判定する（次章）。

> [!NOTE]
> 依存 DLL（`Newtonsoft.Json.dll` など）は、本体がすでに同じ名前のものを読み込んでいれば**本体の版**が使われます（.NET 10 では同じ名前のアセンブリはプロセスで 1 つ）。
> 本体より新しい版を前提にした API は使わないでください。

---

## 2. フック一覧

| フック | シグネチャ | 呼ばれるとき | スレッド | 保護 |
|---|---|---|---|---|
| `onLoad` | `void onLoad(Dictionary<string, object>)` | 起動時（有効・無効にかかわらず） | UI スレッド | なし |
| `onEnabled` | `void onEnabled(Dictionary<string, object>)` | 「使う」をオンにしたとき、起動時に有効だったとき | UI スレッド | なし |
| `onDisabled` | `void onDisabled(Dictionary<string, object>)` | 「使う」をオフにしたとき | UI スレッド | なし |
| `onRequestConfigWindow` | `void onRequestConfigWindow()` | 設定ボタンを押したとき | UI スレッド | なし |
| `PostRecognition` | `string PostRecognition(Dictionary<string, object>)` | 音声認識のあと（発話のたび） | 専用スレッド | 8 秒 + ブレーカ |
| `PreTranslation` | `Dictionary<string, object> PreTranslation(Dictionary<string, object>)` | 翻訳の前（削除時は呼ばれない） | 専用スレッド | 8 秒 + ブレーカ |
| `PostTranslation` | `Dictionary<string, object> PostTranslation(Dictionary<string, object>)` | 翻訳のあと（削除時は呼ばれない） | 専用スレッド | 8 秒 + ブレーカ |
| `UpdateText` | `void UpdateText(Dictionary<string, object>)` | 字幕に変化があったとき | 専用スレッド | 8 秒 + ブレーカ |
| `CaptionAfterView` | `void CaptionAfterView(Dictionary<string, object>)` | 字幕を表示したあと | 専用スレッド | 8 秒 + ブレーカ |
| `onCommand` | `string onCommand(Dictionary<string, object>)` | HTTP の `/api/command`、他のプラグインの `SendCommand` | 専用スレッド | 8 秒 + ブレーカ |
| `onTerminate` | `void onTerminate(Dictionary<string, object>)` | アプリ終了時 / 「設定の読み込み」ボタン | 終了時: 別スレッド / 設定の読み込み: UI スレッド | 終了時のみ **3 秒** |
| `beforeSendInjection` | — | 本体内部用（引数・戻り値が本体内部の型のため、外部のプラグインでは使えません） | | |

- `onTrigger` は v3.0 では呼ばれません。
- `onLoad` と `onTerminate` 以外は「メソッドがあるかどうか」で呼ぶかが決まります。**使わないフックは宣言しないでください**（空でも発話のたびに呼び出しのコストがかかります）。
- 逆に、補助の public メソッドにこれらの名前を付けるとフックとして呼ばれてしまいます。補助メソッドは `private` にしてください。

### UI スレッドで呼ばれるもの（onLoad / onEnabled / onDisabled / onRequestConfigWindow）

- ホストの UI スレッドで同期実行され、**タイムアウトがありません**。ここでブロックした時間は、そのままアプリ全体が止まる時間になります。
- 通信・プロセス・デバイスの停止（`Stop()` / `Dispose()` / `Disconnect()` / `WaitForExit()` など）はバックグラウンドへ逃がし、すぐに返してください。待つ場合は必ず上限を付けてください。
- 本体は 5 秒でログに記録し、20 秒で「プラグイン◯◯が応答しません」というダイアログを出しますが、固まった状態そのものは解けません。
- 実行中は `SynchronizationContext` が退避されているため、`onLoad` の中で `SynchronizationContext.Current` を控えても `null` です。UI スレッドへ戻るには `Dispacher`（[4 章](#4-onload-で渡される値とコールバック)）を使ってください。

### onTerminate

- **アプリ終了時**は UI スレッドとは別のスレッドから、3 秒のタイムアウト付きで呼ばれます。このとき UI スレッドは完了を待ってブロックしています。
  - 保存を一番先に済ませてください（3 秒で打ち切られると、その後ろは実行されません）。
  - WinForms の窓は作成元スレッドへ **`BeginInvoke`（待たない）** で渡して破棄してください。`Invoke`（待つ）にすると UI スレッドと相互に固まります。雛形の `DisposeWindowSafely` / `CloseWindowSafely` がこの形です。
  - `setSettingParam("Voice.Mute", …)` など、UI スレッドを待つ API を呼ばないでください。
- **「設定の読み込み」ボタン**のときは UI スレッドから直接呼ばれます。

### 設定画面（onRequestConfigWindow）

- ホストは WPF アプリで、WinForms の窓はその中で開きます。
- モードレス（`Show()`）の WinForms 窓には Enter / ESC / Tab が届きません。`ShowDialog()`（モーダル）なら効きます。モードレスで ESC を効かせるには `KeyPreview = true` にして `OnKeyDown` で拾ってください。
- ボタンの変数名を `AcceptButton` / `CancelButton` にしないでください（Form の同名プロパティを隠して Enter / ESC が効かなくなります）。
- `Form.Show(owner)` は表示中だと例外になります。もう一度開くときは `Visible` を見て `Activate()` してください。
- 本体の表示言語は `getSettingParam("UILanguage")`（2 文字コード）で取れます。画面の文言の出し分けに使えます。

---

## 3. フックの呼ばれ方（タイムアウトとブレーカ）

`PostRecognition` / `PreTranslation` / `PostTranslation` / `UpdateText` / `CaptionAfterView` / `onCommand` / 終了時の `onTerminate` は、次の仕組みで呼ばれます。

- **プラグインごとに専用スレッドが 1 本**あり、フックはそこで**直列に**実行されます。同じプラグインのフックが並行・再入実行されることはありません。
- 呼び出し元は既定 **8 秒**待ちます。超えると戻り値の代わりに元の値を使って先へ進みます（フック自体は裏で走り続けます）。
- 実行が始まる前にタイムアウトした呼び出し（前のフックが詰まっていて順番が来なかった）は破棄されます。つまり **1 つのフックが詰まると、同じプラグインのほかのフックも巻き添えでスキップされます**。
- **サーキットブレーカ**: 「プラグイン × フック種別」ごとに、実行中のタイムアウトが **3 回連続**すると一時的に呼ばれなくなり、画面にも通知されます。**120 秒後**に自動で再開します。1 回でも成功すれば連続回数はリセットされます。
- 打ち切られたまま終わっていない呼び出しが 2 本残っているフックは、それが終わるまで呼ばれません。
- **高速パス**: 50ms 以下で終わる呼び出しが 8 回続いたフックは、専用スレッドを経由せず呼び出し元で直接実行されます。**この間はタイムアウト保護が効きません**（一度でも 50ms を超えれば保護経路へ戻ります）。「いつもは速いが、たまに長く止まる」フックは、その 1 回でパイプライン全体を止めることになります。

**時間のかかる処理（ネットワーク・ファイル I/O・同期版のホスト API）は必ずバックグラウンドへ逃がし、フック自体はすぐに値を返してください。**
雛形では `RunBackground(() => { ... })`（非同期の処理は `RunBackground(async () => { await ...; })`）、SDK では `SafeTask.Run(...)` が使えます。

---

## 4. onLoad で渡される値とコールバック

`onLoad(Dictionary<string, object> Message)` に渡されるキーの一覧です。コールバックはすべて `Action` / `Func` の型で入っています（`dynamic` ではありません）。
**本体と同じデリゲート型で受け取ってください。** 型が 1 つでもずれると `as` の結果が `null` になり、気づかないまま機能が動かなくなります。
古い本体には存在しないキーもあるので、呼び出す前に必ず `null` チェックしてください。

### 値

| キー | 型 | 雛形のフィールド | 説明 |
|---|---|---|---|
| `ConfigFolder` | string | `ConfigPath` | 設定ファイルの保存先フォルダ |
| `PluginFolder` | string | `PluginPath` | このプラグインの DLL があるフォルダ |
| `LogFolder` | string | `LogPath` | ログフォルダ |
| `Version` | string | `HostVersion` | 本体のバージョン表記 |
| `Enabled` | bool | — | 起動時に有効かどうか |
| `DebugMode` | bool | `HostDebugMode` | 本体のデバッグオプション |
| `isKeyAssignMode` | bool | — | 常に `false` |
| `Dispacher` | `System.Windows.Threading.Dispatcher` | `HostDispatcher` | 本体の UI スレッドの Dispatcher（**キー名の綴りはこのまま**） |

### コールバック

| キー | 型 | 雛形のフィールド | 説明 |
|---|---|---|---|
| `WriteLog` | `Action<string>` | `writeLog` | デバッグログ出力 |
| `GetSettingParam` | `Func<string, string>` | `getSettingParam` | 本体の設定・状態を取得（[7 章](#7-getsettingparam-で取れる値)） |
| `SetSettingParam` | `Action<string, string, string>` | `setSettingParam` | 本体へ値を設定 `(name, value, sender)`。`sender` には `Plugin_Tag`（[8 章](#8-setsettingparam-の特別な名前)） |
| `RegistIcon` | `Action<string, byte[]>` | `registIcon` | プラグインアイコンの登録（※現行の本体では何もしません。アイコンは `plugin_prop.json` で指定） |
| `SendTextdata` | `Action<string, string, bool>` | `sendTextdata` | 字幕を入力として送る `(ID, text, isFixed)` |
| `SendTextdataV2` | `Action<string, string, bool, string, string, Dictionary<string, object>>` | `sendTextdataV2` | 字幕を入力として送る（拡張版）`(ID, text, isFixed, userId, userName, optionData)`（[10 章](#10-字幕を入力として送るsendtextdatav2)） |
| `SendCommand` | `Func<string, string, Dictionary<string, object>, string>` | `sendCommand` | 他のプラグインの `onCommand` を呼ぶ `(senderID, targetTag, args)` → 戻り値（[12 章](#12-プラグイン間通信と-http-コマンド)） |
| `GetTranslate` | `Func<string, string, Dictionary<string, object>>` | `getTranslate` | 翻訳（同期）`(lang, text)` ※フック内で使わない |
| `GetTranslateAsync` | `Func<string, string, Task<Dictionary<string, object>>>` | `getTranslateAsync` | 翻訳（非同期） |
| `GetLanguage` | `Func<string, Dictionary<string, object>>` | `getLanguage` | 言語判定（同期）`(text)` ※フック内で使わない |
| `GetLanguageAsync` | `Func<string, Task<Dictionary<string, object>>>` | `getLanguageAsync` | 言語判定（非同期） |
| `SendSpeech` | `Func<string, string>` | `sendSpeech` | 読み上げ `(text)` |
| `SendSpeechV2` | `Func<string, Dictionary<string, object>, string>` | `sendSpeechV2` | 読み上げ（拡張版）`(text, param)` |
| `StopSpeech` | `Func<string>` | `stopSpeech` | 読み上げの停止 |
| `FinishCaptionForSpeech` | `Action<string>` | `finishCaptionForSpeech` | 読み上げ側が「この発話の読み上げが終わった」を後から知らせる `(messageID)`（読み上げプラグイン向け） |
| `CallGPTFunction` | `Func<string, string, string>` | `callGPTFunction` | 本体の GPT 機能の呼び出し `(method, query)` ※同期 |
| `GetSentiment` | `Func<string, string>` | `getSentiment` | 感情分析 `(text)` ※同期 |
| `CallJevAsync` | `Func<string, string, string, int, Task<string>>` | `callJevAsync` | 判定専用モデル（Jev）に判定させる `(stateJson, questionsJson, model, timeoutMs)` → JSON（[11 章](#11-翻訳言語判定読み上げai)） |
| `SetWebPathMount` | `Action<string, string>` | `setWebPathMount` | 内蔵 Web サーバの URL パスにファイルを割り当てる `(urlPath, filePath)`。`filePath` が `""` で解除 |
| `SetAddString` | `Action<string, string>` | `setAddString` | 内蔵 Web サーバが返すファイルに文字列を追加する `(filePath, data)` |
| `AddLayoutItem` | `Action<string, string, string, string, string, string, string, bool>` | `addLayoutItem` | レイアウト一覧に項目を追加 `(id, title, category, description, iconFile, filePath, urlPath, directPath)` |
| `RemoveLayoutItem` | `Action<string>` | `removeLayoutItem` | レイアウト一覧から項目を削除 `(id)` |
| `ReloadLayoutList` | `Action` | `reloadLayoutList` | レイアウト一覧の再読み込み |
| `GetSettingParamList` | `Func<string, string, string>` | `getSettingParamList` | 内部設定（リスト）の取得 `(name, key)`（[9 章](#9-内部設定リスト)） |
| `SetSettingParamList` | `Action<string, string, string, string>` | `setSettingParamList` | 内部設定（リスト）の設定 `(name, key, value, sender)` |
| `RemoveSettingParamList` | `Action<string, string>` | `removeSettingParamList` | 内部設定（リスト）の削除 `(name, key)` |
| `GetContainsParamList` | `Func<string, string, bool>` | `getContainsParamList` | 内部設定（リスト）の存在確認 `(name, key)` |

> [!WARNING]
> `GetTranslate` / `GetLanguage` / `CallGPTFunction` / `GetSentiment` は内部で完了を待つ同期版です。フックの中で呼ぶと専用スレッドが塞がり、タイムアウトの原因になります。
> フックの中では `RunBackground` の中で非同期版を使ってください。

---

## 5. 各フックに渡される Message

フックに渡される `Message` は、呼ばれる経路によって**入っていないキーがあります**。`(bool)Message["xxx"]` のような直接キャストは例外の原因になるので、
雛形の `GetValue(Message, "xxx", 既定値)`（SDK なら `TryGetValue`）で読んでください。
また、`SendTextdataV2` の `optionData` などで渡された追加のキーも、そのまま `Message` に入ってきます。

### 共通して入ることが多いキー

| キー | 型 | 説明 |
|---|---|---|
| `ID` | string | 文章のユニーク ID（未確定 → 確定まで同じ ID） |
| `TextFixed` | bool | 文章が確定したか |
| `UpdateTranslation` | bool | 翻訳が更新されたか |
| `isDeleted` | bool | この文が削除されたか |
| `LanguageNative` | string | 母国語の言語コード |
| `ShowCaptionMode` | int | 字幕の表示モード |
| `talkerID` / `talkerName` | string | 話者 |
| `isOwnersTalkData` | bool | 配信者本人の発話か |
| `KeepTime` | double | この字幕の表示時間（秒） |
| `CaptureTimeMs` | long | 発話をキャプチャした時刻（Unix ミリ秒） |
| `ReadingPhrase` | string | 読み上げ用のかな（ある場合） |
| `isClearTimer` | bool | 内部用 |
| `LogFolder` / `ConfigFolder` / `PluginFolder` / `Version` | string | `onLoad` と同じ |

### フックごとのキー

| フック | 主なキー |
|---|---|
| `PostRecognition` | `Text`（音声認識された母国語の文章） |
| `PreTranslation` | `Text1`〜`Text4`（翻訳にかける前の母国語の文章）、`Language1`〜`Language4`（翻訳先の言語コード） |
| `PostTranslation` | `Text1`〜`Text4`（翻訳された文章）、`Language1`〜`Language4` |
| `UpdateText` | `Text1`〜`Text6`、`Language1`〜`Language6`、`NativeText`、`AuthorName`、`InsertID`。母国語は上に表示するなら `Text1`、下に表示するなら `Text6` に入り、`Text2`〜`Text5` に翻訳が入る |
| `CaptionAfterView` | `UpdateText` とほぼ同じキーに加え、`DrawParam`（表示に使った内容。ルビの HTML は外したもの） |
| `onCommand` | `target`（宛先の `Plugin_Tag`）、`command`、ほかに HTTP のクエリ / `SendCommand` の引数がそのまま入る |
| `onEnabled` / `onDisabled` | `LogFolder` / `ConfigFolder` / `PluginFolder` / `Version` / `Enabled` |

---

## 6. フックの戻り値

| フック | 戻り値 |
|---|---|
| `PostRecognition` | 補正後の文章（`string`）。タイムアウト時は元の文章が使われる |
| `PreTranslation` | `Dictionary<string, object>`。下表 |
| `PostTranslation` | `Dictionary<string, object>`。下表 |
| `onCommand` | 呼び出し元へ返す文字列。HTTP の `/api/command` では、JSON として読めれば `application/json` で返る |
| そのほか | なし |

### PreTranslation / PostTranslation で返せるキー

| キー | 型 | 対象 | 説明 |
|---|---|---|---|
| `Text1`〜`Text4` | string | 両方 | **必須**。必ず 4 つとも入れて返す |
| `NativeText` | string | `PostTranslation` | 母国語の文章も差し替える。空白だけの文字列は採用されない |
| `KeepTime` | 数値（秒） | 両方 | この字幕だけの表示時間 |
| `ProcessAbort` | bool | 両方 | `true` でこの発話の処理を打ち切る（字幕は表示されない） |
| `KeepTimePending` | bool | `PostTranslation` | 「表示時間は後で知らせる」の申告（読み上げプラグインが `FinishCaptionForSpeech` と組み合わせて使う。`KeepTime` を返した場合は無視） |

---

## 7. getSettingParam で取れる値

| name | 戻り値 |
|---|---|
| `UILanguage` | 本体の表示言語（2 文字コード。翻訳先の言語とは別） |
| `Native` | 母国語の言語コード |
| `Lang1`〜`Lang4` | 翻訳先 1〜4 の言語コード |
| `TalkerName` / `TalkerID` | 話者名 / 話者 ID |
| `Voice.Mute` | ミュートの状態 `"on"` / `"off"`（古い本体では `""`） |
| `BrowserURL` / `BrowserBaseURL` | 字幕ブラウザの URL |
| `BrowserPath` | 選択中のブラウザのパス |
| `InnerPort` | 内蔵 Web サーバのポート |
| `WebSocketPort` | WebSocket のポート |
| `isIPAddressFullOpen` | 外部からの接続を許可しているか `"yes"` / `"no"` |
| `BackgroundColor` | 字幕の背景色 |
| `isEnabledPlugin.<Plugin_Tag>` | そのプラグインが有効か `"yes"` / `"no"` |
| `EnabledPluginList` | 有効なプラグインの一覧（JSON 配列。`Name` / `Tag`） |
| `isFanboxMode` / `isFanboxLv` | 支援者向け機能の状態 |
| 上記以外 | `setSettingParam` で設定された値（無ければ `""`） |

---

## 8. setSettingParam の特別な名前

`setSettingParam(name, value, Plugin_Tag)` で、次の名前は本体が特別に扱います。上記以外の名前は本体内のメモリに保存され、`getSettingParam` で読み出せます（他のプラグインとの値の受け渡しにも使えます）。

### 状態の表示（`Status` / `StatusColor`）

プラグイン一覧の「● 待機中」などの表示と、上部のプラグインの帯の色は、この 2 つの値から本体が決めます。雛形の `SetStatus(statusColor, statusText)` を使うと簡単です。

```csharp
setSettingParam("Status", "OBS に接続しました", Plugin_Tag);   // 状態の文（先に送る）
setSettingParam("StatusColor", "Connected", Plugin_Tag);       // 状態
```

| `StatusColor` | 一覧の表示 | 送るとき |
|---|---|---|
| `Connected` | 接続中（緑） | 相手（OBS・VRChat・配信サイト・外部アプリなど）とつながった |
| `Disconnected` | 未接続（黄） | つながっていない / 切れた / つなぎ直しを待っている |
| `Idle` | 待機中（緑） | 動けるが、いまは何もしていない |
| `Inactive` | 休止中（灰） | 設定で機能を切っている・前提が揃っていないなど（エラーではない） |
| `Normal` | 動作中（緑） | 相手は無いが、動いていることを示したい |
| `Run` | 処理中（青） | 時間のかかる処理の最中（終わったら元の状態へ戻す） |
| `Warning` | 注意（黄） | 動いているが気を付けてほしい（一時的な失敗・制限など） |
| `Error` | エラー（赤） | 設定の誤り・認証の失敗など、**利用者が直さないと動かない** |
| `Fail` | 失敗（紫） | 処理が失敗した（次の処理で回復し得る） |

決まりごと:

1. 相手のあるプラグインは、つながったら `Connected`、切れたら `Disconnected` を必ず送る。起動直後（まだつないでいない）も `Disconnected` を送る。
2. `Error` のときは `Status` に直し方の分かる短い文を送る（例:「認証失敗（OBS の WebSocket パスワードを確認）」）。`Status` の文は、エラー・失敗・注意・未接続・休止中のときだけ一覧の 2 行目に出る。
3. 一時的な失敗は `Warning`。次に成功したら元の状態へ戻す（戻し忘れると注意のまま残る）。
4. `onDisabled` では何も送らなくてよい。
5. 状態が実際に変わったとき（接続・切断・失敗・回復）だけ送る。送信が成功するたびに送らない（帯の塗り直しが UI スレッドで走る）。

なお、`onLoad` / `onEnabled` が例外で終わったときの「停止」、フックが続けてタイムアウトしたときの「一部停止」などは本体が自動で表示します。

### 字幕テンプレートへ CSS を足す（`<Plugin_Tag>.customCSS`）

```csharp
setSettingParam(Plugin_Tag + ".customCSS", css, Plugin_Tag);   // 足す（onEnabled で送る）
setSettingParam(Plugin_Tag + ".customCSS", "", Plugin_Tag);    // 取り下げる（onDisabled で必ず）
```

- 名前が `.customCSS` で終わるものを本体がすべて集め、名前の順に `user.css` の後ろへ足します。
- 本体は起動の終わりに `user.css` を書き直すので、前回の起動で足したものは送り直さなければ消えます。`onEnabled` で送ってください。
- 変更した CSS は、字幕の画面を読み込み直すと効きます。
- クラス名はテンプレートとぶつからない接頭辞を付けてください。`filter` は使わないでください（字幕の描き直しが大きく遅れます）。`text-shadow` はテンプレートの縁取りに使われているので上書きしないでください。

### ミュート（`Voice.Mute`）

```csharp
string now = getSettingParam("Voice.Mute");          // "on" / "off"
setSettingParam("Voice.Mute", "on", Plugin_Tag);     // on にすると表示中の字幕も消える
```

- 一時的に切り替えるなら、元の状態を読んでおき、元からミュートなら触らない・終わったら戻す。
- 切り替えは本体の UI スレッドで行われます。**UI スレッドを待っているスレッド（とくに `onTerminate`）から呼ぶと固まります。**
- on にした直後 10ms 以内に出した字幕は、ミュートの消去に巻き込まれることがあります。

---

## 9. 内部設定（リスト）

`setSettingParamList(name, key, value, Plugin_Tag)` などで、名前付きのリストに値を出し入れできます。次の名前は本体が参照します。

| name | 意味 | 雛形の関数 |
|---|---|---|
| `Common.sendSpeech.PluginEnabled` | 読み上げ API（`SendSpeech` / `SendSpeechV2`）で呼ばれる読み上げプラグインとして登録する（key = `Plugin_Tag`） | `enabledFunctionOnSpeech()` / `disabledFunctionOnSpeech()` |
| `Common.Translate.PluginEnabled` | 翻訳サーバで使われるプラグインとして登録する（key = `Plugin_Tag`） | `enabledFunctionOnTranslateServer()` / `disabledFunctionOnTranslateServer()` |

> [!NOTE]
> 旧雛形にあった `Common.sendSpeechV2.PluginEnabled` は、現行の本体では参照されません。

---

## 10. 字幕を入力として送る（SendTextdataV2）

```csharp
sendTextdataV2?.Invoke(id, text, isFixed, userId, userName, optionData);
```

- ミュート中でも通ります（ミュートを見ているのは音声認識の入口と WebSocket だけです）。
- `id` が空の確定文には本体が GUID を振ります。`id` が空の未確定文は捨てられます。

`optionData` で使えるキー:

| キー | 型 | 意味 |
|---|---|---|
| `APIRoute` / `Client` / `ServiceName` / `ServiceTag` | string | どこから来た字幕か（ログ・フックでの判定用） |
| `IgnorePlugin` | `List<string>` | ここに入れたプラグイン（`Plugin_Tag`）にはフックで渡さない。読み上げさせないときは `Plugin_PlayVoice` / `Plugin_Bouyomi` / `Plugin_Softalk` を入れる |
| `donotTranslate` | bool | 翻訳しない |
| `ReadingPhrase` | string | 読み上げ用のかな |
| `KeepTime` | 数値（秒） | この字幕だけの表示時間。0 以下は「時間では消さない」 |
| `Delete` | bool | `true` でこの `id` の字幕を今すぐ消す（`text` は無視）。**消した `id` は使い回さないこと**（二度と表示されなくなる） |
| `SelfTimed` | bool | `true` で「表示する時刻も消す時刻も送り手が管理する」と宣言し、前の字幕を読み切るまで待つ制御から外す。時刻どおりに `Delete` で消す送り手だけが使うこと |

時刻どおりに出して消したいときは、出すときに `KeepTime`（表示したい長さ + 余白）と `SelfTimed = true` を付け、時間になったら `Delete` で消します。`Delete` が届かなくても `KeepTime` が消し忘れを防ぎます。

---

## 11. 翻訳・言語判定・読み上げ・AI

### 翻訳 / 言語判定

`GetTranslate(Async)(lang, text)` と `GetLanguage(Async)(text)` は次の `Dictionary<string, object>` を返します。

| キー | 型 | 説明 |
|---|---|---|
| `result` | bool | 成功したか |
| `lang` | string | 翻訳先の言語コード / 判定された言語コード（判定できないときは `"unknow"`） |
| `text` | string | 翻訳結果 / 元の文章（失敗時は元の文章） |

```csharp
RunBackground(async () =>
{
    if (getTranslateAsync == null) return;
    var ret = await getTranslateAsync("en", text);
    if (ret != null && ret.TryGetValue("result", out object ok) && ok is bool b && b)
    {
        string translated = ret["text"] as string;
        // ...
    }
});
```

### 読み上げ（SendSpeechV2）

`SendSpeechV2(text, param)` の `param` で使えるキー: `talkerName`（声の名前）、`autherName`、`platformName`、`userId`、`userName`（いずれも string）、`LimitParam`（bool）、`voicePlayAsync`（bool）。
読み上げは `Common.sendSpeech.PluginEnabled` に登録されたプラグインに渡されます。

### Jev（CallJevAsync）

判定専用モデルに判定させる口です。`(stateJson, questionsJson, model, timeoutMs)` を渡すと JSON 文字列が返ります。例外は投げません。

- 成功: `{"ok":true,"answers":{...},"usage":{...},"elapsedMs":n}`
- 失敗: `{"ok":false,"error":"nokey|timeout|http NNN ...","elapsedMs":n}`
- `model` が空なら本体の設定値を使います。API キーは本体の「APIキー」画面で入力するもので、プラグインには渡りません。
- フックの中では撃つだけにして待たないでください。

---

## 12. プラグイン間通信と HTTP コマンド

### 他のプラグインを呼ぶ

```csharp
string ret = sendCommand?.Invoke(Plugin_Tag, "Plugin_Target", new Dictionary<string, object>
{
    ["command"] = "hello",
    ["text"] = "…",
});
```

- 宛先（`Plugin_Tag`、大文字小文字は区別しない）の `onCommand` が呼ばれ、その戻り値が返ります。宛先が無効・`onCommand` を持たない場合は `null` です。
- 宛先の `Message["target"]` には宛先のタグが入ります。

### HTTP から呼ぶ

```
http://localhost:<InnerPort>/api/command?target=<Plugin_Tag>&command=<コマンド>&key=value...
```

- クエリのキーがすべて `Message` に入って `onCommand` が呼ばれます。
- `onCommand` の戻り値が JSON として読めれば `application/json`、そうでなければテキストとして返ります。
- ポートは `getSettingParam("InnerPort")` で取れます。

---

## 13. plugin_prop.json

プラグインのフォルダに置く説明ファイルです。「プラグインを追加」画面のジャンル・表示名・説明・前提条件になります。
無い・壊れているときも一覧からは消えず、「その他」として表示されます。

```json
{
  "tag": "Plugin_Sample",
  "id":  "00000000-0000-0000-0000-000000000000",
  "icon": "icon.png",
  "ja": { "category": "読み上げ", "title": "表示名", "summary": "何ができるか", "require": "前提条件" },
  "en": { "category": "Speech",   "title": "Title",  "summary": "What it does",  "require": "Requirements" }
}
```

- `tag` / `id` は照合用です。`Plugin_Tag` / `Plugin_ID` と違っていても採用されますが、ログに警告が出ます。
- 言語ブロックは「表示言語 → `ja` → `en` → 最初に見つかった `title` 付きのブロック」の順に探されます。
- `icon` はプラグインのフォルダからの相対パスです。実在しないときは使われません。
- csproj で `CopyToOutputDirectory` を付け、出力（= 配布フォルダ）に置いてください（雛形は設定済み）。
