# Slashコマンド活用ガイド (Speckit / AIチャット統合)

AIチャットへ指示を入力する際に、提供されている `/speckit.*` スラッシュコマンドを軸に「何を→どの順で→どう検証するか」を定型化するためのガイドです。旧プロンプト集をコマンド中心表現に再編しました。

---
## コマンド一覧 (再掲)

### コア開発フロー
| コマンド | 目的 | タイミング | 主たる成果物 |
|----------|------|------------|---------------|
| /speckit.constitution | 開発原則/指針の確立・更新 | 初回 / 破壊的方針転換時 | `constitution.md` |
| /speckit.specify | 要件・ユーザーストーリー定義 | 機能開始直後 | `spec.md` (US, 受け入れ基準) |
| /speckit.plan | 技術的実装計画（段階/フェーズ） | 要件確定後 | `plan.md` (Phase構成) |
| /speckit.tasks | 実装タスク分解 | 計画承認後 | `tasks.md` (Txxx) |
| /speckit.implement | タスク実行 (コード生成/修正) | 準備完了後 | 変更差分 + テスト結果 |

### 品質強化オプション
| コマンド | 目的 | 実行推奨タイミング | 期待出力 |
|----------|------|--------------------|-----------|
| /speckit.clarify | 不十分な要件の質問生成 | `/specify` 前 or 直後 | 質問リスト / 補足要求 |
| /speckit.analyze | アーティファクト整合性・網羅性分析 | `/tasks` 完了直後 | 欠落/矛盾指摘一覧 |
| /speckit.checklist | 品質チェック項目生成 | 実装前/レビュー前 | カスタムチェックリスト |

---
## 推奨ワークフロー (既存機能拡張)
1. `/speckit.clarify` で不足要件を洗い出し (任意)
2. `/speckit.specify` で新USや受け入れ基準を追加
3. `/speckit.plan` で新フェーズ (例: Phase 7) を追加
4. `/speckit.tasks` で T146+ のタスクを生成
5. `/speckit.analyze` で spec / plan / tasks の整合性検証
6. `/speckit.checklist` で品質ゲート (テスト/ログ/国際化 等) を明示
7. `/speckit.implement` でタスクを安全に順次実行

> 既存コード差分が大きい場合は `/speckit.analyze` を複数回挟み回帰影響を常時可視化。

---
## 推奨ワークフロー (新機能追加)
```
/speckit.clarify
  → 未確定領域質問を回答
/speckit.specify
  → US/受け入れ基準確定
/speckit.plan
  → フェーズ分解 (MVP / 拡張 / Hardening)
/speckit.tasks
  → T001 から連番生成
/speckit.analyze
  → 欠落モデル/境界条件指摘反映
/speckit.checklist
  → ドキュメント/テスト/性能チェック生成
/speckit.implement
  → 順次実装 (フェーズ単位 or バッチ)
```

---
## コマンド別入力テンプレート

### /speckit.clarify 用
```
/speckit.clarify
前提:
- 目的: 監視間隔動的変更 (US4) 追加
- 未確定: ログ粒度 / UI即時反映方法 / エラー表示方針
出力形式: 質問一覧 (カテゴリ別: UI / ドメイン / 例外 / 運用) + 重要度タグ(H/M/L)
```

### /speckit.specify 用
```
/speckit.specify
目的: US4 追加
既存参照: specs/001-service-monitor/spec.md (US1-US3) 概要要約後に差分のみ提示
要求:
- 新しい US4 の全文
- 受け入れ基準 (箇条書き, テスト可能表現)
- 非ゴール (除外範囲)
```

### /speckit.plan 用
```
/speckit.plan
目的: 動的監視間隔の実装フェーズ設計
提供情報:
- 影響ファイル: ServiceMonitor.cs, SettingsForm.cs, config.json, ConfigurationValidator.cs
- 制約: 既存監視ループ停止不要 / 後方互換維持
要求:
- 新規 Phase 7: 'Dynamic Interval'
- フェーズ内ステップ列挙 (API拡張 → UI → Validation → Logging → Docs)
```

