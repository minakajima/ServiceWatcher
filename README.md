# ServiceWatcher

Windowsサービスの状態を監視し、サービス停止時に即座にポップアップ通知を表示するデスクトップアプリケーションです。

## 🌟 主な機能

- **リアルタイム監視**: 登録されたWindowsサービスの状態を定期的に監視
- **即座の通知**: サービス停止を検知したら1秒以内にポップアップ通知を表示
- **柔軟な設定**: JSON設定ファイルで監視対象サービスや監視間隔を管理
- **軽量**: メモリ使用量50MB以下、CPU使用率1%未満
- **使いやすいUI**: サービス一覧表示、検索、追加/削除機能

## 📋 システム要件

- **OS**: Windows 10 (21H2以降)、Windows 11、Windows Server 2016以降
- **.NET**: .NET 8.0 Runtime
- **権限**: 通常ユーザー権限で動作（一部システムサービスの監視には管理者権限が必要）

## 🚀 インストール

### 前提条件

.NET 8.0 Runtimeがインストールされていることを確認してください。

```powershell
dotnet --version
```

.NET 8.0がインストールされていない場合は、以下からダウンロードしてください:  
https://dotnet.microsoft.com/download/dotnet/8.0

### インストール手順

1. リリースページから最新版のzipファイルをダウンロード
2. 任意のフォルダに解凍
3. `ServiceWatcher.exe`を実行

## 📖 使い方

### 初回起動

1. `ServiceWatcher.exe`を起動
2. 初回起動時に`config.json`が自動的に作成されます
3. 「サービス管理」ボタンをクリックして監視対象サービスを追加

### サービスの追加

1. メイン画面の「サービス管理...」ボタンをクリック
2. サービス一覧から監視したいサービスを選択
3. 「監視対象に追加」ボタンをクリック
4. 「閉じる」ボタンで戻る

### 監視の開始

1. メイン画面の「監視開始」ボタンをクリック
2. ステータスバーに「監視中 - X個のサービス」と表示されます
3. サービスが停止すると、ポップアップ通知が表示されます

### 設定の変更

1. メイン画面の「設定...」ボタンをクリック
2. 監視間隔（1-3600秒）や通知表示時間（0-300秒）を変更
3. 「保存」ボタンをクリック
4. アプリケーションを再起動すると設定が反映されます

## ⚙️ 設定ファイル

`config.json`ファイルで詳細な設定が可能です。ファイルの場所:

- **実行時**: アプリケーションと同じフォルダ
- **開発時**: プロジェクトルート

### 設定項目

- `monitoringIntervalSeconds`: 監視間隔（秒）、1-3600の範囲
- `notificationDisplayTimeSeconds`: 通知の表示時間（秒）、0=手動で閉じるまで表示
- `startMinimized`: 起動時に最小化するかどうか
- `autoStartMonitoring`: 起動時に自動的に監視を開始するかどうか
- `monitoredServices`: 監視対象サービスのリスト

詳細は [config.json のサンプル](config.json) を参照してください。

## ⌨️ キーボードショートカット

- `F5`: サービス一覧を更新
- `Esc`: 監視中の場合、監視を停止

## 📊 ログファイル

ログファイルの場所:
```
%LOCALAPPDATA%\ServiceWatcher\logs\
```

ログファイルは日次でローテーションされ、10MBを超えると自動的に分割されます（最大10ファイル保持）。

## 🔧 トラブルシューティング

### サービスが監視できない

- **原因**: 管理者権限が必要なサービス
- **解決策**: アプリケーションを右クリックして「管理者として実行」

### 設定ファイルが破損した

- **自動復元**: アプリケーション起動時にバックアップから復元を提案します
- **手動復元**: `config.json.bak`を`config.json`にリネーム
- **初期化**: `config.json`を削除して再起動すると、デフォルト設定で再作成されます

### 通知が表示されない

1. 監視が開始されているか確認（ステータスバーに「監視中」と表示）
2. サービスの`notificationEnabled`が`true`になっているか確認
3. ログファイルでエラーメッセージを確認

## 🏗️ 開発者向け情報

### ビルド方法

```powershell
# クローン
git clone <repository-url>
cd ServiceWatcher

# ビルド
dotnet build

# 実行
dotnet run

# リリースビルド
dotnet publish -c Release -r win-x64 --self-contained
```

### テスト実行

#### すべてのテストを実行

```powershell
# テストプロジェクトのビルドと実行
dotnet test

# または、テストプロジェクトを指定して実行
dotnet test tests\ServiceWatcher.Tests.csproj
```

#### 詳細な出力で実行

```powershell
# 通常の詳細レベル
dotnet test --verbosity normal

# 最小限の出力
dotnet test --verbosity minimal

# 詳細な出力
dotnet test --verbosity detailed
```

#### 特定のテストクラスのみ実行

```powershell
# フィルターを使用
dotnet test --filter FullyQualifiedName~MonitoredServiceTests
dotnet test --filter FullyQualifiedName~ValidationResultTests
```

#### カバレッジレポート付きで実行

```powershell
# coverletパッケージを使用（別途インストール必要）
dotnet test --collect:"XPlat Code Coverage"
```

#### テスト構成

テストプロジェクトには以下のテストが含まれています:

- **MonitoredServiceTests** (10テスト): MonitoredServiceクラスの検証ロジックテスト
- **ServiceStatusChangeTests** (4テスト, 14実行): サービス状態変更イベントのテスト
- **ServiceStatusTests** (2テスト, 5実行): ServiceStatus列挙型のテスト
- **ResultTests** (10テスト, 14実行): Result<T>パターンのテスト
- **ValidationResultTests** (12テスト, 18実行): ValidationResultクラスのテスト

**合計**: 38テストメソッド、51テスト実行

### アーキテクチャ

```
ServiceWatcher/
├── Models/          # データモデル
├── Services/        # ビジネスロジック
├── UI/              # ユーザーインターフェース
└── Utils/           # ユーティリティ
```

### 使用技術

- **言語**: C# 12
- **フレームワーク**: .NET 8.0
- **UI**: Windows Forms
- **API**: System.ServiceProcess.ServiceController
- **シリアライズ**: System.Text.Json

## 📄 開発ガイドライン

本プロジェクトの開発原則は `.specify/memory/constitution.md` で定義されています。
すべての機能開発は憲章の原則に従う必要があります。

## 📝 バージョン履歴

詳細は [CHANGELOG.md](CHANGELOG.md) を参照してください。

## 📧 サポート

- バグ報告: GitHub Issues
- パフォーマンステスト: [PERFORMANCE.md](PERFORMANCE.md)

## ⚖️ 免責事項

このアプリケーションは、Windowsサービスの監視のみを行い、サービスの起動・停止・設定変更は行いません。

## ライセンス

TBD
