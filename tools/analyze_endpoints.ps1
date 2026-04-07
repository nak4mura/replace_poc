param(
    [string]$EndpointsFile = "migration_docs/endpoints.json",
    [string]$OutputDir = "migration_docs/analyze_endpoints",
    [int]$RateLimitWaitMinutes = 10,
    [int]$MaxRetries = 3,
    [switch]$TestMode
)

# --- Helper Functions (stub) ---

function Get-OutputFileName {
    param([string]$FilePath)
    throw "Not implemented"
}

function Get-SourceContent {
    param([string]$FilePath)
    throw "Not implemented"
}

function Build-Prompt {
    param(
        [string]$SourceContent,
        [string]$EndpointType,
        [string]$Description
    )
    throw "Not implemented"
}

function Test-RateLimitError {
    param([string]$Output, [int]$ExitCode)
    throw "Not implemented"
}

function Save-AnalysisResult {
    param([string]$Content, [string]$OutputPath)
    throw "Not implemented"
}

# --- Main ---
if (-not $TestMode) {
    Write-Host "analyze_endpoints.ps1 - Not yet implemented"
}
