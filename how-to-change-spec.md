# Speckit 仕様変更ガイド

Speckitで仕様変更を行う場合の標準的な手順を説明します。

## 仕様変更の2つのパターン

### パターン1: 既存機能の変更・拡張

既存のフィーチャー（例: `001-service-monitor`）に変更を加える場合:

```powershell
# 1. 新しいブランチを作成
git checkout main
git pull origin main  # 最新の状態を取得
git checkout -b 001-service-monitor-v2

# 2. 仕様書を更新
# specs/001-service-monitor/spec.md を編集して変更内容を追記
# 例: 新しいユーザーストーリー(US4)を追加

# 3. チェックリストで要件を確認
# .github/prompts/speckit.checklist.prompt.md の指示に従って
# specs/001-service-monitor/checklists/requirements.md を更新

# 4. プランを更新
# .github/prompts/speckit.plan.prompt.md の指示に従って
# specs/001-service-monitor/plan.md を更新
# - 新しいフェーズ(Phase 7)を追加
# - アーキテクチャへの影響を記載

# 5. タスク分解
# .github/prompts/speckit.tasks.prompt.md の指示に従って
# specs/001-service-monitor/tasks.md に新タスク(T146以降)を追加

# 6. 実装
# .github/prompts/speckit.implement.prompt.md の指示に従って実装

# 7. コミット
git add .
git commit -m "feat: 変更内容のサマリー (T146-T150)"

# 8. マージ
git checkout main
git merge 001-service-monitor-v2 --no-ff -m "Merge: 変更内容の詳細"

# 9. タグ作成（バージョンアップの場合）
git tag -a v1.1.0 -m "Release version 1.1.0 - 変更内容"

# 10. プッシュ
git push origin main
git push origin v1.1.0
```

### パターン2: 新機能の追加

全く新しい機能を別フィーチャーとして追加する場合:

```powershell
# 1. 新機能用のディレクトリとブランチを作成
.\.specify\scripts\powershell\create-new-feature.ps1 -FeatureId "002-email-notification"

# これにより以下が自動作成される:
# - specs/002-email-notification/ (テンプレートから)
# - ブランチ: 002-email-notification

# 2. 要件を明確化
# GitHub Copilot Chatで以下を実行:
# 「@workspace .github/prompts/speckit.clarify.prompt.md の指示に従って、
#  メール通知機能の要件を整理してください」

# 3. 仕様書を作成
# 「.github/prompts/speckit.specify.prompt.md の指示に従って、
#  specs/002-email-notification/spec.md を完成させてください」

# 4. チェックリストで検証
# specs/002-email-notification/checklists/requirements.md を確認・更新

# 5. 実装計画とタスク分解
# plan.md と tasks.md を作成

# 6. 実装
# 「.github/prompts/speckit.implement.prompt.md の指示に従って、
#  specs/002-email-notification/tasks.md の全タスクを実装してください」

# 7. コミット
git add .
git commit -m "feat: Add email notification feature"

# 8. マージ
git checkout main
git merge 002-email-notification --no-ff -m "Merge: Email notification feature"

# 9. タグ作成
git tag -a v1.1.0 -m "Release version 1.1.0 - Email notification"

# 10. プッシュ
git push origin main
git push origin v1.1.0
```

## 実践的なワークフロー例

### 例: 「監視間隔を動的に変更できるようにする」変更

```powershell
# ステップ1: 現在の仕様を確認
cat specs\001-service-monitor\spec.md

# ステップ2: 変更ブランチを作成
git checkout main
git checkout -b 001-service-monitor-dynamic-interval

# ステップ3: 仕様書を更新
# specs/001-service-monitor/spec.md を編集
```

```markdown
### US4: 動的監視間隔変更

**As a** システム管理者  
**I want to** 監視中に監視間隔を動的に変更できるようにしたい  
**So that** サービスの状態に応じて監視の粒度を調整できる

#### 受け入れ基準
- [ ] 設定画面で監視間隔を変更できる
- [ ] 変更は即座に反映される（再起動不要）
- [ ] 変更履歴がログに記録される
- [ ] 最小値: 1秒、最大値: 3600秒（1時間）
```

```powershell
# ステップ4: タスクを追加
# specs/001-service-monitor/tasks.md の最後に追記
```

