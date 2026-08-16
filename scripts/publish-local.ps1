param(
  [string]$Configuration = "Release",
  [string]$ApiBaseUrl = "/api"
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
# build/ 同時是「部署設定來源」與「建置產物輸出」目錄：
#   - 設定檔（docker-compose.yml / .env.example / nginx.conf / docker/）為 git 追蹤的來源
#   - api/ web/ seed/ 為本腳本產生的成品，已於 .gitignore 忽略
$buildRoot = Join-Path $root "build"
$apiProject = Join-Path $root "src\backend\RentalManager.Api\RentalManager.Api.csproj"
$frontendDir = Join-Path $root "src\frontend\rental-manager-web"
$apiOutput = Join-Path $buildRoot "api\publish"
$webOutput = Join-Path $buildRoot "web"
$seedSource = Join-Path $root "src\backend\RentalManager.Api\Seed"
$seedTarget = Join-Path $buildRoot "seed"

Write-Host "Cleaning generated artifact directories (keeps committed deploy config)..."
# 只清理產生的頂層子資料夾，保留 build/ 內的設定檔來源（docker-compose.yml / .env.example / nginx.conf / docker/）
foreach ($generated in @("api", "web", "seed")) {
  $target = Join-Path $buildRoot $generated
  if (Test-Path $target) {
    Remove-Item -Recurse -Force $target
  }
}
New-Item -ItemType Directory -Force -Path $apiOutput | Out-Null
New-Item -ItemType Directory -Force -Path $webOutput | Out-Null
New-Item -ItemType Directory -Force -Path $seedTarget | Out-Null

Write-Host "Publishing backend..."
dotnet publish $apiProject -c $Configuration -o $apiOutput /p:UseAppHost=false
if ($LASTEXITCODE -ne 0) {
  throw "Backend publish failed."
}

Write-Host "Building frontend..."
Push-Location $frontendDir
try {
  npm ci
  if ($LASTEXITCODE -ne 0) {
    throw "npm ci failed."
  }

  $env:VITE_API_BASE_URL = $ApiBaseUrl
  npm run build
  if ($LASTEXITCODE -ne 0) {
    throw "Frontend build failed."
  }
} finally {
  Remove-Item Env:VITE_API_BASE_URL -ErrorAction SilentlyContinue
  Pop-Location
}

$distDir = Join-Path $frontendDir "dist"
$webDist = Join-Path $webOutput "dist"
if (Test-Path $webDist) {
  Remove-Item -Recurse -Force $webDist
}
Copy-Item -Recurse -Force $distDir $webDist
Copy-Item -Recurse -Force (Join-Path $seedSource "*") $seedTarget

Write-Host ""
Write-Host "Deployment bundle ready:"
Write-Host "  Build:  $buildRoot"
Write-Host "  API:    $apiOutput"
Write-Host "  Web:    $webDist"
Write-Host "  Seed:   $seedTarget"
Write-Host ""
Write-Host "Next step:"
Write-Host "  cd build"
Write-Host "  docker compose up -d --build"
