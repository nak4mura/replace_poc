param(
    [string]$EndpointsFile = "migration_docs/endpoints.json",
    [string]$OutputDir = "migration_docs/analyze_endpoints",
    [int]$RateLimitWaitMinutes = 10,
    [int]$MaxRetries = 3,
    [switch]$TestMode
)

# --- Helper Functions ---

function Get-OutputFileName {
    param([string]$FilePath)
    $relative = $FilePath -replace '^replace_target/', ''
    $flat = $relative -replace '/', '_'
    $baseName = [System.IO.Path]::GetFileNameWithoutExtension($flat)
    return "$baseName.json"
}

function Get-SourceContent {
    param([string]$FilePath)
    $content = "=== File: $FilePath ===" + [Environment]::NewLine
    $content += Get-Content -Path $FilePath -Raw -Encoding UTF8

    $ext = [System.IO.Path]::GetExtension($FilePath)
    if ($ext -eq '.ascx' -or $ext -eq '.aspx') {
        $codeBehind = "$FilePath.vb"
        if (Test-Path $codeBehind) {
            $content += [Environment]::NewLine + [Environment]::NewLine
            $content += "=== Code-Behind: $codeBehind ===" + [Environment]::NewLine
            $content += Get-Content -Path $codeBehind -Raw -Encoding UTF8
        }
    }
    return $content
}

function Build-Prompt {
    param(
        [Parameter(Position=0)][string]$SourceContent,
        [Parameter(Position=1)][string]$EndpointType,
        [Parameter(Position=2)][string]$Description
    )

    $systemInstruction = @'
あなたは優秀なリバースエンジニアリングAIです。提供されたソースコードを解析し、システムの移行設計書に必要なメタデータを抽出してください。コードに明記されていない推測は避け、事実のみを抽出してください。必ず以下のキーを持つJSON形式のみを出力してください。Markdownのコードブロック(```json)は不要です。

{
  "endpoint_info": {
    "name": "エンドポイントや機能の論理名",
    "trigger_or_url": "URLパス、またはバッチ/画面のトリガー条件",
    "type": "API / Screen / Batch / Function のいずれか"
  },
  "contracts": {
    "request_params": ["受け取る主要なパラメータ、型、必須/任意"],
    "response_structure": "返却するデータの構造やViewに渡す主要なデータ"
  },
  "database_crud": [
    {
      "table_name": "対象テーブル/エンティティ名",
      "operation": "CREATE / READ / UPDATE / DELETE",
      "description": "どのような条件・目的で操作するか"
    }
  ],
  "dependencies": {
    "internal_methods": ["呼び出している主要な内部メソッドやサービスクラス"],
    "external_calls": ["外部API呼び出し、メール送信、別システムへの連携などの副作用"]
  },
  "security_and_rules": {
    "auth_roles": ["要求される権限やロール"],
    "validations": ["エラーを返す主要な業務ルールや前提条件（ガード節）"]
  }
}
'@

    $prompt = $systemInstruction + [Environment]::NewLine + [Environment]::NewLine
    $prompt += "【エンドポイント情報】" + [Environment]::NewLine
    $prompt += "- タイプ: $EndpointType" + [Environment]::NewLine
    $prompt += "- 説明: $Description" + [Environment]::NewLine + [Environment]::NewLine
    $prompt += "【ソースコード】" + [Environment]::NewLine
    $prompt += $SourceContent
    return $prompt
}

function Test-RateLimitError {
    param([string]$Output, [int]$ExitCode)
    if ($ExitCode -eq 0) { return $false }
    $patterns = @('rate limit', 'rate_limit', '429', 'overloaded', 'too many requests', 'quota')
    foreach ($p in $patterns) {
        if ($Output -match [regex]::Escape($p)) { return $true }
    }
    return $false
}

function Save-AnalysisResult {
    param([string]$Content, [string]$OutputPath)

    $cleaned = $Content.Trim()
    if ($cleaned.StartsWith('```')) {
        $lines = $cleaned -split "`n"
        $lines = $lines[1..($lines.Length - 2)]
        $cleaned = ($lines -join "`n").Trim()
    }

    try {
        $null = $cleaned | ConvertFrom-Json
        [System.IO.File]::WriteAllText($OutputPath, $cleaned, [System.Text.Encoding]::UTF8)
        return $true
    }
    catch {
        $errorPath = $OutputPath + '.error'
        [System.IO.File]::WriteAllText($errorPath, $Content, [System.Text.Encoding]::UTF8)
        return $false
    }
}

