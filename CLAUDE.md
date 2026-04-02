# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Purpose

This is a replacement verification POC (Proof of Concept) project. The goal is to validate a methodology for replacing a DotNetNuke-based ASP.NET + VB.NET application with a WinForms + C# application while reusing the existing SQL Server database.

**Verification strategy:** All business logic functions are recreated with identical names in C#. For each function, the following must match exactly between the original VB and the replacement C#:
- Input parameters
- Output values
- SQL statements issued

Test cases are created against the current VB application to capture expected values (inputs, outputs, SQL). These expected values are then used to verify the replacement C# code produces identical behavior.

## Language

The user communicates in Japanese. Respond in Japanese unless otherwise indicated.

## Repository Structure

- `replace_target/` — The current DotNetNuke application being replaced (source of truth for behavior)
  - `Library/` — Core VB.NET library (`DotNetNuke.Library.vbproj`), contains business logic, data access, entities, services
  - `Website/` — ASP.NET Web Forms site root with `web.config`
  - `Tests/` — Existing test projects (C#, Gallio/MbUnit framework)
  - `Modules/` — Separate module projects (HTML, Messaging, RazorHost, Taxonomy)
  - `BuildScripts/` — MSBuild custom tasks and packaging targets

## Build

The solution uses MSBuild with Visual Studio 2022. Main solution file:

```
replace_target/DotNetNuke_Community_Source.sln
```

Build from command line:
```bash
msbuild replace_target/DotNetNuke_Community_Source.sln /p:Configuration=Debug
```

Test solution (Gallio/MbUnit + Moq):
```
replace_target/DotNetNuke_Community_UnitTests_Source.sln
```

## Architecture of the Current Application

### Data Access Pattern

Abstract provider pattern with SQL Server implementation:
- `Library/Data/DataProvider.vb` — Abstract base class defining all data access methods
- `Library/Providers/DataProviders/SqlDataProvider/SqlDataProvider.vb` — SQL Server implementation
- Provider is resolved via `ComponentFactory.GetComponent(Of DataProvider)()`

Key data access methods: `ExecuteReader`, `ExecuteNonQuery`, `ExecuteScalar`, `ExecuteDataSet`, `ExecuteSQL`, `ExecuteScript`

SQL uses `{databaseOwner}` and `{objectQualifier}` placeholders for portability.

### Database Connection

SQL Server via `SiteSqlServer` connection string in `Website/web.config`:
```
Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=DotNetNuke;Integrated Security=True
```

### Code Distribution

- VB.NET: ~1,010 files (93%) — all core library, providers, services, entities
- C#: ~79 files (7%) — Syndication component, external components, test projects

### Key Patterns

- **Provider pattern** throughout (data, caching, logging, membership, scheduling, search, navigation, permissions)
- **Namespace:** `DotNetNuke.*`
- **VB.NET strict mode** enabled (`OptionStrict=On`)
- **Target framework:** .NET Framework 3.5 (main), .NET Framework 4.0 (tests)

## Replacement Verification Approach

When creating test cases for verification:
1. Identify a business logic function in the VB source
2. Determine its inputs, expected outputs, and SQL statements it issues
3. Create a test case that captures these as expected values from the current VB code
4. Use these expected values to assert the replacement C# function behaves identically

SQL capture is critical — the replacement must issue the same SQL queries with the same parameters to ensure database compatibility.

## 検証問題の記録

検証中に発生した問題は `docs/issues/verification_issues.md` に追記する。ファイル内のテンプレートに従い、Issue番号を連番で付与して記録する。記録項目は以下の通り:

- 対象関数名
- 問題の概要
- 発生日
- 原因分類（言語差異 / SQL差異 / データ型差異 / ロジック差異 / その他）
- ステータス（未対応 / 対応中 / 対応済）
- 問題の詳細
- 対応方針・対応内容
