<#!
.SYNOPSIS
  単一実行ファイル(Self-contained + PublishSingleFile)版の ServiceWatcher をパッケージ化するスクリプト。
.DESCRIPTION
  - PublishProfile: Win64SingleFile を利用して publish
  - dist/ 配下に zip を生成
  - 既存のパッケージがある場合は --Force で上書き
  - バージョンは引数指定が無ければ ServiceWatcher.csproj の <Version> 要素から取得
.PARAMETER Version
  パッケージ化するバージョン。省略時は csproj から自動取得。
.PARAMETER Force
  既存の出力フォルダ / zip を強制的に削除して再生成。
.EXAMPLE
  pwsh scripts/package-single.ps1
.EXAMPLE
  pwsh scripts/package-single.ps1 -Version 1.2.3
.EXAMPLE
  pwsh scripts/package-single.ps1 -Force
.NOTES
  Windows PowerShell / PowerShell 7 両対応。
#>
param(
    [string]$Version,
    [switch]$Force,
    [switch]$SkipPublish
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Write-Info($msg) { Write-Host "[INFO] $msg" -ForegroundColor Cyan }
function Write-Warn($msg) { Write-Host "[WARN] $msg" -ForegroundColor Yellow }
function Write-Err($msg)  { Write-Host "[ERROR] $msg" -ForegroundColor Red }

# 1. ルート判定
$RepoRoot = Split-Path -Parent $PSCommandPath | Split-Path -Parent
Push-Location $RepoRoot

# 2. バージョン取得
if (-not $Version) {
    # まず AssemblyInfo.cs からバージョンを取得
    $assemblyInfo = Join-Path $RepoRoot 'Properties/AssemblyInfo.cs'
    if (Test-Path $assemblyInfo) {
        $content = Get-Content $assemblyInfo -Raw
        if ($content -match 'AssemblyVersion\("([^"]+)"\)') {
            $Version = $matches[1]
            Write-Info "Version: $Version (AssemblyInfo.cs から取得)"
        }
    }
    
    # AssemblyInfo.cs からバージョンが取得できなかった場合は csproj を確認
    if (-not $Version) {
        $csproj = Join-Path $RepoRoot 'ServiceWatcher.csproj'
        if (-not (Test-Path $csproj)) { Write-Err "ServiceWatcher.csproj が見つかりません"; exit 1 }
        $xml = [xml](Get-Content $csproj -Encoding UTF8)
        $Version = $xml.Project.PropertyGroup.Version
        if ($Version) {
            Write-Info "Version: $Version (csproj から取得)"
        }
    }
    
    if (-not $Version) { 
        Write-Err "バージョンが取得できません。AssemblyInfo.cs または csproj に定義するか、-Version で指定してください。"
        exit 1 
    }
} else {
    Write-Info "Version: $Version (引数指定)"
}

# 3. dotnet SDK チェック
$dotnet = (Get-Command dotnet -ErrorAction SilentlyContinue)
if (-not $dotnet) { Write-Err "dotnet コマンドが見つかりません。SDK をインストールしてください。"; exit 1 }
Write-Info "dotnet version: $(dotnet --version)"

# 4. PublishProfile 確認
$publishProfile = Join-Path $RepoRoot 'Properties/PublishProfiles/Win64SingleFile.pubxml'
if (-not (Test-Path $publishProfile)) {
    Write-Err "PublishProfile Win64SingleFile が存在しません。先に作成してください。"; exit 1 }

# 5. publish 実行 (必要なら)
if (-not $SkipPublish) {
    Write-Info 'Publishing (Win64SingleFile)...'
    dotnet publish -c Release -p:PublishProfile=Win64SingleFile | Write-Host
} else {
    Write-Warn 'SkipPublish が指定されたため publish をスキップします。'
}

$PublishDir = Join-Path $RepoRoot 'bin/Release/net8.0-windows/publish/win-x64-single'
if (-not (Test-Path $PublishDir)) { Write-Err "Publish 出力が見つかりません: $PublishDir"; exit 1 }

# 6. 出力準備
$DistRoot = Join-Path $RepoRoot 'dist'
if (-not (Test-Path $DistRoot)) { New-Item -ItemType Directory -Path $DistRoot | Out-Null }
$PackageName = "ServiceWatcher-v$Version-win-x64-single"
$PackageDir  = Join-Path $DistRoot $PackageName
$ZipPath     = "$PackageDir.zip"

if (Test-Path $PackageDir) {
    if ($Force) { Write-Warn "既存ディレクトリを削除: $PackageDir"; Remove-Item -Recurse -Force $PackageDir }
    else { Write-Err "既に存在します: $PackageDir (-Force で上書き)"; exit 1 }
}
if (Test-Path $ZipPath) {
    if ($Force) { Write-Warn "既存Zipを削除: $ZipPath"; Remove-Item -Force $ZipPath }
    else { Write-Err "既に存在します: $ZipPath (-Force で上書き)"; exit 1 }
}

New-Item -ItemType Directory -Path $PackageDir | Out-Null

# 7. ファイルコピー
Write-Info 'ファイルコピー'
$exe = Join-Path $PublishDir 'ServiceWatcher.exe'
if (-not (Test-Path $exe)) { Write-Err 'ServiceWatcher.exe が見つかりません'; exit 1 }
Copy-Item $exe -Destination $PackageDir

# 一緒に含めるドキュメント
$docs = @('README.md','CHANGELOG.md','PERFORMANCE.md')
foreach ($d in $docs) {
  if (Test-Path $d) { Copy-Item $d -Destination $PackageDir } else { Write-Warn "$d が見つかりません" }
}
# config.json をテンプレート名にして含める (存在すれば)
if (Test-Path 'config.json') { Copy-Item 'config.json' -Destination (Join-Path $PackageDir 'config.json.template') }
elseif (Test-Path 'config.template.json') { Copy-Item 'config.template.json' -Destination (Join-Path $PackageDir 'config.json.template') }
else { Write-Warn 'config.json / config.template.json が見つかりません' }

# ライセンスがあれば
if (Test-Path 'LICENSE') { Copy-Item 'LICENSE' -Destination $PackageDir }

# 8. Zip 化
Write-Info 'Zip 作成'
Compress-Archive -Path (Join-Path $PackageDir '*') -DestinationPath $ZipPath -Force

# 9. 要約表示
$exeSizeMB = [math]::Round((Get-Item $exe).Length / 1MB,2)
$zipSizeMB = [math]::Round((Get-Item $ZipPath).Length / 1MB,2)
Write-Host "================ SUMMARY ================" -ForegroundColor Green
Write-Host "Package Folder : $PackageDir" -ForegroundColor Green
Write-Host "Zip File       : $ZipPath" -ForegroundColor Green
Write-Host "EXE Size       : $exeSizeMB MB" -ForegroundColor Green
Write-Host "ZIP Size       : $zipSizeMB MB" -ForegroundColor Green
Write-Host "Docs Included  : $($docs -join ', ')" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green

Pop-Location