# --- Main ---
if ($TestMode) { return }

$ErrorActionPreference = "Continue"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptDir
$endpointsPath = Join-Path $projectRoot $EndpointsFile
$outputDirPath = Join-Path $projectRoot $OutputDir

if (-not (Test-Path $endpointsPath)) {
    Write-Error "endpoints.json not found: $endpointsPath"
    exit 1
}

$endpoints = Get-Content -Path $endpointsPath -Raw -Encoding UTF8 | ConvertFrom-Json

if (-not (Test-Path $outputDirPath)) {
    New-Item -ItemType Directory -Path $outputDirPath -Force | Out-Null
}

$total = $endpoints.Count
$processed = 0
$skipped = 0
$failed = 0

Write-Host "=== analyze_endpoints.ps1 ==="
Write-Host "Total endpoints: $total"
Write-Host ""

for ($i = 0; $i -lt $total; $i++) {
    $endpoint = $endpoints[$i]
    $filePath = $endpoint.file_path
    $outputName = Get-OutputFileName $filePath
    $outputPath = Join-Path $outputDirPath $outputName

    Write-Host "[$($i + 1)/$total] $filePath"

    if (Test-Path $outputPath) {
        Write-Host "  Skipped (already exists)"
        $skipped++
        continue
    }

    $sourceFullPath = Join-Path $projectRoot $filePath
    if (-not (Test-Path $sourceFullPath)) {
        Write-Warning "  Source file not found: $sourceFullPath"
        $failed++
        continue
    }

    $sourceContent = Get-SourceContent $sourceFullPath
    $prompt = Build-Prompt $sourceContent $endpoint.type $endpoint.description

    $tempFile = [System.IO.Path]::GetTempFileName()
    [System.IO.File]::WriteAllText($tempFile, $prompt, [System.Text.Encoding]::UTF8)

    $success = $false
    $retryCount = 0

    while (-not $success -and $retryCount -le $MaxRetries) {
        try {
            $result = Get-Content $tempFile -Raw -Encoding UTF8 | claude -p --output-format text 2>&1
            $exitCode = $LASTEXITCODE
            $output = $result | Out-String

            if ($exitCode -eq 0) {
                $saved = Save-AnalysisResult -Content $output -OutputPath $outputPath
                if ($saved) {
                    Write-Host "  OK -> $outputName"
                    $processed++
                } else {
                    Write-Warning "  Invalid JSON response -> ${outputName}.error"
                    $failed++
                }
                $success = $true
            }
            elseif (Test-RateLimitError -Output $output -ExitCode $exitCode) {
                $retryCount++
                if ($retryCount -le $MaxRetries) {
                    Write-Host "  Rate limit detected. Waiting $RateLimitWaitMinutes minutes... (retry $retryCount/$MaxRetries)"
                    Start-Sleep -Seconds ($RateLimitWaitMinutes * 60)
                } else {
                    Write-Warning "  Rate limit: max retries exceeded"
                    $failed++
                }
            }
            else {
                Write-Warning "  Claude CLI error: $output"
                $failed++
                $success = $true
            }
        }
        catch {
            Write-Warning "  Exception: $_"
            $failed++
            $success = $true
        }
    }

    Remove-Item $tempFile -ErrorAction SilentlyContinue
}

Write-Host ""
Write-Host "=== Summary ==="
Write-Host "Total: $total | Processed: $processed | Skipped: $skipped | Failed: $failed"