```markdown
## Phase 7: Dynamic Configuration (T146-T155)

### ステップ1: インターフェース拡張 (T146-T148)

#### T146: ServiceMonitor に間隔変更メソッド追加
- [ ] IServiceMonitor に UpdateMonitoringInterval(int seconds) メソッド追加
- [ ] ServiceMonitor に実装
- [ ] タイマーの再スケジュール処理

#### T147: 設定ファイルスキーマ拡張
- [ ] config.json に monitoringInterval フィールド追加
- [ ] ConfigurationValidator で検証ロジック追加（1-3600秒）
- [ ] ConfigurationHelper でロード・保存処理更新

#### T148: ユニットテスト作成
- [ ] ServiceMonitor.UpdateMonitoringInterval のテスト
- [ ] 設定保存・読込のテスト

### ステップ2: UI実装 (T149-T153)

#### T149: SettingsForm に間隔設定コントロール追加
- [ ] NumericUpDown コントロール配置
- [ ] ラベル追加: "監視間隔 (秒)"
- [ ] 初期値の読込処理

#### T150: リアルタイム適用ボタン実装
- [ ] "適用" ボタン追加
- [ ] クリックイベントで ServiceMonitor.UpdateMonitoringInterval 呼び出し
- [ ] 成功・失敗のフィードバック表示

#### T151: MainForm にステータス表示追加
- [ ] 現在の監視間隔を表示するラベル
- [ ] 変更時に自動更新

#### T152: バリデーション実装
- [ ] 入力値チェック（1-3600秒）
- [ ] エラーメッセージ表示

#### T153: UI統合テスト
- [ ] 手動テストシナリオ実行

### ステップ3: ログとドキュメント (T154-T155)

#### T154: ログ出力追加
- [ ] 間隔変更時にログ記録
- [ ] 変更前後の値を記録

#### T155: ドキュメント更新
- [ ] README.md に機能説明追加
- [ ] CHANGELOG.md に v1.1.0 エントリ追加
- [ ] config.template.json を更新
```

```powershell
# ステップ5: Copilotに実装依頼
# GitHub Copilot Chat で以下を入力:
```

```
@workspace .github/prompts/speckit.implement.prompt.md の指示に従って実装してください。

変更内容:
- specs/001-service-monitor/spec.md にUS4を追加済み
- specs/001-service-monitor/tasks.md にT146-T155を追加済み

実装対象: T146-T155 (動的監視間隔変更機能)

開始してください。
```

```powershell
# ステップ6: 実装完了後、コミット
git add .
git commit -m "feat: Add dynamic monitoring interval change (T146-T155)

- Add UpdateMonitoringInterval method to ServiceMonitor
- Add interval configuration UI in SettingsForm
- Update configuration schema and validation
- Add logging for interval changes
- Update documentation

Implements US4: Dynamic monitoring interval change
Closes T146-T155"

# ステップ7: マージ
git checkout main
git merge 001-service-monitor-dynamic-interval --no-ff -m "Merge: Dynamic monitoring interval feature

Complete implementation of US4 - Dynamic monitoring interval change

Features:
- Real-time interval adjustment without restart
- UI controls with validation (1-3600 seconds)
- Configuration persistence
- Logging of interval changes

Version: 1.1.0
Tasks: T146-T155"

# ステップ8: バージョンタグ作成
git tag -a v1.1.0 -m "Release version 1.1.0 - Dynamic Monitoring Interval

New Features:
- Dynamic monitoring interval adjustment (US4)
- Real-time configuration update without restart

Changes:
- Updated ServiceMonitor with interval control API
- Enhanced SettingsForm with interval configuration UI
- Extended configuration schema and validation

Implements: T146-T155"

# ステップ9: プッシュ
git push origin main
git push origin v1.1.0

# ステップ10: ブランチクリーンアップ（オプション）
git branch -d 001-service-monitor-dynamic-interval
```

## 重要なポイント

### ✅ やるべきこと

1. **常にブランチを作成**して変更する
   - mainブランチは保護する
   - フィーチャーブランチで作業する

2. **spec.mdを最初に更新**して変更内容を明確にする
   - 新しいユーザーストーリーを追加
   - 既存のストーリーを修正
   - 受け入れ基準を明確に記載

3. **tasks.mdで追跡可能**にする
   - タスク番号を継続（T146以降を追加）
   - 古いタスクは削除せず履歴として残す
   - チェックボックスで進捗管理

4. **constitution.mdの原則**に従っているか確認する
   - `.specify/memory/constitution.md` を参照
   - 設計原則に違反していないか確認

5. **マージ時は--no-ff**でマージコミットを残す
   - 履歴を明確に保つ
   - 機能単位でのロールバックを容易にする

6. **バージョンタグを作成**する
   - セマンティックバージョニング（Major.Minor.Patch）
   - タグメッセージに変更内容を記載

