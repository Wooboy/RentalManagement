[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")][string]$Configuration = "Release",
    # Ubuntu 主機為 ARM 時改成 linux-arm64
    [string]$Runtime = "linux-x64",
    # 前端打 API 的基底路徑；預設走同網域 nginx 反向代理的 /api（見 src/services/api.ts）
    [string]$ApiBaseUrl = "/api",
    [ValidatePattern('^\d{4}\.\d{4}\.\d{4}$')]
    [string]$Version = (Get-Date -Format "yyyy.MMdd.HHmm"),
    [switch]$RunTests,
    # 另外產生自帶執行檔的 efbundle（約 140MB）；預設只輸出 idempotent SQL
    [switch]$EfBundle,
    # 一併打包 Seed/seed-template.json（可能含真實資料，預設不打包）
    [switch]$IncludeSeed
)
$ErrorActionPreference = "Stop"

# 前後端可各自單獨建置：build-backend.ps1、build-frontend.ps1；此處共用同一個 $Version
& (Join-Path $PSScriptRoot "build-backend.ps1") -Configuration $Configuration -Runtime $Runtime `
    -Version $Version -RunTests:$RunTests -EfBundle:$EfBundle -IncludeSeed:$IncludeSeed
& (Join-Path $PSScriptRoot "build-frontend.ps1") -ApiBaseUrl $ApiBaseUrl -Version $Version

Write-Host ""
Write-Host "Server packages were built (version $Version)."
Write-Host "Ubuntu 部署步驟：docs\DEPLOYMENT_UBUNTU.md"
