# Start the full CineVision backend (SQL + RabbitMQ + API) with one command.
# Usage (from this folder):
#   .\start.ps1
#   .\start.ps1 -Build   # force image rebuild

param(
    [switch]$Build
)

$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    Write-Error "Docker is not installed or not on PATH. Install Docker Desktop first."
}

Write-Host "Starting CineVision stack (SQL Server + RabbitMQ + API)..." -ForegroundColor Cyan

$composeArgs = @("compose", "up", "-d", "--build")
& docker @composeArgs
if ($LASTEXITCODE -ne 0) {
    Write-Error "docker compose failed. Is Docker Desktop running?"
}

Write-Host ""
Write-Host "Waiting for API health at http://localhost:5126/health ..." -ForegroundColor Yellow
$ready = $false
for ($i = 1; $i -le 60; $i++) {
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:5126/health" -UseBasicParsing -TimeoutSec 3
        if ($response.StatusCode -eq 200) {
            $ready = $true
            break
        }
    } catch {
        Start-Sleep -Seconds 3
    }
}

if (-not $ready) {
    Write-Host "API is not healthy yet. Check logs with: docker compose logs -f ecomm-fit-2026-api" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Backend is up." -ForegroundColor Green
Write-Host "  API:            http://localhost:5126"
Write-Host "  Swagger/Scalar: http://localhost:5126/swagger  (or /scalar)"
Write-Host "  Health:         http://localhost:5126/health"
Write-Host "  RabbitMQ UI:    http://localhost:15672  (guest / guest)"
Write-Host "  SQL Server:     localhost,1435  (sa / see .env or default qweasd123!)"
Write-Host ""
Write-Host "Flutter apps still run on your PC (not in Docker):" -ForegroundColor Cyan
Write-Host "  Desktop:  cd UI\ecommerce_desktop ; flutter run -d windows"
Write-Host "  Mobile:   cd UI\ecommerce_mobile  ; flutter run"
Write-Host ""
Write-Host "Stop everything:  docker compose down"