7. **ドキュメントを更新**する
   - CHANGELOG.md に変更履歴を追加
   - README.md に新機能の説明を追加
   - 必要に応じて DEPLOY.md を更新

### ❌ 避けるべきこと

1. **mainブランチで直接編集しない**
   - 必ずフィーチャーブランチを作成

2. **仕様書を更新せずに実装しない**
   - コードより先に仕様を更新
   - 仕様なき実装は技術的負債

3. **タスク番号を振り直さない**
   - T001から始めない
   - 既存の最大番号+1から開始

4. **古いタスクを削除しない**
   - 履歴として残す
   - チェック済み([x])で完了を示す

5. **コミットメッセージを省略しない**
   - Conventional Commits形式を使用
   - feat:, fix:, docs:, refactor: などのプレフィックス

6. **テストを省略しない**
   - 既存機能への影響を確認
   - 新機能のテストケースを追加

## Copilotへの依頼テンプレート

### 仕様変更時

```
@workspace .github/prompts/speckit.implement.prompt.md の指示に従って実装してください。

変更内容:
- specs/{feature-id}/spec.md を更新済み（US{n}追加）
- specs/{feature-id}/tasks.md にT{xxx}-T{yyy}を追加済み

実装対象: T{xxx}-T{yyy} ({機能名})

constitution.mdの原則に従い、既存コードとの整合性を保ちながら実装してください。
```

### 新機能追加時

```
@workspace 新機能「{機能名}」を実装したいです。

以下の手順で進めてください:
1. .github/prompts/speckit.clarify.prompt.md に従って要件整理
2. .github/prompts/speckit.specify.prompt.md に従って仕様書作成
3. .github/prompts/speckit.plan.prompt.md に従って実装計画作成
4. .github/prompts/speckit.tasks.prompt.md に従ってタスク分解
5. .github/prompts/speckit.implement.prompt.md に従って実装

フィーチャーID: {feature-id}
ブランチ名: {feature-id}
```

## ディレクトリ構造

```
specs/
├── 001-service-monitor/          # 既存機能
│   ├── spec.md                   # 仕様書（US1-US4...）
│   ├── plan.md                   # 実装計画（Phase 1-7...）
│   ├── tasks.md                  # タスク一覧（T001-T155...）
│   ├── data-model.md             # データモデル設計
│   ├── research.md               # 技術調査
│   ├── quickstart.md             # クイックスタート
│   ├── checklists/
│   │   └── requirements.md       # 要件チェックリスト
│   └── contracts/                # インターフェース仕様
│       ├── README.md
│       ├── IServiceMonitor.md
│       ├── INotificationService.md
│       └── IConfigurationManager.md
│
└── 002-email-notification/       # 新機能（例）
    ├── spec.md
    ├── plan.md
    ├── tasks.md
    └── ...
```

## バージョニング規則

### セマンティックバージョニング

- **Major (X.0.0)**: 破壊的変更、API変更
- **Minor (x.Y.0)**: 新機能追加（後方互換性あり）
- **Patch (x.y.Z)**: バグ修正、軽微な変更

### 例

- `v1.0.0` → `v1.1.0`: 動的監視間隔変更機能追加（新機能）
- `v1.1.0` → `v1.1.1`: 設定保存時のバグ修正（パッチ）
- `v1.1.1` → `v2.0.0`: 設定ファイル形式変更（破壊的変更）

## トラブルシューティング

### Q: タスク番号が重複してしまった

```powershell
# tasks.md を開いて最後のタスク番号を確認
cat specs\001-service-monitor\tasks.md | Select-String "^#### T"

# 最新番号+1から開始するように修正
```

### Q: マージ時にコンフリクトが発生した

```powershell
# 1. コンフリクトを確認
git status

# 2. ファイルを手動編集して解決
code <conflicted-file>

# 3. 解決後にマージを完了
git add <conflicted-file>
git commit -m "Merge: Resolve conflicts"
```

### Q: 仕様変更が大きすぎて管理しきれない

**解決策**: 複数のフェーズに分割する

```markdown
## Phase 7: Dynamic Configuration - Part 1 (T146-T150)
## Phase 8: Dynamic Configuration - Part 2 (T151-T155)
```

それぞれを別々のブランチで実装し、順次マージする。

## まとめ

Speckitの仕様変更は以下の流れで行います:

1. **ブランチ作成** → 2. **仕様更新** → 3. **タスク追加** → 4. **実装** → 5. **コミット** → 6. **マージ** → 7. **タグ** → 8. **プッシュ**

常に**仕様ファーストで文書化**し、**タスクで追跡可能**にすることで、チーム開発や将来のメンテナンスが容易になります。
