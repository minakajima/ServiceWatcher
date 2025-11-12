---
marp: true
theme: default
paginate: true
style: |
  section {
    background: linear-gradient(135deg, #0f2027 0%, #203a43 50%, #2c5364 100%);
    color: #ffffff;
    font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
    padding: 50px;
  }
  section h1 {
    color: #ffffff;
    text-shadow: 2px 2px 4px rgba(0,0,0,0.3);
    border-bottom: 3px solid rgba(255,255,255,0.3);
    padding-bottom: 20px;
  }
  section h2 {
    color: #f0f0f0;
    text-shadow: 1px 1px 2px rgba(0,0,0,0.2);
  }
  section h3 {
    color: #e0e0e0;
  }
  section strong {
    color: #ffd700;
  }
  section a {
    color: #00ffff;
  }
  section code {
    background: rgba(0,0,0,0.3);
    color: #00ff00;
    padding: 2px 8px;
    border-radius: 4px;
  }
  section table {
    background: transparent;
    border-radius: 8px;
    border-collapse: collapse;
    width: auto;
    margin: 20px auto;
  }
  section th {
    background: rgba(0,0,0,0.7);
    color: #00ffff;
    font-weight: bold;
    padding: 15px 30px;
    border: 2px solid rgba(255,255,255,0.3);
    font-size: 1.1em;
  }
  section td {
    color: #ffffff;
    padding: 15px 30px;
    border: 1px solid rgba(255,255,255,0.2);
    background: rgba(0,0,0,0.4);
    font-size: 1em;
  }
  section ul, section ol {
    margin-left: 20px;
  }
  section li {
    margin: 10px 0;
  }
  /* ページ番号のスタイル */
  section::after {
    color: rgba(255,255,255,0.7);
  }
  /* タイトルスライド専用 */
  section.title {
    text-align: center;
    display: flex;
    flex-direction: column;
    justify-content: center;
  }
  section.title h1 {
    font-size: 3em;
    margin-bottom: 0.5em;
  }
---

<!-- _class: title -->

# GitHub Speckit で開発してみた

---

## 📌 今日のテーマ
**「GitHubの新ツール Speckit を使って<br>Windowsサービス監視ツールを開発した話」**

---

## 🤔 Speckitって何？

**GitHubが2025年9月に公開した開発支援ツール**

- **仕様駆動開発**を実現するOSSフレームワーク
- 「何を作るか」を明確にしてから実装へ
- AIとの協働を前提とした新しい開発スタイル

---

## 📝 従来のAI開発の課題

❌ **「いい感じに作って」では手戻りが多い**
- AIへの指示が曖昧
- 仕様と実装がズレる
- チームで品質がバラバラ

---

## ✨ Speckitの4ステップ

1. **📋 Specify** - 仕様を自然言語で記述
2. **🎯 Plan** - 技術方針を立案
3. **✅ Tasks** - タスクに分解
4. **💻 Implement** - AIと協働で実装

**仕様を起点に、すべてが連動！**

---

## 🛠️ 実際に作ったもの

**ServiceWatcher - Windowsサービス監視ツール**

### 主な機能
- ✅ サービス状態の自動監視
- ✅ 異常時の通知表示
- ✅ 多言語対応（日本語/英語）
- ✅ 設定のGUI管理

### 技術スタック
- C# / .NET 8.0
- Windows Forms
- JSON設定ファイル

---

## 💡 開発してわかったこと

### Good 👍
- **仕様が常に最新** - ドキュメントが信頼できる
- **AIの精度向上** - 構造化された情報で出力が安定
- **手戻りが激減** - 事前に方針を固めるため

### 課題 🤔
- 初期バージョン（v0.0.9）で機能が発展途上
- 仕様記述に慣れが必要
- チーム運用のルール設計が重要

---

## 📊 開発プロセスの比較

| 従来の開発 | Speckit使用 |
|:---:|:---:|
| プロンプト→コード | 仕様→計画→タスク→コード |
| 曖昧な指示 | 構造化された仕様 |
| 手戻り多い | 一貫性が高い |
| ドキュメント古い | 常に最新 |

---

## 🎯 こんな人におすすめ

✅ AIコーディングで手戻りが多い
✅ チームでAI活用の品質を揃えたい
✅ 仕様書を信頼できる情報源にしたい
✅ 複雑なプロジェクトでAIを活用したい

---

## 🚀 まとめ

### Speckitは「AI時代の開発文化」を変える

- 📝 仕様を中心に据えた開発
- 🤖 AIとの協働の質が向上
- 📚 ドキュメントが開発の起点に

**まだ初期版だけど、可能性を感じました！**

---

## 🔗 参考情報

**Speckit公式**
- https://github.com/github/spec-kit

