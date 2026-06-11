param(
  [string]$Configuration = "Release",
  [string]$ApiBaseUrl = "/api"
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$deployRoot = Join-Path $root "deploy"
$apiProject = Join-Path $root "src\backend\RentalManager.Api\RentalManager.Api.csproj"
$frontendDir = Join-Path $root "src\frontend\rental-manager-web"
$publishRoot = Join-Path $root "publish"
$apiOutput = Join-Path $publishRoot "api\publish"
$webOutput = Join-Path $publishRoot "web"
$seedSource = Join-Path $root "src\backend\RentalManager.Api\Seed"
$seedTarget = Join-Path $publishRoot "seed"
$nginxSource = Join-Path $deployRoot "nginx.conf"
$nginxTarget = Join-Path $publishRoot "nginx.conf"

Write-Host "Preparing artifact directories..."
if (Test-Path $publishRoot) {
  Get-ChildItem -Force $publishRoot | Remove-Item -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $publishRoot | Out-Null
New-Item -ItemType Directory -Force -Path $apiOutput | Out-Null
New-Item -ItemType Directory -Force -Path $webOutput | Out-Null
New-Item -ItemType Directory -Force -Path $seedTarget | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $publishRoot "docker") | Out-Null

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
Copy-Item -Force (Join-Path $deployRoot "docker-compose.yml") (Join-Path $publishRoot "docker-compose.yml")
Copy-Item -Force (Join-Path $deployRoot ".env.example") (Join-Path $publishRoot ".env.example")
Copy-Item -Recurse -Force (Join-Path $deployRoot "docker\*") (Join-Path $publishRoot "docker")
Copy-Item -Force $nginxSource $nginxTarget

Write-Host ""
Write-Host "Deployment bundle ready:"
Write-Host "  Publish: $publishRoot"
Write-Host "  API: $apiOutput"
Write-Host "  Web: $webDist"
Write-Host "  Seed: $seedTarget"
Write-Host ""
Write-Host "Next step:"
Write-Host "  cd publish"
Write-Host "  docker compose up -d --build"
