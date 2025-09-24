# Celestus.Storage.Cache NuGet Package Publishing

This document describes how to create and publish the Celestus.Storage.Cache NuGet packages.

## Package Structure

The solution contains the following packages:

1. **Celestus.Exceptions** - Exception handling library
2. **Celestus.Io** - Input/Output utilities  
3. **Celestus.Serialization** - Serialization utilities
4. **Celestus.Storage.Cache** - Main caching library (depends on the above)

## Prerequisites

1. .NET 9 SDK installed
2. Valid NuGet.org API key with push permissions
3. PowerShell (for the automated script)

## Getting a NuGet API Key

1. Go to [nuget.org](https://www.nuget.org) and sign in
2. Click on your username ? API Keys
3. Create a new API key with push permissions
4. Set the appropriate package glob pattern (e.g., `Celestus.*`)

## Building Packages

The packages are automatically built when you build the solution in Release mode:

```bash
dotnet build --configuration Release
```

This will generate both the main packages (.nupkg) and symbol packages (.snupkg) in the `nupkg` folder.

## Publishing Packages

### Option 1: Automated Script (Recommended)

Use the PowerShell script to publish all packages in the correct order:

```powershell
.\publish-packages.ps1 -ApiKey "your-api-key-here"
```

Or use the batch file:

```cmd
publish-packages.bat "your-api-key-here"
```

### Option 2: Manual Publishing

Publish packages individually in dependency order:

```bash
# Publish dependencies first
dotnet nuget push "nupkg\Celestus.Exceptions.1.0.1.nupkg" --api-key "your-api-key" --source https://api.nuget.org/v3/index.json
dotnet nuget push "nupkg\Celestus.Io.1.0.1.nupkg" --api-key "your-api-key" --source https://api.nuget.org/v3/index.json  
dotnet nuget push "nupkg\Celestus.Serialization.1.0.1.nupkg" --api-key "your-api-key" --source https://api.nuget.org/v3/index.json

# Then publish the main package
dotnet nuget push "nupkg\Celestus.Storage.Cache.1.0.1.nupkg" --api-key "your-api-key" --source https://api.nuget.org/v3/index.json
```

## Package Versions

Current version: **1.0.1**

To update versions, modify the `<Version>` property in each project file and rebuild.

## Troubleshooting

### API Key Issues

If you get a 403 Forbidden error:
- Verify your API key is correct
- Check that the API key has push permissions
- Ensure the API key hasn't expired
- Verify the package glob pattern includes your packages

### Package Already Exists

If you get an error that the package already exists:
- Increment the version number in the project files
- Rebuild the solution
- Publish the new version

### Symbol Package Issues

Symbol packages (.snupkg) are automatically generated and pushed with the main packages. If you don't want symbol packages, remove these properties from the project files:
- `<IncludeSymbols>true</IncludeSymbols>`
- `<SymbolPackageFormat>snupkg</SymbolPackageFormat>`

## Package Metadata

The packages include the following metadata:
- **License**: MIT
- **Repository**: https://github.com/sofia890/Celestus.Storage.Cache
- **Tags**: cache, caching, thread-safe, storage, performance, cleanup, timeout
- **Author**: Sofia
- **Company**: Celestus

## Usage

Once published, users can install the main package:

```bash
dotnet add package Celestus.Storage.Cache
```

The dependencies will be automatically installed.