### /speckit.tasks 用
```
/speckit.tasks
目的: Phase 7 をタスク分解
ガイドライン:
- 1タスク = 1責務 (最大10ファイル編集)
- テスト追加は機能タスク直後
出力形式: T146-T155 / 依存関係 / 成功条件 / リスク
```

### /speckit.analyze 用
```
/speckit.analyze
対象: spec.md (US4), plan.md Phase 7, tasks.md (T146-T155)
要求:
- 矛盾 (値範囲不整合等)
- 欠落 (例: 負値テスト未指定)
- 重複 (類似タスク)
出力: Issue種別 / 対象 / 修正提案
```

### /speckit.checklist 用
```
/speckit.checklist
目的: 実装前品質ゲート定義
カテゴリ: テスト / 例外処理 / ログ / i18n / 設定永続化 / パフォーマンス
形式: チェック項目 [ ] / 自動判定可否 / 対応タスク番号
```

### /speckit.implement 用
```
/speckit.implement
対象タスク: T146-T148 (API + Validation + テスト)
進め方:
- タスクごと: 変更予定ファイル宣言 → 実装 → ビルド/テスト結果 (PASS/FAIL) → 次へ
制約:
- 無関係ファイル非編集
- 既存公開API破壊時は設計再協議を促す
```

---
## 途中で方針不安時の"安全確認"例
```
/speckit.analyze
追加情報: 途中まで実装済み (T146-T150) コミット差分要約:
- ServiceMonitor.cs: Timer再構成
- SettingsForm.cs: Interval入力追加
要求: 潜在的なレース/リソースリーク/イベント二重購読を検証し必要追加タスク提案
```

---
## 品質保証とゲート例 (コマンド連携)
| フェーズ | 推奨コマンド | 目的 |
|----------|--------------|------|
| 要件未確定 | /speckit.clarify | 質問生成/あいまい除去 |
| 要件定義 | /speckit.specify | 明確なUS & 受入基準 |
| 計画設計 | /speckit.plan | 技術的分解と順序 |
| タスク列挙 | /speckit.tasks | 実装単位の明確化 |
| 整合性検証 | /speckit.analyze | 欠落/矛盾発見 |
| 実装前品質 | /speckit.checklist | チェックリスト生成 |
| 実装 | /speckit.implement | 実コード変更 |

---
## 失敗しやすいアンチパターン → コマンド活用改善
| アンチパターン | 問題 | 修正行動 |
|----------------|------|-----------|
| 仕様曖昧のまま実装開始 | 後戻り多い | 先に `/speckit.clarify` で質問抽出 |
| タスク粒度が巨大 | 差分レビュー困難 | `/speckit.tasks` で再分解依頼 |
| 受け入れ基準とテスト乖離 | 網羅漏れ | `/speckit.analyze` で整合性チェック |
| チェック観点属人化 | 品質ばらつき | `/speckit.checklist` で標準化 |
| 実装後まとめて修正 | 回帰誘発 | `/speckit.implement` で段階報告 |

---
## 差分影響分析プロンプト例 (高度)
```
/speckit.analyze
追加観点: パフォーマンス (監視間隔頻度増加時の CPU 使用率) / スレッド安全性
要求: 現在の設計が高頻度(1秒)ポーリング時に問題を起こす箇所と緩和策 (Backoff / Debounce / Cancellation Token 再利用) を列挙
```

---
## バージョンタグ生成支援
```
/speckit.checklist
目的: v1.1.0 リリース前最終チェック
カテゴリ追加: ReleaseDocs / MigrationNotes
→ 完了後 `/speckit.implement` で README & CHANGELOG 更新タスク実行
```
その後:
```
タグ生成要約:
Version: v1.1.0
Type: Minor (後方互換)
Added: US4 Dynamic Interval
Changed: SettingsForm UI
Removed: なし
Fixed: 初期ステータス Unknown 表示遅延
```

