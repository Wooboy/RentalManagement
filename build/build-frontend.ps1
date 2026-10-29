[CmdletBinding()]
param(
    # 前端打 API 的基底路徑；預設走同網域 nginx 反向代理的 /api（見 src/services/api.ts）
    [string]$ApiBaseUrl = "/api",
    [ValidatePattern('^\d{4}\.\d{4}\.\d{4}$')]
    [string]$Version = (Get-Date -Format "yyyy.MMdd.HHmm")
)
$ErrorActionPreference = "Stop"
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$buildPrefix = [IO.Path]::GetFullPath($PSScriptRoot) + [IO.Path]::DirectorySeparatorChar
$frontendOutput = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot "server-frontend"))
$frontendDir = Join-Path $repo "src\frontend"
if (-not $frontendOutput.StartsWith($buildPrefix, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Output must remain inside build: $frontendOutput"
}
if (Test-Path $frontendOutput) { Remove-Item $frontendOutput -Recurse -Force }

Push-Location $frontendDir
try {
    # 用 npm install 而非 npm ci（在 Windows/網路磁碟上較耐用，避免 ENOTEMPTY 鎖檔問題）
    npm install
    if ($LASTEXITCODE) { throw "Frontend npm install failed." }
    $oldApiUrl = $env:VITE_API_BASE_URL
    $env:VITE_API_BASE_URL = $ApiBaseUrl
    try {
        # 直接輸出到 build/server-frontend；本機 npm run build 仍輸出到 dist/
        npm run build -- --outDir $frontendOutput --emptyOutDir
        if ($LASTEXITCODE) { throw "Frontend build failed." }
    }
    finally {
        $env:VITE_API_BASE_URL = $oldApiUrl
    }
}
finally {
    Pop-Location
}
Set-Content -LiteralPath (Join-Path $frontendOutput "version.txt") -Value $Version -Encoding ascii
Write-Host "Frontend: $frontendOutput (version $Version, API $ApiBaseUrl)"
