# 閲覧専用の PTDocumentController で Apple Pencil を使うと指のタッチが無効になる（.NET MAUI）

 `PTDocumentController` で Apple Pencil を使うと、それ以降、指でのスクロールやピンチズームが一切効かなくなる事象を検証するための、最小構成のサンプルアプリ。

-  **.NET MAUI（net10.0-ios）＋ NuGet `PDFTron.iOS` / `PDFTron.iOS.Tools` 12.1.0** で作成。
- 閲覧専用の設定（`toolManager.tool` は `PTPanTool` のまま）を常に適用し、`pencilInteractionMode`、`readonly`、`shouldSwitchToTool` などを、アプリの画面から切り替えられる。
- タッチ・ジェスチャ・ツールの状態を記録する**診断機能**を内蔵しています。ログはファイルとして書き出せます。

---

## 目次

1. [動作環境](#1-動作環境)
2. [ビルドと iPad へのインストール](#2-ビルドと-ipad-へのインストール)
3. [画面と設定項目](#3-画面と設定項目)
4. [再現の手順](#4-再現の手順)
5. [診断機能の見方](#5-診断機能の見方)
6. [ファイル構成](#6-ファイル構成)

---

## 1. 動作環境

| 項目           | 内容                                                                   |
| -------------- | ---------------------------------------------------------------------- |
| .NET           | .NET 10 SDK と iOS ワークロード（`dotnet workload install ios maui`）  |
| Xcode          | 使用する .NET for iOS が要求するバージョン（後述の注意を参照）         |
| Apryse SDK     | NuGet `PDFTron.iOS` / `PDFTron.iOS.Tools` **12.1.0**（バージョン固定） |
| 実行先         | **iPad の実機**（Apple Pencil はシミュレータで試せないため）           |
| ライセンスキー | 不要。（キーがない場合はデモモードで動作し、ページに透かしが入る）     |

**注意 1（Xcode のバージョン）**：.NET for iOS は、対応する Xcode のバージョンが決まっています。インストール済みの Xcode と合わない場合は、ビルドが次のエラーで止まります。

```
This version of .NET for iOS (...) requires Xcode XX.X. The current version of Xcode is YY.Y.
```

対応する Xcode を使うのが基本です。検証のために一時的に回避する場合は、`-p:ValidateXcodeVersion=false` を付けてビルドしてください（Apryse 側の検証はこの方法で行いました）。

**注意 2（シミュレータ）**：NuGet の `PDFTron.iOS` には実機用のバイナリのみが含まれているため、シミュレータ向けにはリンクできません。実機向け（`ios-arm64`）でビルドしてください。

---

## 2. ビルドと iPad へのインストール

### 2-1. 署名の設定

`Signing.props.example` をコピーして `Signing.props` を作り、Team ID と Bundle ID を設定します。

```sh
cd PencilReproMaui
cp Signing.props.example Signing.props
# Signing.props を編集
#   CodesignTeamId : 貴社の Apple Developer の Team ID
#   ApplicationId  : 貴社のチームで使える Bundle ID（例: com.example.pencilrepro.maui）
```

その Bundle ID 用のプロビジョニングプロファイルが必要です。お持ちでない場合は、Xcode で同じ Bundle ID の空のアプリを一度実機にビルドすると、自動署名でプロファイルが作られます。

### 2-2. ビルド

```sh
cd PencilReproMaui
dotnet build -f net10.0-ios -r ios-arm64
```

署名の証明書やプロファイルが自動で選ばれない場合は、明示的に指定してください。

```sh
dotnet build -f net10.0-ios -r ios-arm64 \
  -p:CodesignKey="Apple Development: <名前> (<ID>)" \
  -p:CodesignProvision="<プロファイル名>"
```

### 2-3. iPad へのインストールと起動

Visual Studio Code / Rider などから実機を選んで実行するか、コマンドラインで次のようにインストールします。

```sh
xcrun devicectl list devices                       # iPad の Identifier を確認
xcrun devicectl device install app --device <Identifier> \
  bin/Debug/net10.0-ios/ios-arm64/PencilReproMaui.app
```

初回は iPad の 設定 > 一般 > VPN とデバイス管理 で、開発者を「信頼」する必要があります。

**重要：アプリは iPad のホーム画面から手動で起動してください。** Apryse 側の検証では、Mac から `xcrun devicectl device process launch` で起動したプロセスに、**Apple Pencil のタッチが一切届かない**ことがありました（指のタッチは届きます）。起動し直すときも、アプリスイッチャーでアプリを終了させてから、ホーム画面のアイコンで起動してください。

### 2-4. Bluetooth のキーの有無を切り替える（任意）

Apryse SDK は、Apple Pencil がペアリングされているかを **CoreBluetooth** で調べています。iOS では、Info.plist に `NSBluetoothAlwaysUsageDescription` がないアプリは Bluetooth を使えないため、このキーの有無で SDK の動作が変わる可能性があります。

| ビルド                                                   | Info.plist のキー |
| -------------------------------------------------------- | ----------------- |
| `dotnet build ...`（既定）                               | **あり**          |
| `dotnet build ... -p:PencilReproBluetoothUsageKey=false` | **なし**          |

貴社アプリの Info.plist にこのキーがあるかどうかに合わせて切り替えてください。別の状態を試すときは、**アプリを iPad から削除してからインストール**してください。削除すると、Bluetooth の許可の状態も「未決定」に戻ります。

---

## 3. 画面と設定項目

### ホーム画面

- **ENVIRONMENT**：端末、OS、SDK のバージョン、OS の「Apple Pencil のみで描画」の状態、Bluetooth の状態（Info.plist のキーの有無と、`CBManager.authorization`）を表示します。
- **SETTINGS**：ビューアを開くときに反映される設定です（アプリを終了しても保持されます）。
- **OPEN**：「Open view-only viewer」で閲覧専用のビューアを開きます。「Export log」でログを書き出します。

### 設定項目（SETTINGS）

ご報告で変えて試された項目だけです。

| 項目                                  | 選択肢（**太字**が既定）                  | 対応する API                                                                                                                   |
| ------------------------------------- | ----------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------ |
| `toolManager.pencilInteractionMode`   | **PencilOnly** / System / FingerAndPencil | `PTToolManager.pencilInteractionMode`                                                                                          |
| `toolManager.readonly`                | **OFF** / ON                              | `PTToolManager.readonly`                                                                                                       |
| shouldSwitchToTool override           | **Off** / AlwaysNo                        | `-toolManager:shouldSwitchToTool:`。AlwaysNo = 常に NO を返す                                                                  |
| `toolManager.pencilTool`              | **SdkDefault** / FreeHand                 | `PTToolManager.pencilTool`。ご報告では `PTFreeHandCreate` ですが、SDK 12.1.0 の既定は `PTPencilDrawingCreate` のため両方を用意 |
| `setMinimumTwoFingersToScrollEnabled` | **NotCalled** / CallNo / CallYes          | `-[PTPDFViewCtrl setMinimumTwoFingersToScrollEnabled:]`（ご質問 5）                                                            |

### 固定している条件

| 条件                     | 内容                                                                                                                                                                                                         |
| ------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| 閲覧専用の設定           | ご報告の5行を常に適用（下記）                                                                                                                                                                                |
| `toolManager.tool`       | アプリからは一切変更せず、SDK の既定（`PTPanTool`）のまま                                                                                                                                                    |
| MAUI への埋め込み        | 公式の [maui-sample](https://github.com/ApryseSDK/maui-sample) と同じ構造（ViewHandler の UIView に `UINavigationController` のビューを追加）。そのページを全画面でモーダル表示                              |
| C# からのツールの制御    | `ToolManager.ShouldSwitchToTool` プロパティと `ToolManagerToolChanged` イベントを使用。MAUI のバインディングでは、このとき **`toolManager.delegate` が `PTDocumentController` からバインディング内部のオブジェクトに置き換わります** |
| サンプル PDF             | 12ページ、注釈なし。ビューアを開くたびに元のファイルをコピーし直すため、前回ペンで描いた注釈は残りません                                                                                                     |
| 診断用のオーバーレイ     | 常に表示（ビューアの「Overlay」ボタンで表示・非表示を切り替え可能）                                                                                                                                          |

閲覧専用の5行の設定（`Viewer/ReproDocumentController.cs`）は次のとおりです。

```csharp
ToolGroupsEnabled = false;            // controller.toolGroupsEnabled = NO;
SetToolGroupToolbarHidden(true, false);
HidesNavigationBar = false;
HidesBottomBar = false;
SetControlsHidden(true, false);
```

### ビューアの画面

ナビゲーションバーに診断用のボタンがあります。

| ボタン     | 動作                               |
| ---------- | ---------------------------------- |
| Close      | ビューアを閉じる                   |
| Dump state | その時点の状態を詳しくログに出す   |
| Overlay    | 診断用のオーバーレイの表示・非表示 |
| Export log | ログを書き出す                     |

---

## 4. 再現の手順

### 4-1. 手順

ご報告の再現手順と同じです。

1. iPad の 設定 > Apple Pencil > **「Apple Pencil のみで描画」を ON** にします。
2. アプリを**ホーム画面から**起動し、ホーム画面で設定を選びます（既定のままで構いません）。
3. 「**Open view-only viewer**」を押してビューアを開きます。
4. **指**で何度かスクロールし、正常に動くことを確認します。
5. **Apple Pencil** で、ページを何度かなぞります。
6. もう一度、**指**で1本指のスクロール、2本指のスクロール、ピンチズームを試します。
7. 「**Export log**」を押してログを書き出します。
8. 「Close」で閉じ、もう一度開いて、元に戻るかを確認します。

### 4-2. ご報告の「試した構成」との対応

| ご報告の構成                                    | 本サンプルの設定                                                                 |
| ----------------------------------------------- | -------------------------------------------------------------------------------- |
| `PencilOnly`                                    | pencilInteractionMode = PencilOnly                                               |
| `System`（OS 設定 ON）                          | pencilInteractionMode = System、iPad の「Apple Pencil のみで描画」= ON           |
| `System`（OS 設定 OFF）                         | pencilInteractionMode = System、iPad の「Apple Pencil のみで描画」= OFF          |
| `FingerAndPencil`                               | pencilInteractionMode = FingerAndPencil                                          |
| `System` ＋ `shouldSwitchToTool` で `NO` を返す | pencilInteractionMode = System、shouldSwitchToTool override = AlwaysNo           |
| `readonly` = `YES` / `NO`                       | readonly = ON / OFF（上のそれぞれと組み合わせ）                                  |
| ご質問 5                                        | setMinimumTwoFingersToScrollEnabled = CallNo                                     |

### 4-3. ログの取り出し

「Export log」を押すと共有シートが開き、ログ（`PencilReproMaui-log-<日時>.txt`）を AirDrop やメールで送れます。同じファイルがアプリの Documents にも保存されます。Mac から取り出す場合は、次のコマンドを使えます（Finder や「ファイル」アプリからも見られます）。

```sh
xcrun devicectl device copy from --device <Identifier> \
  --domain-type appDataContainer --domain-identifier <Bundle ID> \
  --source Documents --destination ./device-logs
```

---

## 5. 診断機能の見方

### オーバーレイ（ビューアの右上）

タッチを妨げない設定（`UserInteractionEnabled = false`）にしてあります。1秒ごとと、タッチが終わるたびに更新されます。

表示例（一部を省略しています）：

```
OK: no stuck recognizers             ← 症状を検出すると赤い「STUCK for ...」に変わる
tool: PTPanTool  pencilTool: PTPencilDrawingCreate
mode: PencilOnly  readonly: NO  pencilOnly: YES
delegate: NSObject                   ← toolManager.delegate（バインディング内部のオブジェクト）
BT: plistKey=YES auth=allowedAlways prompt=YES
PTPDFScrollView[0]: ScrollViewPinch:Possible ScrollViewPan:Possible ...
PTPDFViewCtrl: LongPress:Possible PTFastTap:Possible Tap:Possible ...
hitTest(center): ...  windows: 2
```

**症状の自動検出**：UIKit は、タッチの操作が終わると、すべてのジェスチャの状態を `Possible` に戻します。そのため、**タッチがない状態が 0.25秒以上続いているのに、`PTPDFScrollView` か `PTPDFViewCtrl` のジェスチャが `Possible` 以外のまま**のときに、オーバーレイを赤くして、ログに `STUCK DETECTED` と記録します。ご報告の「発生後」の状態（`Pan:Ended`、`Pinch:Failed` などから戻らない）は、これで検出できるはずです。戻った場合は `RECOVERED` と記録します。

### ログの種類

各行に、時刻と起動からの経過秒数が付いています。

| 分類      | 内容                                                                                                                                                                                                                                                                                                                                                         |
| --------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `[app]`   | 起動、SDK のバージョン、設定の反映、Bluetooth の状態など                                                                                                                                                                                                                                                                                                     |
| `[touch]` | 画面に届いたすべてのタッチ（指 / ペン、開始 / 最初の移動 / 終了 / キャンセル）と、**そのタッチを受け取ったジェスチャの一覧**。一覧が `[]` の場合は、どのジェスチャもタッチを受け取っていない（たとえば delegate が `-gestureRecognizer:shouldReceiveTouch:` で NO を返した）ことを意味します                                                                 |
| `[state]` | 状態の詳細（`toolManager.tool`、`pencilTool`、`pencilInteractionMode`、`readonly`、`annotationsCreatedWithPencilOnly`、delegate、各ジェスチャの class / state / enabled / delegate、`scrollEnabled` / `userInteractionEnabled` / `hidden`、ページ中央での `hitTest` の結果、ウィンドウの数、Bluetooth の状態）。開いた時点、「Dump state」を押したとき、状態が変わったときに出力します。変わらないときは1秒ごとに1行だけ出します |
| `[tool]`  | ツールの変更の通知、`shouldSwitchToTool` の呼び出しとその結果、C# のイベント                                                                                                                                                                                                                                                                                 |
| `[stuck]` | `STUCK DETECTED`（状態の詳細つき）、検出中に届いた指のタッチ、`RECOVERED`                                                                                                                                                                                                                                                                                    |
| `[ui]`    | ボタンの操作、ログの書き出しなど                                                                                                                                                                                                                                                                                                                             |

**ペンを使った後に見るべき点**

1. `[touch] ... pencil began / ended ... recognizers=[...]`：ペンのタッチを、どのジェスチャが受け取ったか。`pencil` の行が1つもない場合は、ペンのタッチがアプリに届いていません（2-3 の「重要」を参照）
2. `[tool]`：SDK がツールを切り替えたか（ご報告では `PTPanTool` のまま）
3. `[stuck] STUCK DETECTED`：どのジェスチャが `Possible` に戻らなかったか
4. その後の `[touch] ... finger began ... recognizers=[...]`：指のタッチが画面に届いているか。届いている場合、ジェスチャが受け取っているか（`[]` か、`Failed` / `Ended` のままか）

### タッチの記録のしかたについて

MAUI はアプリ自身の `UIWindow` を作るため、ウィンドウの `sendEvent` を上書きできません。そこで、ウィンドウに**観察専用のジェスチャ**を1つ追加して記録しています（ログやジェスチャの一覧では `UIWindow.TouchObserver` と表示されます）。このジェスチャは、何も認識せず、タッチのキャンセルや遅延もせず、ほかのすべてのジェスチャと同時に動くように設定してあり、タッチの配信には影響しないようにしています。

---


## 6. ファイル構成

```
PencilReproMaui-sample/
├── README.md                          
└── PencilReproMaui/
    ├── PencilReproMaui.csproj         プロジェクト（NuGet 12.1.0 固定、Bluetooth のキーの切り替え）
    ├── Signing.props.example          署名の設定のひな形（→ Signing.props にコピー）
    ├── MauiProgram.cs                 MAUI の初期化、ViewHandler の登録
    ├── Platforms/iOS/
    │   ├── AppDelegate.cs             PDFNet の初期化（ライセンスキーなし = デモモード）
    │   ├── Info.plist
    │   └── Bluetooth.plist            NSBluetoothAlwaysUsageDescription（ビルド時に合成）
    ├── Home/
    │   ├── HomePage.cs                ホーム画面（環境の表示、設定、ビューアを開く）
    │   └── ReproSettings.cs           設定項目と保存
    ├── Viewer/
    │   ├── ReproDocumentController.cs PTDocumentController のサブクラス（ご報告の構成。設定箇所に REPRO の印）
    │   ├── ViewerHost.cs              UINavigationController＋診断ボタン、サンプル PDF のコピー
    │   ├── ViewerControl.cs           ViewHandler での埋め込み（maui-sample と同じ構造）
    │   └── ViewerPage.cs              ビューアを全画面でモーダル表示する MAUI のページ
    ├── Diagnostics/                   診断機能（タッチの記録、状態のダンプ、症状の検出、ログの書き出し）
    └── Resources/Raw/
        └── PencilRepro-sample.pdf     サンプル PDF（12ページ、注釈なし。Apryse の API で生成）
```
