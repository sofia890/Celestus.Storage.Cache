# Celestus.Storage.Cache

A high-performance, thread-safe caching library for .NET 9 applications with automatic cleanup, persistence support, and timeout handling.

[![NuGet](https://img.shields.io/nuget/v/Celestus.Storage.Cache.svg)](https://www.nuget.org/packages/Celestus.Storage.Cache/)
[![.NET](https://img.shields.io/badge/.NET-9.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/9.0)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

## 🚀 Features

- **Thread-Safe**: Built with `ReaderWriterLockSlim` for optimal read/write performance.
- **Automatic Cleanup**: Configurable background cleanup of expired entries.
- **Persistence Support**: Save and load cache data to/from files.
- **Timeout Handling**: Configurable timeouts for cache operations.
- **Generic Support**: Store any data type, data must be serializable for persistence.
- **Weak References**: Memory-efficient cache management.
- **Source Generators**: Automatic caching method generation with attributes.
- **Factory Pattern**: Centralized cache management and sharing.

## 📦 Installation

Install via NuGet Package Manager:

```bash
dotnet add package Celestus.Storage.Cache
```

Or via Package Manager Console:

```powershell
Install-Package Celestus.Storage.Cache
```

## 🏁 Quick Start

### Basic Usage

```csharp
using Celestus.Storage.Cache;

// Create a new thread-safe cache
using var cache = new ThreadCache();

// Store data with optional expiration
cache.Set("user:123", new User { Name = "John", Age = 30 }, TimeSpan.FromMinutes(10));

// Retrieve data
var user = cache.Get<User>("user:123");

// Try to retrieve (safer approach)
if (cache.TryGet<User>("user:123") is (true, var userData))
{
    Console.WriteLine($"Welcome back, {userData.Name}!");
}
```

### Factory Pattern for Shared Caches

```csharp
// Get or create a shared cache instance
var sharedCache = ThreadCache.Factory.GetOrCreateShared("user-cache");

// Use across different parts of your application
var userCache = ThreadCache.Factory.GetOrCreateShared("user-cache"); // Same instance
```

### Persistence Support

```csharp
// Create a persistent cache
using var cache = new ThreadCache("my-cache", persistent: true, persistentStorageLocation: "./cache");

// Cache automatically saves to disk when instance is disposed or finalized and loads on startup
cache.Set("important-data", "This will survive application restarts");

// Manual save/load operations
cache.TrySaveToFile(new Uri("./my-cache-backup.json"));
cache.TryLoadFromFile(new Uri("./my-cache-backup.json"));
```

### With Timeout Operations

```csharp
using var cache = new ThreadCache();

// Set with timeout (useful in high-concurrency scenarios)
bool success = cache.TrySet("key", "value", 
                            duration: TimeSpan.FromMinutes(5), 
                            timeout: TimeSpan.FromSeconds(1));

// Get with timeout
var (found, data) = cache.TryGet<string>("key", timeout: TimeSpan.FromSeconds(1));
```

## 🔧 Advanced Usage

### Custom Cleanup Intervals

```csharp
// Create cache with custom cleanup interval
using var cache = new ThreadCache(cleaningInterval: TimeSpan.FromMinutes(30));

// Or modify existing factory settings
ThreadCache.Factory.SetCleanupInterval(TimeSpan.FromMinutes(15));

### Source Generator Support

Add the `Celestus.Storage.Cache.Attributes` package and use attributes for automatic caching:

```csharp
using Celestus.Storage.Cache.Attributes;

[Cache(key: "expensive-calculations", durationInMs: 300000)] // 5 minutes
public partial class CalculationService
{
    [Cache(durationInMs: 60000)] // 1 minute
    public int ExpensiveCalculation(int input)
    {
        // Expensive operation here
        Thread.Sleep(1000);
        return input * input;
    }
}

// Generated code provides:
// - ExpensiveCalculationCached() method
// - Automatic cache key generation
// - Built-in cache management
```

## 🏗️ Architecture

### Core Components

- **`ThreadCache`**: Main thread-safe cache implementation
- **`Cache`**: Base cache without thread safety (for single-threaded scenarios)
- **`CacheBase<T>`**: Abstract base class for all cache implementations
- **`CacheEntry`**: Internal cache entry with expiration metadata
- **`CacheManager`**: Factory for managing shared cache instances


## ⚙️ Configuration Options

### Constructor Options

```csharp
// Basic cache
var cache1 = new ThreadCache();

// Named cache with key
var cache2 = new ThreadCache("user-sessions");

// With custom cleaner interval
var cache3 = new ThreadCache(cleaningInterval: TimeSpan.FromMinutes(10));

// Persistent cache
var cache4 = new ThreadCache("persistent-cache", 
    persistent: true, 
    persistentStorageLocation: "./data/cache");

// With custom cleaner
var cache5 = new ThreadCache("custom-cache", new MyCacheCleaner());
```

### Factory Configuration

```csharp
// Set global cleanup interval
ThreadCache.Factory.SetCleanupInterval(TimeSpan.FromMinutes(5));

// Set lock timeout for factory operations
ThreadCache.Factory.SetLockTimeoutInterval(TimeSpan.FromSeconds(10));
```

## 🔒 Thread Safety

Celestus.Storage.Cache uses `ReaderWriterLockSlim` to provide:

- **Multiple concurrent readers** for GET operations
- **Exclusive access for writers** for SET/REMOVE operations
- **Configurable timeouts** to prevent deadlocks

## 📊 Performance Characteristics

- **O(1)** average case for GET/SET operations
- **Memory efficient** with weak reference cleanup
- **Lock-free reads** when no writes are occurring
- **Configurable cleanup** to balance memory vs. CPU usage
- **Minimal allocations** for cache hits

## 🧪 Testing

The project includes comprehensive test suites:

- **Unit Tests**: Core functionality testing
- **Concurrency Tests**: Thread safety validation
- **Performance Tests**: Benchmarking and optimization
- **Integration Tests**: End-to-end scenarios

Run tests:

```bash
dotnet test
```

## 📝 Examples

### Web API Caching

```csharp
[ApiController]
public class UsersController : ControllerBase
{
    private static readonly ThreadCache _cache = ThreadCache.Factory.GetOrCreateShared("users");

    [HttpGet("{id}")]
    public async Task<User> GetUser(int id)
    {
        var cacheKey = $"user:{id}";
        
        if (_cache.TryGet<User>(cacheKey) is (true, var user))
        {
            return user;
        }
        else
        {
            user = await _userService.GetUserAsync(id);
            _cache.Set(cacheKey, user, TimeSpan.FromMinutes(10));
        
            return user;
        }
    }
}
```

## Development Setup

1. Clone the repository
2. Install .NET 9 SDK
3. Run `dotnet restore`
4. Run `dotnet build`
5. Run `dotnet test`

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🔗 Dependencies

- **Celestus.Exceptions**: Exception handling utilities
- **Celestus.Io**: File I/O operations
- **Celestus.Serialization**: JSON serialization support
- **.NET 9**: Target framework

## 📞 Support

- **Issues**: [GitHub Issues](https://github.com/sofia890/Celestus.Storage.Cache/issues)
- **Documentation**: [Wiki](https://github.com/sofia890/Celestus.Storage.Cache/wiki)
- **NuGet**: [Package Page](https://www.nuget.org/packages/Celestus.Storage.Cache/)