---
## クイックチートシート
| 状態 | 次の一手 | 入力例 |
|------|----------|--------|
| ざっくり要件しか無い | clarify | `/speckit.clarify 機能: 動的間隔変更 未確定: エラー表示/ログ粒度` |
| US記述済み | plan | `/speckit.plan 影響ファイル: ServiceMonitor.cs ...` |
| 計画OK | tasks | `/speckit.tasks Phase7 分解指針: 1責務1タスク` |
| タスク生成後 | analyze | `/speckit.analyze 対象: spec/plan/tasks` |
| 品質ゲート必要 | checklist | `/speckit.checklist カテゴリ: ログ/テスト/性能` |
| 実装開始 | implement | `/speckit.implement タスク: T146-T148` |

---
## ベストプラクティス (入力フォーマット最適化)
1. 冒頭 1行で目的
2. 前提 (既存ファイル・ブランチ・未決事項)
3. 成果物形式 (テーブル/箇条書き/コード差分 等)
4. 制約 (互換性 / パフォーマンス / セキュリティ)
5. コマンド実行
6. 次段階へ移る前に `/speckit.analyze` で健全性確認

---
## 例: US4 実装一連 (最短版)
```
/speckit.clarify 目的: US4 動的監視間隔 既存: US1-3 未確定: ログ粒度/UI即時反映方式
/speckit.specify 差分: 新US4 受け入れ基準 (1-3600秒 / 即時適用 / ログ記録)
/speckit.plan 影響: ServiceMonitor.cs / SettingsForm.cs / config.json / Validator
/speckit.tasks Phase7 → T146 API / T147 Schema / T148 Test / T149 UI Control / T150 Apply Button / T151 Interval Label / T152 Validation / T153 Manual test / T154 Logging / T155 Docs
/speckit.analyze 対象: spec/plan/tasks 抜け: 負値/境界テスト提案
/speckit.checklist カテゴリ: Validation / Logging / i18n / Persistence / Concurrency
/speckit.implement タスク: T146-T148 (段階レポート)
```

---
## トラブルシューティング (コマンド併用)
| 問題 | 対応コマンド組 | ねらい |
|------|---------------|--------|
| タスク重複 | /speckit.analyze + /speckit.tasks | 再生成と重複検出 |
| 要件抜けが後から判明 | /speckit.clarify 再実行 | 追加質問で補完 |
| 実装で方向性揺れ | /speckit.plan 再生成 | 計画再固定 |
| 品質観点漏れ | /speckit.checklist | チェックリスト再評価 |

---
## セマンティックバージョニングとコマンド連携
| 変更種別 | 前段準備 | 推奨コマンド | 追加観点 |
|-----------|----------|--------------|-----------|
| Patch | 影響最小差分特定 | /speckit.analyze | 回帰テスト最小集合 |
| Minor | 新US追加 | /speckit.specify /plan /tasks | 互換性維持確認 |
| Major | 破壊的API計画 | /speckit.clarify /plan /analyze | 移行手順 / Deprecation |

---
## 最終出荷前レビュー例
```
/speckit.analyze 対象: spec.md / plan.md / tasks.md / 直近 diff
要求:
- 公開API破壊有無
- 未テスト境界 1-3件
- ログ/例外/国際化不足指摘
→ 修正後 `/speckit.checklist` で最終ゲート生成
```

---
## 注意点まとめ
- スラッシュコマンドは段階遷移のスイッチ。飛ばさず直列化で精度向上
- 大量一括実装は `/speckit.implement` で避け、タスクバッチ(3-5件)単位
- 分析結果は次コマンドにフィードバックして反復最適化

---
## 旧ガイドからの移行メモ
旧: 手動でプロンプト文章を作成 → 新: 標準化コマンドで成果物自動生成 → 人は差分確認と補正へ集中。

---
## 追加要望がある場合
以下のパターンで追記可能: テスト生成強化 / 負荷試験観点 / セキュリティチェック / i18n改善。必要になったら `/speckit.clarify` で新カテゴリ質問抽出 → `/speckit.checklist` へ反映。

---
AI入力品質 = 出力品質。目的/前提/制約/成果物形式 + 適切な `/speckit.*` コマンドを組み合わせ、再現性の高い進行を維持してください。

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
