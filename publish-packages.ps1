# Publish Celestus.Storage.Cache NuGet Packages
# Usage: .\publish-packages.ps1 -ApiKey "your-api-key-here"

param(
    [Parameter(Mandatory=$true)]
    [string]$ApiKey,
    [string]$Source = "https://api.nuget.org/v3/index.json"
)

# Array of packages in dependency order
$packages = @(
    "Celestus.Exceptions.1.0.1.nupkg",
    "Celestus.Io.1.0.1.nupkg", 
    "Celestus.Serialization.1.0.1.nupkg",
    "Celestus.Storage.Cache.1.0.1.nupkg"
)

Write-Host "Publishing Celestus.Storage.Cache packages to NuGet.org..." -ForegroundColor Green

foreach ($package in $packages) {
    $packagePath = "nupkg\$package"
    
    if (Test-Path $packagePath) {
        Write-Host "Publishing $package..." -ForegroundColor Yellow
        
        try {
            dotnet nuget push $packagePath --api-key $ApiKey --source $Source --skip-duplicate
            Write-Host "? Successfully published $package" -ForegroundColor Green
        }
        catch {
            Write-Error "? Failed to publish $package"
            Write-Error $_.Exception.Message
        }
    } else {
        Write-Warning "Package not found: $packagePath"
    }
}

Write-Host "Package publishing completed!" -ForegroundColor Green