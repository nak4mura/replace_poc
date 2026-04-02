---
name: process-issue
description: GitHub の Issue を分析して修正・PR作成まで実施。引数にissue番号を指定して使用。
disable-model-invocation: false
allowed-tools: Bash(gh issue:*), Bash(gh pr:*), Bash(git:*), Read, Glob, Grep, Write, Edit
---

$ARGUMENTS で指定された GitHub Issue を分析し修正する。

1. `gh issue view $ARGUMENTS` で Issue 詳細を取得
2. **thinking モードで深く考えながら**問題を分析し、実装方針を検討する
3. **必ず planモード（EnterPlanMode）に入り**、以下を含む実装計画を立ててユーザーに確認する
   - 問題の根本原因
   - 修正対象ファイルと変更内容
   - テスト方針
4. ユーザーの承認後、planモードを抜けて実装を開始する
5. `feature/<任意の名称>` ブランチを作成し、その中で実装を行う
6. 期待される入出力に基づき、テストから作成する
7. テストを実行し、失敗を確認する
8. コミットする
9. Issue の問題が解消し、テストをパスする実装を進める
10. コミットする
11. `gh pr create` で `develop` ブランチへ PR 作成

- 注意点
  - 実装中はテストを変更せず、コードを修正し続ける
  - すべてのテストが通過するまで繰り返す
  - 不明点や判断が難しい箇所は thinking モードで再度深く考える
