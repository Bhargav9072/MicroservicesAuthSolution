<#
.SYNOPSIS
    Creates the .sln file, adds every project to it, and restores NuGet packages.
    Run this ONCE after extracting the solution, from the solution root folder
    (the folder containing this script), using PowerShell on Windows.

.EXAMPLE
    cd C:\Users\AyushPanchal\Desktop\MicroservicesAuthSolution
    .\setup.ps1
#>

Write-Host "==> Creating solution file..." -ForegroundColor Cyan
dotnet new sln -n MicroservicesAuthSolution --force

Write-Host "==> Adding projects to solution..." -ForegroundColor Cyan
Get-ChildItem -Recurse -Filter *.csproj | ForEach-Object {
    dotnet sln add $_.FullName
}

Write-Host "==> Restoring NuGet packages for all projects..." -ForegroundColor Cyan
dotnet restore

Write-Host ""
Write-Host "==> Installing/updating EF Core CLI tool (needed for migrations)..." -ForegroundColor Cyan
dotnet tool install --global dotnet-ef 2>$null
dotnet tool update --global dotnet-ef 2>$null

Write-Host ""
Write-Host "Setup complete!" -ForegroundColor Green
Write-Host "Next steps:"
Write-Host "  1. Make sure SQL Server is running (local instance, LocalDB, or via docker-compose)."
Write-Host "  2. Create the initial EF Core migrations (see README.md 'Database Setup' section)."
Write-Host "  3. Run each service with 'dotnet run' or use docker-compose up --build."
