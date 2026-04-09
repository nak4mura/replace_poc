$here = Split-Path -Parent $MyInvocation.MyCommand.Path
. "$here\analyze_endpoints.ps1" -TestMode

Describe "Get-OutputFileName" {
    It "replace_target/ プレフィックスを除去して / を _ に変換する" {
        $result = Get-OutputFileName "replace_target/Modules/HTML/Settings.ascx"
        $result | Should Be "Modules_HTML_Settings.json"
    }

    It "Website配下のファイルを正しく変換する" {
        $result = Get-OutputFileName "replace_target/Website/Default.aspx"
        $result | Should Be "Website_Default.json"
    }

    It "深いパスのファイルを正しく変換する" {
        $result = Get-OutputFileName "replace_target/Website/DesktopModules/Admin/Banners/BannerClickThrough.aspx"
        $result | Should Be "Website_DesktopModules_Admin_Banners_BannerClickThrough.json"
    }

    It "VBファイルを正しく変換する" {
        $result = Get-OutputFileName "replace_target/Library/Services/Search/SearchEngineScheduler.vb"
        $result | Should Be "Library_Services_Search_SearchEngineScheduler.json"
    }

    It "スペースを含むパスを正しく変換する" {
        $result = Get-OutputFileName "replace_target/Library/Entities/Users/Users Online/PurgeUsersOnline.vb"
        $result | Should Be "Library_Entities_Users_Users Online_PurgeUsersOnline.json"
    }
}

Describe "Get-SourceContent" {
    $testDir = "$env:TEMP\analyze_endpoints_test"

    BeforeEach {
        New-Item -ItemType Directory -Path $testDir -Force | Out-Null
    }

    AfterEach {
        Remove-Item -Path $testDir -Recurse -Force -ErrorAction SilentlyContinue
    }

    It "単一ファイルの内容を読み込む" {
        Set-Content -Path "$testDir\test.vb" -Value "Public Class TestClass" -Encoding UTF8
        $result = Get-SourceContent "$testDir\test.vb"
        $result | Should Match "Public Class TestClass"
    }

    It "ascxファイルのコードビハインド(.vb)も含めて読み込む" {
        Set-Content -Path "$testDir\test.ascx" -Value "<asp:Control />" -Encoding UTF8
        Set-Content -Path "$testDir\test.ascx.vb" -Value "Public Class TestCode" -Encoding UTF8
        $result = Get-SourceContent "$testDir\test.ascx"
        $result | Should Match "<asp:Control />"
        $result | Should Match "Public Class TestCode"
        $result | Should Match "Code-Behind"
    }

    It "コードビハインドが無い場合はメインファイルのみ読み込む" {
        Set-Content -Path "$testDir\test.ascx" -Value "<asp:Control />" -Encoding UTF8
        $result = Get-SourceContent "$testDir\test.ascx"
        $result | Should Match "<asp:Control />"
        $result | Should Not Match "Code-Behind"
    }

    It "aspxファイルのコードビハインドも読み込む" {
        Set-Content -Path "$testDir\test.aspx" -Value "<html>" -Encoding UTF8
        Set-Content -Path "$testDir\test.aspx.vb" -Value "Partial Class TestPage" -Encoding UTF8
        $result = Get-SourceContent "$testDir\test.aspx"
        $result | Should Match "<html>"
        $result | Should Match "Partial Class TestPage"
    }
}

Describe "BuildPrompt" {
    It "generates prompt with source and endpoint info" -Test {
        $result = Build-Prompt "sample code" "Screen" "sample screen"
        $result | Should Match "sample code"
        $result | Should Match "Screen"
        $result | Should Match "endpoint_info"
        $result | Should Match "database_crud"
    }
}

Describe "Test-RateLimitError" {
    It "正常終了の場合はfalseを返す" {
        $result = Test-RateLimitError -Output "success" -ExitCode 0
        $result | Should Be $false
    }

    It "rate limitエラーを検出する" {
        $result = Test-RateLimitError -Output "Error: rate limit exceeded" -ExitCode 1
        $result | Should Be $true
    }

    It "429エラーを検出する" {
        $result = Test-RateLimitError -Output "HTTP 429 Too Many Requests" -ExitCode 1
        $result | Should Be $true
    }

    It "overloadedエラーを検出する" {
        $result = Test-RateLimitError -Output "API is overloaded" -ExitCode 1
        $result | Should Be $true
    }

    It "無関係なエラーの場合はfalseを返す" {
        $result = Test-RateLimitError -Output "file not found" -ExitCode 1
        $result | Should Be $false
    }
}

Describe "Save-AnalysisResult" {
    $testDir = "$env:TEMP\analyze_endpoints_test_save"

    BeforeEach {
        New-Item -ItemType Directory -Path $testDir -Force | Out-Null
    }

    AfterEach {
        Remove-Item -Path $testDir -Recurse -Force -ErrorAction SilentlyContinue
    }

    It "有効なJSONを保存してtrueを返す" {
        $json = '{"endpoint_info": {"name": "test"}}'
        $outPath = "$testDir\test.json"
        $result = Save-AnalysisResult -Content $json -OutputPath $outPath
        $result | Should Be $true
        Test-Path $outPath | Should Be $true
    }

    It "Markdownコードブロックを除去して保存する" {
        $json = '```json' + "`n" + '{"name": "test"}' + "`n" + '```'
        $outPath = Join-Path $testDir "test2.json"
        $result = Save-AnalysisResult -Content $json -OutputPath $outPath
        $result | Should Be $true
        $saved = Get-Content $outPath -Raw
        $saved.Trim() | Should Be '{"name": "test"}'
    }

    It "無効なJSONの場合は.errorファイルに保存してfalseを返す" {
        $outPath = Join-Path $testDir "test3.json"
        $result = Save-AnalysisResult -Content "not valid json" -OutputPath $outPath
        $result | Should Be $false
        $errorPath = $outPath + '.error'
        Test-Path $errorPath | Should Be $true
    }
}
