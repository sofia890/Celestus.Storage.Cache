@echo off
REM Publish Celestus.Storage.Cache NuGet Packages
REM Usage: publish-packages.bat "your-api-key-here"

if "%~1"=="" (
    echo Error: API key is required
    echo Usage: publish-packages.bat "your-api-key-here"
    exit /b 1
)

set API_KEY=%~1
set SOURCE=https://api.nuget.org/v3/index.json

echo Publishing Celestus.Storage.Cache packages to NuGet.org...

echo Publishing Celestus.Exceptions.1.0.1.nupkg...
dotnet nuget push "nupkg\Celestus.Exceptions.1.0.1.nupkg" --api-key "%API_KEY%" --source "%SOURCE%" --skip-duplicate

echo Publishing Celestus.Io.1.0.1.nupkg...
dotnet nuget push "nupkg\Celestus.Io.1.0.1.nupkg" --api-key "%API_KEY%" --source "%SOURCE%" --skip-duplicate

echo Publishing Celestus.Serialization.1.0.1.nupkg...
dotnet nuget push "nupkg\Celestus.Serialization.1.0.1.nupkg" --api-key "%API_KEY%" --source "%SOURCE%" --skip-duplicate

echo Publishing Celestus.Storage.Cache.1.0.1.nupkg...
dotnet nuget push "nupkg\Celestus.Storage.Cache.1.0.1.nupkg" --api-key "%API_KEY%" --source "%SOURCE%" --skip-duplicate

echo Package publishing completed!