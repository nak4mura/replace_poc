---
name: process-subissues
description: GitHub issue番号から紐づくsub-issuesを取得し、ブロック関係を考慮して未ブロック順に処理します。引数にissue番号を指定して使用。
disable-model-invocation: false
allowed-tools: Bash, Read, Write, Edit, Glob, Grep, Agent
---

ARGUMENTS: $ARGUMENTS

## タスク

GitHubのissue **#$ARGUMENTS** に紐づくsub-issuesを取得し、blockedBy関係を考慮した順序で一つずつ処理します。

## ステップ1: リポジトリ情報の取得

```bash
gh repo view --json owner,name --jq '[.owner.login, .name] | join(" ")'
```

## ステップ2: Sub-issueを順序付きで取得

以下のスクリプトを実行してブロック関係を解析した処理順序を取得します:

```bash
node .claude/skills/process-subissues/scripts/get-ordered-subissues.js <owner> <repo> <issue_number>
```

スクリプトはJSON形式で出力します:
- `ordered`: トポロジカルソート済みのissue一覧（blockedByが先、blockするものが後）
- `hasCycle`: 循環依存の有無（trueなら警告）
- `total` / `open` / `closed`: 統計

## ステップ3: 処理順序を表示

スクリプト出力をもとに以下のテーブルを表示します:

| 順序 | Issue | タイトル | 状態 | ブロック状況 |
|------|-------|----------|------|--------------|
| 1 | #xxx | ... | OPEN | なし（開始可能） |
| 2 | #yyy | ... | OPEN | #xxx依存 |

循環依存がある場合は `⚠️ 循環依存あり` と警告を表示します。

## ステップ4: featureブランチの作成

処理を開始する前に、作業用ブランチを作成します。

1. **親issueのタイトルをもとにブランチ名を決定** — `feature/<スラッグ>` 形式（スラッグは小文字英数字とハイフンのみ）
2. **ブランチを作成してチェックアウト**:
   ```bash
   git checkout -b feature/<スラッグ>
   ```
   既にfeatureブランチにいる場合はそのまま継続します。

## ステップ5: 各Issueを順番に処理

既にCLOSEDのissueはスキップします。OPENのissueを順番に処理します:

### 各issueの処理フロー

1. **issueの詳細を取得**:
   ```bash
   gh issue view <number> --json number,title,body,labels,assignees
   ```

2. **実装方針をユーザーに提示** — 以下を確認:
   - issueで要求されているタスクの内容
   - 実装アプローチ
   - 影響範囲

3. **ユーザーの承認を得てから実装開始**

4. **実装完了後、変更をコミット**:
   ```bash
   git add <変更ファイル>
   git commit -m "fix: #<number> <issueタイトル>

   Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>"
   ```

5. **issueをclose**:
   ```bash
   gh issue close <number> --comment "実装完了"
   ```

6. **次のissueへ** — 直前のissueの完了によりunblockされたissueを提示

## ステップ6: 全subissue完了後の処理

全てのsub-issueがCLOSEDになったら:

1. **親issueをclose**:
   ```bash
   gh issue close <parent_number> --comment "全sub-issueの実装が完了しました"
   ```

2. **developブランチへのプルリクエストを作成**:
   ```bash
   gh pr create --base develop --title "<親issueタイトル>" --body "$(cat <<'EOF'
   ## Summary
   - Closes #<parent_number>
   - <処理したsub-issueの一覧（番号とタイトル）>

   ## Test plan
   - [ ] 各sub-issueの実装内容を確認
   - [ ] 動作確認

   🤖 Generated with [Claude Code](https://claude.com/claude-code)
   EOF
   )"
   ```

## 注意事項

- blockedByの判定はsub-issue間のみ（外部issueによるブロックはスコープ外）
- `canStart: false` かつ `cycleWarning: true` のissueは循環依存の可能性あり
- issueのstate=CLOSEDは既完了としてスキップ
- 引数（issue番号）がない場合は現在のブランチ名から推測:
  ```bash
  git branch --show-current
  ```
- コミットは各sub-issue完了ごとに行い、まとめてコミットしない
- PRのbaseブランチは必ず `develop` とする
