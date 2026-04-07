# CLAUDE.md

ASP.NET + VB のプロジェクトを WindowsForms + C# のプロジェクトに移行する際のベストプラクティスを検証するプロジェクト。

## リポジトリ構成

- `replace_target/` — 現在リプレース対象となっているDotNetNukeアプリケーション（振る舞いの正解となるソース）
  - `Library/` — VB.NETで書かれたコアライブラリ（DotNetNuke.Library.vbproj）。ビジネスロジック、データアクセス、エンティティ、サービスを含む
  - `Website/` — web.configを含むASP.NET Web Formsサイトのルートディレクトリ
  - `Tests/` — 既存のテストプロジェクト（C#、Gallio/MbUnitフレームワークを使用）
  - `Modules/` — 個別のモジュールプロジェクト（HTML、Messaging、RazorHost、Taxonomyなど）
  - `BuildScripts/` — MSBuildのカスタムタスクおよびパッケージングターゲット
- `tools/` — 移行用ツール
  - `VBAnalyzer/` — Roslynを使用した移行対象の静的解析ツール
- `migration_docs/` — 移行に使用する情報のドキュメントを出力する

## DB接続情報

```
Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=DotNetNuke;Integrated Security=True
```

