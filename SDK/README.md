# YNC_PluginSdk（参照用）

ゆかりねっとコネクターNEO v3.0 以降の本体に同梱されている、プラグイン共通 SDK の参照用 DLL です。
プラグインをコンパイルするときの参照先として使います。

**SDK は必須ではありません。** リポジトリ直下の雛形（`Plugin_Prototype`）は SDK を使わずに同じ機能を持っています。

| ファイル | 内容 |
|---|---|
| `lib/YNC_PluginSdk.dll` | SDK 本体（参照用） |
| `lib/YNC_PluginSdk.xml` | ドキュメントコメント（Visual Studio の補完に説明が出ます） |

## 使い方

プラグインの csproj から、`Private=false`（出力にコピーしない）で参照します。

```xml
<ItemGroup>
  <Reference Include="YNC_PluginSdk">
    <HintPath>..\SDK\lib\YNC_PluginSdk.dll</HintPath>
    <Private>false</Private>
  </Reference>
</ItemGroup>
```

使用例は `../Samples/Plugin_SdkSample/` を参照してください。

本体をインストールしてあれば、本体のフォルダにある `YNC_PluginSdk.dll` を `HintPath` に指定しても構いません。

## 配布ルール（重要）

- **`YNC_PluginSdk.dll` をプラグインのフォルダに入れて配布しないでください。** 実行時は本体（`YNC_Neo.exe`）と同じフォルダにある 1 本が使われます。
- 同じ名前のアセンブリはプロセスで 1 つしか使われないため、版違いの DLL が混ざると `Plugin_ApiVersion` の判定や本体との連携が壊れます。

## 主なクラス

| クラス | 内容 |
|---|---|
| `PluginBase` | 基底クラス。`Plugin_ID` / `Plugin_Tag` / `Plugin_Name` / `Plugin_ApiVersion` と `onLoad` / `onTerminate` を持つ。初期化は `OnLoaded()` をオーバーライドする |
| `PluginHost` | `onLoad` で渡されるコールバックを型安全なプロパティにしたもの（`Host.WriteLog` など）。SDK にプロパティが無いキーは `Host.RawMessage` から読む |
| `ConfigStore<T>` | `{ConfigFolder}\{Plugin_Tag}.config` への設定の保存・読み込み |
| `SafeTask` | 例外でアプリを巻き込まないバックグラウンド実行 |
| `SharedHttp` | プロセスで共有する `HttpClient` |
| `PluginSdk` | `ApiVersion`（現在 `1`） |

## 更新について

本体の SDK が更新されたときは、この 2 ファイルを本体同梱の版に合わせて差し替えます。
新しい本体にしか無い API を使ったプラグインは、古い本体では `MissingMethodException` になるので注意してください。
