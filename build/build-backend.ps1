[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")][string]$Configuration = "Release",
    # Ubuntu 主機為 ARM 時改成 linux-arm64
    [string]$Runtime = "linux-x64",
    [ValidatePattern('^\d{4}\.\d{4}\.\d{4}$')]
    [string]$Version = (Get-Date -Format "yyyy.MMdd.HHmm"),
    [switch]$RunTests,
    # 另外產生自帶執行檔的 efbundle（約 140MB）；預設只輸出 idempotent SQL
    [switch]$EfBundle,
    # 一併打包 Seed/seed-template.json（可能含真實資料，預設不打包）
    [switch]$IncludeSeed
)
$ErrorActionPreference = "Stop"
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$buildPrefix = [IO.Path]::GetFullPath($PSScriptRoot) + [IO.Path]::DirectorySeparatorChar
$backendOutput = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot "server-backend"))
$backendDir = Join-Path $repo "src\backend"
$apiProject = Join-Path $backendDir "RentalManager.Api\RentalManager.Api.csproj"
$backendTests = Join-Path $repo "tests\RentalManager.Api.Tests\RentalManager.Api.Tests.csproj"
$versionProps = @("-p:Version=$Version", "-p:AssemblyVersion=$Version", "-p:FileVersion=$Version", "-p:InformationalVersion=$Version")
if (-not $backendOutput.StartsWith($buildPrefix, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Output must remain inside build: $backendOutput"
}
if (Test-Path $backendOutput) { Remove-Item $backendOutput -Recurse -Force }

# 測試須在 -r 還原之前：dotnet test 的隱含還原不帶 RID，會覆寫 project.assets.json 使 publish 失敗
if ($RunTests) {
    dotnet test $backendTests -c $Configuration @versionProps
    if ($LASTEXITCODE) { throw "Backend tests failed." }
}
else {
    Write-Host "Backend xUnit tests skipped. Use -RunTests to execute them."
}
dotnet restore $apiProject -r $Runtime
if ($LASTEXITCODE) { throw "Backend restore failed." }
dotnet publish $apiProject -c $Configuration -r $Runtime --self-contained false --no-restore -o $backendOutput @versionProps -p:UseAppHost=false -p:DebugType=None -p:DebugSymbols=false
if ($LASTEXITCODE) { throw "Backend publish failed." }
# 開發用設定不進部署包；正式設定由主機上的環境變數檔提供
Remove-Item (Join-Path $backendOutput "appsettings.Development.json") -Force -ErrorAction SilentlyContinue
if ($IncludeSeed) {
    Write-Host "Seed template included. 業務資料為空時啟動會自動匯入。"
}
else {
    Remove-Item (Join-Path $backendOutput "Seed") -Recurse -Force -ErrorAction SilentlyContinue
}

# ── DB migration（正式環境不會自動 migrate，見 Program.cs）──
$migrationOutput = Join-Path $backendOutput "db_migration"
New-Item -ItemType Directory -Force $migrationOutput | Out-Null
Push-Location $backendDir
try {
    dotnet tool restore
    if ($LASTEXITCODE) { throw "dotnet tool restore failed." }
    # idempotent：可對任何版本的既有 DB 重複執行，只套用尚未套用的 migration
    dotnet ef migrations script --idempotent `
        --project $apiProject `
        --configuration $Configuration `
        --output (Join-Path $migrationOutput "rental-manager-schema.idempotent.sql")
    if ($LASTEXITCODE) { throw "EF migration script failed." }
    if ($EfBundle) {
        dotnet ef migrations bundle `
            --project $apiProject `
            --configuration $Configuration --self-contained --runtime $Runtime `
            --output (Join-Path $migrationOutput "efbundle") --force
        if ($LASTEXITCODE) { throw "EF migration bundle failed." }
    }
}
finally {
    Pop-Location
}
Write-Host "Backend: $backendOutput (version $Version, $Runtime)"
