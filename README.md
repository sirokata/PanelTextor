# PanelTextor

[日本語](README.md) | [English](README.en.md) | [简体中文](README.zh-CN.md) | [한국어](README.ko.md)

![PanelTextor](Assets/PanelTextor.png)

Windows 11向けのローカル多言語テキスト配置ツールです。

Copyright (c) 2026 **sirokata** · 独自コードは [MIT License](LICENSE)

## ダウンロード・起動

1. このリポジトリの **Releases** からWindows x64用ZIPをダウンロードします。
2. ZIPを展開して `PanelTextor.exe` を起動します。.NETの別途インストールは不要です。
3. 初回に利用条件と第三者ライセンスを確認し、同意すると編集画面が開きます。同意しない場合は終了します。
4. 「画像を追加」で画像を読み込み、文章を配置します。

詳しくは **[使い方（USAGE.md）](USAGE.md)** を参照してください。

exeには必要な実行ライブラリとライセンスを含みます。exe単体でも動作しますが、配布には説明書付きZIPを用意しています。起動時に実行用ファイルがWindowsの一時領域へ展開されます。

## 主な機能

- JP / EN / CN / KRごとに文章・位置・サイズ・色を管理
- 日本語縦書き、主要な約物、半角記号・数字の自動縦中横
- 可変フォントのウェイト指定（Regular / Medium / Bold等）
- 空行で文章を分割して追加、ドラッグ移動、複数選択・Delete削除
- 同じファイル名を対応付けた複数背景セットと不一致チェック
- 複数言語×背景セットへの一括書き出し、進捗表示、完了通知
- PNG / JPEG品質100、sRGB変換・ICC付与
- ページ別の撮影フレーム・経過時間・5段階バッテリー
- 日本語・英語・簡体字中国語・韓国語の画面表示

## 保存・移行

- プロジェクト：`作品.paneltextor.json`。画像はパスで参照します。プロジェクトと画像フォルダーを一緒に保管してください。
- 旧 `.polytext.json` も開けます。保存時に新しい名前を提案し、元ファイルは自動削除しません。
- 設定：`%LOCALAPPDATA%\PanelTextor\settings.json`
- 旧 `%LOCALAPPDATA%\PolyText\settings.json` は、新しい設定がない場合に自動コピーします。旧設定は残します。
- 同意記録：`%LOCALAPPDATA%\PanelTextor\terms-acceptance.txt`。利用条件・原文が変わると再同意を求めます。

## 環境・制限

Windows 11 x64用です。フォントは同梱しません。使用するフォントをPCにインストールしてください。独自の画像アップロード・アクセス解析機能はありません。

他の画像編集ソフトとのピクセル一致、ルビ、禁則処理、自動折り返し、Undo/Redoは未対応です。JPEG品質100も可逆圧縮ではありません。無劣化保存にはPNGを使用してください。

## ソースからビルド

Windowsと.NET 10 SDKが必要です。

```powershell
dotnet restore PanelTextor.csproj
dotnet build PanelTextor.csproj -c Release
dotnet publish PanelTextor.csproj -c Release -r win-x64 --self-contained true -o dist/PanelTextor
```

配布ZIPを作成：

```powershell
powershell -ExecutionPolicy Bypass -File scripts/package.ps1
```

生成先は `release/`。実行ファイル・ユーザー向け文書・必要なライセンスを同梱します。

```powershell
dist/PanelTextor/PanelTextor.exe --self-test test-output
```

テストには日本語・中国語・韓国語フォントが必要です。可変フォント検証にはSource Han Sans CN VFをインストールしてください。テストは専用フォルダーの設定を使用します。

## ライセンス・再配布

PanelTextor独自コードは **MIT** です。無料・商用利用、改変、再配布が可能です。著作権表示と許諾文を保持してください。

.NET/WPF等の同梱部品には各権利者の条件が適用され、MITへ変更されません。[利用条件](TERMS.md)・[第三者通知](THIRD_PARTY_NOTICES.md)と `licenses/` を参照してください。配布版は第三者条件を含めた初回同意を求めます。改変・再配布時も必要な通知と同意の仕組みを保持してください。

不具合報告にはバージョン、Windowsのバージョン、フォント、再現手順を添えてください。非公開の作品や個人情報を公開Issueへ添付しないでください。


