<p align="center">
  <img src="assets/TokenMonitor.png" width="72" alt="Token Monitor">
</p>
<h1 align="center">Token Monitor</h1>
<p align="center"><strong>利用枠・残高・API 使用量を、ひと目で。</strong><br>Codex / ChatGPT Work、DeepSeek、OpenAI API に対応する Windows デスクトップ用モニター。</p>
<p align="center">
  <img src="https://img.shields.io/badge/Windows-10%20%2F%2011%20x64-0078D4" alt="Windows 10 / 11 x64">
  <a href="https://github.com/Ricki0613/TokenMonitor/releases/latest"><img src="https://img.shields.io/badge/version-3.2.1-10A37F" alt="Version 3.2.1"></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/license-MIT-blue" alt="MIT License"></a>
</p>
<p align="center">
  <a href="https://github.com/Ricki0613/TokenMonitor/releases/download/v3.2.1/TokenMonitor-v3.2.1-windows-x64.zip"><strong>Windows 版をダウンロード</strong></a> ·
  <a href="#はじめに">はじめに</a> ·
  <a href="docs/USER_GUIDE.zh-CN.md">利用ガイド（中国語）</a>
</p>

<p align="center">
  <a href="README.md">简体中文</a> · <a href="README.en.md">English</a> · <a href="README.ja.md">日本語</a> · <a href="README.es.md">Español</a> · <a href="README.fr.md">Français</a> · <a href="README.de.md">Deutsch</a> · <a href="README.ru.md">Русский</a>
</p>

[![Token Monitor](assets/readme-hero.png)](assets/readme-hero.png)

<p align="center"><sub>メイン画面とフローティングウィジェットの表示例 · サンプルデータ · クリックで高解像度画像を表示</sub></p>

## 使用量をいつも手元に

コーディング中やモデルの利用中も、3 つのサービスの状況をまとめて確認できます。

- **小さなフローティングウィジェット**：上段に 5 時間枠、下段に週間枠の残り割合を表示。ドラッグで移動、ダブルクリックで詳細を表示でき、サイズは 5 段階から選べます。
- **独立したフローティングウィンドウ**：サービスごとに表示・非表示、移動、サイズ変更が可能。非表示にしても、接続済みサービスの更新は続きます。
- **ローカル履歴**：直近 7 日／30 日のグラフを表示し、90 日分を保存。取得できなかったデータは補いません。
- **見やすさを調整**：ライト・ダーク・システム連動のテーマ、3 段階の文字サイズ、常設のサイドバー。
- **更新とロールバック**：アプリ内でダウンロードと検証を行い、確認後に再起動して更新。前のバージョンに戻すこともできます。

## 対応するデータ

| サービス | 確認できる内容 | 接続方法 |
| --- | --- | --- |
| **ChatGPT / Codex** | Codex / ChatGPT Work 共通の残り利用枠、リセット時刻、利用可能なリセット回数 | 同じ PC でログイン済みの Codex |
| **DeepSeek** | 利用可能残高、チャージ残高、付与残高を通貨別に表示 | API Key またはローカルの DeepSeek Harness ログイン状態 |
| **OpenAI API** | 指定 API Key ID の当日／当月 tokens、入力・出力・キャッシュ入力、リクエスト数 | 通常の API Key。履歴には Admin API Key と対応する Key ID も必要 |

ChatGPT のデータは Codex / ChatGPT Work の利用枠が対象で、通常のチャットにおける全モデルのメッセージ上限を網羅しません。OpenAI の集計範囲は公式 `usage/completions` エンドポイントです。キャッシュ入力は入力 tokens に含まれるため、重複加算しません。

## はじめに

**動作環境：Windows 10 / 11 x64、.NET Framework 4.8。アプリの画面は現在中国語です。** 実行に Python、Node、.NET SDK は不要です。

