# /create-subissues

GitHub issueをタスク分解してsub-issuesを作成します。

## 手順

1. **Issue取得**: 指定されたissue番号（引数）からissueの詳細を取得
   - `gh issue view {番号} --json title,body,labels,assignees`
   - 引数がない場合は、現在のブランチに関連するissueを探す

2. **タスク分解**: issueの内容を分析し、実装可能なタスクに分解
   - 依存関係（前後関係）を考慮して順序を決定
   - 各タスクには以下を含める:
     - 明確なタイトル
     - 実装内容の説明
     - 依存するタスクがあれば明記
     - 受け入れ条件

3. **ユーザー確認**: 分解案を表示し、ユーザーに確認
   - マークダウンのテーブル形式で表示
   - タスク間の依存関係を矢印などで視覚化
   - ユーザーが修正・承認できるようにする

4. **Sub-issue作成**: ユーザー承認後に実行
   - `gh issue create` で各サブイシューを作成
   - GraphQL APIでブロック関係を設定

5. **ブロック関係の設定**: GraphQL APIを使用
   ```bash
   # タスクBがタスクAに依存する場合（B is blocked by A）
   gh api graphql -f query='
     mutation {
       addBlockedBy(input: {issueId: "Bのnode_id", blockingIssueId: "Aのnode_id"}) {
         issue { number }
       }
     }'
   ```

## 使用例
- `/create-subissues 123` - issue #123を分解
- `/create-subissues` - 引数なしの場合は現在のブランチに関連するissueを探す

## 依存関係の設定
GitHub Issues のブロック機能を使用:
- **Blocked By**: このタスクが開始できる前に完了すべきタスク
- **Blocking**: このタスクが完了するまで待っているタスク

## node_idの取得方法
```bash
gh issue view {番号} --json id --jq '.id'
```

## GraphQL API ミューテーション
- `addSubIssue` - サブイシュー関係を追加
- `addBlockedBy` - ブロック関係を追加
