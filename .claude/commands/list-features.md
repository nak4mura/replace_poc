# /list-features

ソースコードとAPIエンドポイントを解析して、機能単位の一覧をMarkdown形式で生成する手順：

## 解析フロー

1. **プロジェクト構造の把握**
   - ルートディレクトリの構造を確認
   - 主要なソースコードディレクトリ（src/, app/, lib/, etc.）を特定
   - 使用言語を特定（package.json, requirements.txt, Cargo.toml, go.mod, etc.）

2. **APIエンドポイントの抽出**
   - ルーティング定義ファイルを特定（routes/, api/, controllers/, handlers/, etc.）
   - フレームワークに応じたルーティング定義を検索：
     - Express: app.get(), router.post(), etc.
     - Fastify: fastify.get(), fastify.route(), etc.
     - NestJS: @Get(), @Post(), @Controller(), etc.
     - Next.js: pages/api/, app/api/, etc.
   - エンドポイント情報を抽出：
     - HTTP メソッド
     - パス
     - ハンドラ関数のファイルパスと行番号
     - 説明（JSDoc、コメント、ドキュメントから抽出）

3. **ソースコードからの機能抽出**
   - サービス層、コントローラ層、ユースケース層を特定
   - ビジネスロジックを含むクラス・関数を抽出：
     - クラスメソッド
     - スタンドアロン関数
     - TypeScript/JavaScript の export 関数
     - Python の関数・メソッド
     - Go の関数・メソッド
   - 機能説明を抽出（JSDoc、docstring、コメント）
   - 関連するエンティティやドメインモデルを特定

4. **機能のカテゴライズ**
   - ドメインごとにグループ化（ユーザー管理、注文処理、認証など）
   - 類似機能をまとめる
   - 階層構造を作成（カテゴリ > サブカテゴリ > 機能）

5. **Markdown出力の生成**
   以下の構造で機能一覧を出力する：

```markdown
# 機能一覧

## API エンドポイント

### GET /api/path
- **説明**: エンドポイントの説明
- **ファイル**: `src/path/to/file.ts:123`
- **ハンドラ**: `handlerFunctionName`

...

## 機能一覧（ビジネスロジック）

### カテゴリ名

#### 機能名
- **説明**: 機能の詳細説明
- **ファイル**: `src/path/to/service.ts:45`
- **関連エンティティ**: EntityName
- **関連エンドポイント**: GET /api/path, POST /api/other

...

## 統計情報

- **API エンドポイント数**: 10
- **機能カテゴリ数**: 5
- **機能総数**: 25
```

## 注意点

- ファイルパスはプロジェクトルートからの相対パスとする
- 行番号はハンドラ関数・クラス定義の開始行
- API エンドポイントと機能を関連付けることで、理解しやすくする
- 重複する機能は統合する
- 明確な説明がない場合は、関数名・クラス名から推測した説明を記載
- 出力先は `docs/features.md` とする