1. [Windows 用 ZIP をダウンロード](https://github.com/Ricki0613/TokenMonitor/releases/download/v3.2.1/TokenMonitor-v3.2.1-windows-x64.zip)し、すべて展開します。GitHub の `Source code` ではなく、実行用の ZIP を選んでください。
2. **Install.cmd** を実行し、アプリとデータの保存先を選択します。デスクトップとスタートメニューにショートカットが作成されます。展開した `TokenMonitor.exe` を直接実行することもできます。
3. 各カードの **连接设置**（接続設定）から必要なサービスを接続し、フローティング表示を有効にします。

自動更新は標準で **30 秒**。概要画面で 15 / 30 / 60 / 120 秒を選択でき、手動更新も可能です。メイン画面を閉じると通知領域に格納されます。通知領域のアイコンをダブルクリックすると再表示でき、メニューから完全に終了できます。

[インストールとトラブルシューティング](docs/START_HERE.zh-CN.md) · [詳しい利用ガイド](docs/USER_GUIDE.zh-CN.md)（いずれも中国語）

### DeepSeek：API Key または Harness

左の **DeepSeek 密钥** で API Key を入力するか、同じ Windows ユーザーで Harness にログインし、**DeepSeek 密钥 → DeepSeek 余额来源 → DeepSeek Harness 登录账户** を選択して保存します。Harness を起動し続ける必要はありません。

Harness モードではチャージ分と付与分を通貨別に合算します。標準のローカル保存先にある認証情報を必要時に読み取り、Token Monitor にはコピーしません。API Key モードとの切り替えが可能です。Harness の独自データ保存先や非公式サーバーには対応していません。[認証情報の取り扱い](SECURITY.md)も参照してください。

### OpenAI：キーの検証と履歴へのアクセス

通常の API Key を検証した後、組織の **Admin API Key** と対応する **API Key ID**（`key_…`）を入力します。通常キーの検証成功だけでは履歴へのアクセス権は得られません。設定が不足している場合は次の手順を表示し、使用量をゼロとは表示しません。

## 履歴・更新・データ保存

### 7 日／30 日の推移

時刻は北京時間（UTC+8）です。ChatGPT と DeepSeek は 15 分ごとに最後の正常取得値を保存します。ローカルの接続元ごとの記録なので、ログインアカウントを変更するとグラフに影響します。

OpenAI は Key ID のハッシュごとに日別使用量を保存し、取得のたびに当月分を修正します。過去の月は取得済みの値のみを保持し、当日分は暫定値です。

**残高の変化は支出額ではありません。** チャージ、付与、返金でも DeepSeek の残高は変化します。

### アプリ内更新

**关于软件**（アプリについて）で更新確認、変更内容の確認、ダウンロードができます。SHA-256 と実行ファイルのバージョンを検証した後、確認に応じて再起動・インストールします。設定、認証情報、履歴は保持され、同じ画面から前のバージョンへ戻せます。

起動時と正常な確認の 6 時間後に正式リリースを確認します。通信障害や GitHub のレート制限が発生しても使用量の監視は継続します。手動ダウンロードへのリンクもあります。v3.1.0 以前から初めて更新する際は、手動インストールが必要です。

[リリース一覧](https://github.com/Ricki0613/TokenMonitor/releases) · [変更履歴](CHANGELOG.md)

### データはローカルに保存

通常の実行やキーの保存に管理者権限は不要です。標準のデータ保存先は `%LOCALAPPDATA%\TokenMonitor`。**数据目录** で書き込み可能な空のフォルダーを選ぶと、設定・認証情報・履歴をコピーして検証し、保存先を切り替えます。元のフォルダーはバックアップとして残ります。

- DeepSeek と OpenAI のキーは Windows DPAPI で現在のユーザー向けに暗号化します。
- Harness の認証情報は必要時に読み取り、公式 DeepSeek サービスにのみ送信します。
- 使用量の確認でモデルへの推論リクエストは行いません。公式の利用枠リセットには別途確認が必要です。
- 更新確認時にアカウント使用量、認証情報、ローカル設定を GitHub に送信しません。

詳細は [SECURITY.md](SECURITY.md) を参照してください。集計にはサービス側の遅延が生じる場合があります。自動起動設定はなく、公開プログラムは現在コード署名されていません。

## 開発と貢献

[不具合報告・機能提案](https://github.com/Ricki0613/TokenMonitor/issues/new/choose)や Pull Request を歓迎します。スクリーンショット、ログ、コミットに実際の認証情報やアカウントデータを含めないでください。[貢献ガイド](CONTRIBUTING.md)を参照してください。

<details>
<summary>ソースからのビルドと検証</summary>

```powershell
git clone https://github.com/Ricki0613/TokenMonitor.git
cd TokenMonitor
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\build.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\test.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\test-ui.ps1
```

出力先は `build/`。.NET Framework C# コンパイラーを使用します。オフラインテストはサンプルデータを使用し、実際のアカウントやキーは不要です。`scripts/package.ps1` でパッケージを生成し、`scripts/test-package.ps1 -Archive <ZIP>` でインストールを確認できます。

README の画像は `scripts/render-readme.ps1` により、ネイティブ WPF コントロールとサンプルデータから 2 倍の解像度で生成しています。更新通知を有効にするには、実行用 ZIP を含む正式リリースを公開し Latest に設定します。Git タグのみでは通知されません。

</details>

## ライセンス

[MIT License](LICENSE) · Copyright (c) 2026 Ricki0613

本プロジェクトは独立したツールであり、OpenAI および DeepSeek との公式な提携関係はありません。
