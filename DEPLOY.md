# ServiceWatcher - Deployment Guide

このドキュメントでは、ServiceWatcherのビルド、テスト、デプロイ手順を説明します。

## ビルド環境

- **SDK**: .NET 8.0 SDK
- **OS**: Windows 10/11 (ビルドにはWindows環境が必要)
- **IDE**: Visual Studio 2022またはVS Code（推奨）

## ビルドコマンド

### 開発ビルド

```powershell
# Debug ビルド
dotnet build

# Release ビルド
dotnet build -c Release
```

### パブリッシュ（配布用）

#### 方法1: フォルダ形式（複数ファイル）

```powershell
dotnet publish -c Release -r win-x64 --self-contained -p:PublishProfile=Win64SelfContained
```

出力先: `bin\Release\net8.0-windows\publish\win-x64\`

#### 方法2: 単一実行ファイル

```powershell
dotnet publish -c Release -r win-x64 --self-contained -p:PublishProfile=Win64SingleFile
```

出力先: `bin\Release\net8.0-windows\publish\win-x64-single\`

**メリット**:
- 単一の.exeファイルで配布可能
- インストール不要

**デメリット**:
- ファイルサイズが大きい（約70-100MB）
- 初回起動が若干遅い（展開処理のため）

## テスト

### T143: 複数Windows環境でのテスト

#### テスト環境

1. **Windows 10 (21H2以降)**
   ```powershell
   winver
   # → バージョン 21H2以降であることを確認
   ```

2. **Windows 11**
   ```powershell
   winver
   # → Windows 11であることを確認
   ```

3. **Windows Server 2016以降**
   ```powershell
   winver
   # → Windows Server 2016以降であることを確認
   ```

#### テスト項目

各環境で以下を確認:

- [ ] アプリケーションが起動する
- [ ] サービス一覧が表示される
- [ ] サービスを監視対象に追加できる
- [ ] 監視を開始できる
- [ ] サービスを手動で停止したときに通知が表示される
- [ ] 設定画面が開いて設定を変更できる
- [ ] アプリケーションを再起動しても設定が保存されている
- [ ] ログファイルが `%LOCALAPPDATA%\ServiceWatcher\logs\` に作成される
- [ ] ウィンドウのサイズと位置が次回起動時に復元される

#### 管理者権限のテスト

一部のシステムサービスは管理者権限が必要です:

```powershell
# 管理者として実行
# PowerShellを右クリック → 管理者として実行

cd "path\to\ServiceWatcher"
.\ServiceWatcher.exe
```

- [ ] 管理者として実行すると、システムサービスも監視できる

### パフォーマンステスト

[PERFORMANCE.md](PERFORMANCE.md)を参照してテストを実行:

- [ ] T129: メモリ使用量（50サービス） < 50MB
- [ ] T130: CPU使用率（20サービス） < 1%
- [ ] T131: 通知表示時間 < 1秒
- [ ] T132: 24時間連続稼働

## T144: Zipパッケージの作成

### 手動パッケージング

```powershell
# 1. パブリッシュ
dotnet publish -c Release -r win-x64 --self-contained -p:PublishProfile=Win64SelfContained

# 2. 配布用フォルダを作成
$version = "1.0.0"
$packageDir = "ServiceWatcher-v$version-win-x64"
New-Item -ItemType Directory -Path $packageDir -Force

# 3. 必要なファイルをコピー
Copy-Item "bin\Release\net8.0-windows\publish\win-x64\*" -Destination $packageDir -Recurse
Copy-Item "README.md" -Destination $packageDir
Copy-Item "CHANGELOG.md" -Destination $packageDir
Copy-Item "PERFORMANCE.md" -Destination $packageDir
Copy-Item "config.json" -Destination "$packageDir\config.json.template"

# 4. Zipファイルを作成
Compress-Archive -Path $packageDir -DestinationPath "$packageDir.zip" -Force

Write-Host "Package created: $packageDir.zip"
```

### パッケージ内容

```
ServiceWatcher-v1.0.0-win-x64.zip
├── ServiceWatcher.exe
├── ServiceWatcher.dll
├── (その他のDLLファイル)
├── config.json.template
├── README.md
├── CHANGELOG.md
└── PERFORMANCE.md
```

### 単一実行ファイル版のパッケージング

```powershell
# 1. パブリッシュ
dotnet publish -c Release -r win-x64 --self-contained -p:PublishProfile=Win64SingleFile

# 2. 配布用フォルダを作成
$version = "1.0.0"
$packageDir = "ServiceWatcher-v$version-win-x64-single"
New-Item -ItemType Directory -Path $packageDir -Force

# 3. 必要なファイルをコピー
Copy-Item "bin\Release\net8.0-windows\publish\win-x64-single\ServiceWatcher.exe" -Destination $packageDir
Copy-Item "README.md" -Destination $packageDir
Copy-Item "CHANGELOG.md" -Destination $packageDir
Copy-Item "PERFORMANCE.md" -Destination $packageDir
Copy-Item "config.json" -Destination "$packageDir\config.json.template"

# 4. Zipファイルを作成
Compress-Archive -Path $packageDir -DestinationPath "$packageDir.zip" -Force

Write-Host "Package created: $packageDir.zip"
```

## リリースチェックリスト

リリース前に以下を確認:

### コード品質
- [ ] すべてのビルド警告が解決されている
- [ ] README.mdが最新である
- [ ] CHANGELOG.mdが更新されている
- [ ] バージョン番号が正しい（ServiceWatcher.csproj）

### 機能テスト
- [ ] すべての機能が動作する
- [ ] Windows 10, 11, Server 2016+でテスト済み
- [ ] 管理者権限の有無で動作を確認
- [ ] エラーハンドリングが適切に機能する

### パフォーマンス
- [ ] メモリ使用量が要件を満たす (<50MB)
- [ ] CPU使用率が要件を満たす (<1%)
- [ ] 通知表示が1秒以内
- [ ] 24時間連続稼働テスト完了

### ドキュメント
- [ ] README.mdにインストール手順がある
- [ ] トラブルシューティングセクションが充実している
- [ ] CHANGELOGが更新されている
- [ ] ライセンス情報が明記されている

### パッケージング
- [ ] フォルダ版zipファイルが作成されている
- [ ] 単一ファイル版zipファイルが作成されている
- [ ] config.json.templateが含まれている
- [ ] ドキュメントが含まれている

## トラブルシューティング

### ビルドエラー: "SDK not found"

```powershell
# .NET 8.0 SDKをインストール
winget install Microsoft.DotNet.SDK.8
```

### パブリッシュが失敗する

```powershell
# クリーンビルド
dotnet clean
dotnet build -c Release
dotnet publish -c Release -r win-x64 --self-contained
```

### 単一ファイルパブリッシュが大きすぎる

ReadyToRunを無効化してサイズを削減:

```xml
<PublishReadyToRun>false</PublishReadyToRun>
```

または、Trimmingを有効化（注意: 一部機能が動作しなくなる可能性）:

```xml
<PublishTrimmed>true</PublishTrimmed>
```

## CI/CD（今後の拡張）

GitHub Actionsを使用した自動ビルド・リリースの例:

```yaml
# .github/workflows/release.yml
name: Release

on:
  push:
    tags:
      - 'v*'

jobs:
  build:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'
      - run: dotnet publish -c Release -r win-x64 --self-contained
      - uses: actions/upload-artifact@v3
        with:
          name: ServiceWatcher-win-x64
          path: bin/Release/net8.0-windows/publish/win-x64/
```

## サポート

ビルドやデプロイに関する問題は、GitHubのIssuesで報告してください。
