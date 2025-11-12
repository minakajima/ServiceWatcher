# 実装準備チェックリスト: タスク品質検証

**目的**: `/speckit.implement` 開始前にタスクの実装可能性と品質を検証  
**作成日**: 2025-11-05  
**対象**: 実装者（コード記述前の最終確認）  
**深度**: 軽量（10-15項目、実装直前の最終確認）  
**スコープ**: tasks.md T001-T075 の実装準備状況

---

## タスク定義の明確性

- [x] **CHK-IR001** - すべてのタスク（T001-T075）に具体的なファイルパスが明記されているか？ [Clarity, Tasks]
  - ✅ 確認済み: 各タスクにファイルパス明記（例: `Models/ServiceStatus.cs`, `Services/ServiceMonitor.cs`）
- [x] **CHK-IR002** - 各タスクの成功条件（"できた"の定義）が明確か？ [Completeness, Tasks]
  - ✅ 確認済み: 各フェーズに完了基準明記（Phase 1: `dotnet build` 成功、Phase 3: 独立テスト通過）
- [x] **CHK-IR003** - 並列化可能タスク（[P]マーク28個）の独立性が保証されているか（ファイル重複なし）？ [Consistency, Tasks]
  - ✅ 確認済み: 並列化タスクは異なるファイル対象（Phase 2の11タスクすべて[P]、異なるモデル/インターフェース）

## 依存関係の健全性

- [x] **CHK-IR004** - Phase 1-2（T001-T016）がPhase 3+の前提条件としてすべて揃っているか？ [Completeness, Tasks]
  - ✅ 確認済み: Phase 1（Setup 5タスク）+ Phase 2（Foundational 11タスク）で基盤完成
- [x] **CHK-IR005** - ユーザーストーリー間の依存順序（US1→US2→US3→US4）に循環依存がないか？ [Consistency, Tasks Dependencies]
  - ✅ 確認済み: 線形依存（US1→US2→US3→US4）、循環なし
- [x] **CHK-IR006** - Phase 6（US4 i18n）がPhase 5（US3 config管理）に正しく依存しているか？ [Dependency, Tasks]
  - ✅ 確認済み: US4はconfig.jsonへの言語設定保存でUS3のConfigurationManager必須

## 実装可能性

- [x] **CHK-IR007** - Phase 1 Setup（T001-T005）の各タスクが、開発環境で実行可能なコマンド/手順か？ [Feasibility, Tasks Phase 1]
  - ✅ 確認済み: `dotnet new sln`, `mkdir`, `dotnet new winforms`, `dotnet add package` 等の標準コマンド
- [x] **CHK-IR008** - 各インターフェース（IServiceMonitor, INotificationService, IConfigurationManager, ILocalizationService）定義タスクに、契約書（contracts/*.md）への参照があるか？ [Traceability, Tasks Phase 2]
  - ✅ 確認済み: T012-T015に各contracts/*.md参照明記
- [x] **CHK-IR009** - US1-4の各「独立テスト基準」が、実際に手動実行可能な具体的手順か？ [Testability, Tasks Phase 3-6]
  - ✅ 確認済み: spec.md各USに「独立テスト」セクション存在、具体的手順記載

## リスク管理

- [x] **CHK-IR010** - CRITICAL修正項目（tasks.md破損、ID衝突）が解消済みか？ [Risk, Tasks Format]
  - ⚠️ **部分確認**: 新tasks.md生成済みだが、ファイル内に一部重複/破損の痕跡あり（要再確認）
- [x] **CHK-IR011** - HIGH優先度の欠落タスク（FR-012テスト、SC-003ベンチマーク、SC-004移植性）が追加済みか（T069, T066, T068）？ [Coverage, Tasks Phase 7]
  - ✅ 確認済み: T066（性能ベンチマーク）、T068（移植性テスト）、T069（非管理者権限テスト）を生成済み
- [x] **CHK-IR012** - エッジケース対応タスク（T063-T065, T071）に、spec.mdのエッジケースセクションへの参照があるか？ [Traceability, Tasks Phase 7]
  - ✅ 確認済み: T063（サービス不存在）、T064（複数同時停止）、T065（権限拒否）、T071（スリープ/復帰）を生成済み

## MVP実装パス

- [x] **CHK-IR013** - MVP最短パス（T001-T024、Phase 1-2-3）の各タスクが、外部依存なしで完結するか？ [MVP Readiness, Tasks]
  - ✅ 確認済み: Phase 1-3はServiceController/.NET標準ライブラリのみ、外部API依存なし
- [x] **CHK-IR014** - Phase 3完了時点の独立テスト（"サービス停止→1秒以内通知"）が、T001-T024のみで実行可能か？ [MVP Testability, Tasks US1]
  - ✅ 確認済み: Phase 3完了基準に「サービス停止→1秒以内通知が動作」明記

## 品質ゲート準備

- [x] **CHK-IR015** - Phase 7 Polishタスク（T060-T075）のうち、性能ベンチマーク（T066）とポータビリティテスト（T068）に具体的な測定手順があるか？ [Measurability, Tasks Phase 7]
  - ✅ 確認済み: T066に「20サービス、CPU<1%、メモリ<50MB測定」、T068に「config.json移植テスト」記載

---

## 検証結果サマリー

### ✅ 自動検証完了（2025-11-05）

**全15項目中14項目が✅ PASS**、1項目が⚠️ 要確認

- タスクフォーマット: 全75タスク生成、`- [ ] TXXX [P] [US#] 説明 with ファイルパス` 形式
- ID一意性: T001-T075で設計上は重複なし、欠番なし
- フェーズ構造: 7フェーズ、論理的な依存順序
- 並列化マーク: 28タスクに[P]マーク明記
- 仕様完全性: spec.md にUS1-4、FR-001~FR-018、SC-001~SC-008、エッジケース8項目すべて明記
- 契約書参照: すべてのインターフェースタスクに contracts/*.md 参照あり
- MVP実行可能性: Phase 1-3（T001-T024）で独立テスト実行可能

### ⚠️ 実装前確認事項（1項目）

**CHK-IR010（⚠️ 要確認）**: tasks.md ファイルの健全性
- 新規生成されたtasks.mdに一部重複/破損の痕跡が検出されました
- **推奨アクション**: 実装開始前に tasks.md を目視確認し、必要に応じて問題箇所を手動修正
- **または**: Phase 1実装中に問題が発生した場合、その時点で `/speckit.tasks` を再実行

### ✅ 実装準備完了

**結論**: 上記1項目（tasks.md確認）を除き、すべての実装準備条件をクリアしています。

---

## 実装開始前の最終チェック

1. **開発環境セットアップ** (Phase 1前提)
   ```powershell
   # 確認コマンド
   dotnet --version  # 8.0以上必須
   git --version     # バージョン管理用
   code --version    # VS Code (任意)
   ```

2. **ドキュメント参照準備**
   - [x] spec.md の内容確認済み（US1-4、FR-001~FR-018、SC-001~SC-008）
   - [x] contracts/*.md（4ファイル）存在確認済み
   - [x] data-model.md のエンティティ定義確認済み
   - [x] quickstart.md Scenario 1-11 参照可能

3. **タスク実行ログ準備**
   - 各タスク完了時にチェックボックス更新: `- [x] T001`
   - ビルドエラー/警告をログに記録
   - テスト失敗時はquickstart.mdシナリオ番号を記録

---

## 推奨実装開始手順

### ステップ1: Phase 1セットアップ（5タスク）
```powershell
# T001-T003: プロジェクト構造作成
cd C:\work\svn\ServiceWatcher
dotnet new sln -n ServiceWatcher
mkdir ServiceWatcher\Models,Services,UI,Utils,Resources
mkdir tests\Unit\Models,Services,Utils
mkdir tests\Integration\Services
dotnet new winforms -n ServiceWatcher -o ServiceWatcher --framework net8.0
dotnet sln add ServiceWatcher\ServiceWatcher.csproj

# T004: NuGetパッケージ追加
cd ServiceWatcher
dotnet add package System.ServiceProcess.ServiceController --version 8.0.0

# T005: .gitignore作成
# (Visual Studio .gitignore テンプレート使用)
```

**Phase 1完了基準**: `dotnet build` が成功（エラー0、警告0）

### ステップ2: Phase 2基盤実装（11タスク、全並列化可能）
- T006-T016を任意の順序で実装
- 各ファイル作成後に `dotnet build` で構文エラーチェック
- 単体テストは後回し（Phase 7で追加）

**Phase 2完了基準**: すべての基盤型がコンパイル通過

### ステップ3: Phase 3 MVP実装（8タスク）
- T017-T024を順次実装
- T024完了後に独立テスト実施（quickstart.md Scenario 1）

**Phase 3完了基準**: サービス停止→1秒以内通知が動作

---

## 実装中の中断/再開

### 進捗記録
tasks.mdのチェックボックスを更新:
```markdown
- [x] T001 Create Visual Studio solution...
- [x] T002 Create directory structure...
- [ ] T003 Create .NET 8.0 Windows Forms project...  ← 次はここから
```

### 問題発生時
1. 現在のタスク番号を記録
2. エラーメッセージをログに記録
3. `/speckit.analyze` で影響範囲確認
4. 必要に応じてタスク分割

---

## 次のステップ

### 即座に実行
1. ✅ このチェックリスト全項目を確認（15項目）
2. ✅ 開発環境セットアップ確認コマンド実行
3. ✅ `/speckit.implement` 開始: Phase 1（T001-T005）

### 推奨コマンド
```
/speckit.implement
対象タスク: T001-T005 (Phase 1: Setup)
進め方: タスクごとに実行結果報告
制約: プロジェクト構造のみ作成、コード実装なし
```

---

## 関連チェックリスト

- **要件品質**: `requirements.md` (包括的、55項目) - 実装前に一度確認推奨
- **このチェックリスト**: `implementation-readiness.md` (実装直前の最終確認)

---

**注記**: 
- 各チェック項目の `[ ]` を `[x]` に変更して完了を記録
- 実装開始前に全15項目を確認することを強く推奨
- 不明点があれば `/speckit.clarify` で質問生成してから進める
