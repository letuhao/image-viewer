# 📏 Coding Standards & Best Practices - ImageViewer Platform

**Ngày tạo:** 2025-01-03  
**Version:** 1.0.0  
**Mục tiêu:** Định nghĩa coding standards và best practices cho ImageViewer Platform

---

## 📋 Overview

Tài liệu này định nghĩa các coding standards và best practices mà tất cả developers phải tuân thủ khi làm việc với ImageViewer Platform. Các standards này dựa trên industry best practices và đặc thù của dự án.

---

## 🏗️ Architecture Standards

### **Clean Architecture Compliance**
```csharp
// ✅ Good: Proper layer separation
src/
├── ImageViewer.Domain/          # Business logic only
├── ImageViewer.Application/     # Use cases & services
├── ImageViewer.Infrastructure/  # External concerns
└── ImageViewer.Api/            # Presentation layer
```

**Rules:**
- ✅ Domain layer không được depend vào bất kỳ layer nào khác
- ✅ Application layer chỉ được depend vào Domain layer
- ✅ Infrastructure layer có thể depend vào Domain và Application
- ✅ API layer có thể depend vào tất cả các layer khác

### **Dependency Injection**
```csharp
// ✅ Good: Constructor injection
public class CollectionService : ICollectionService
{
    private readonly ICollectionRepository _repository;
    private readonly ILogger<CollectionService> _logger;

    public CollectionService(
        ICollectionRepository repository,
        ILogger<CollectionService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
}

// ❌ Bad: Service locator pattern
public class CollectionService : ICollectionService
{
    public async Task<Collection> GetCollectionAsync(ObjectId id)
    {
        var repository = ServiceLocator.GetService<ICollectionRepository>();
        return await repository.GetByIdAsync(id);
    }
}
```

---

## 📝 Naming Conventions

### **Classes and Interfaces**
```csharp
// ✅ Good: PascalCase for classes
public class CollectionService : ICollectionService
public class UserRepository : IUserRepository
public class JwtAuthenticationMiddleware

// ✅ Good: Interface prefix "I"
public interface ICollectionService
public interface IUserRepository
public interface IAuthenticationService
```

### **Methods and Properties**
```csharp
// ✅ Good: PascalCase for public members
public async Task<Collection> CreateCollectionAsync(ObjectId libraryId, string name)
public string Name { get; private set; }
public bool IsActive { get; private set; }

// ✅ Good: camelCase for parameters and local variables
public async Task<Collection> CreateCollectionAsync(ObjectId libraryId, string collectionName)
{
    var existingCollection = await _repository.GetByNameAsync(collectionName);
    var newCollection = new Collection(libraryId, collectionName, path, type);
    return await _repository.CreateAsync(newCollection);
}
```

### **Private Fields**
```csharp
// ✅ Good: Underscore prefix for private fields
private readonly ICollectionRepository _repository;
private readonly ILogger<CollectionService> _logger;
private readonly IMongoDatabase _database;
```

### **Constants and Enums**
```csharp
// ✅ Good: PascalCase for constants
public const int DefaultPageSize = 20;
public const string DefaultCollectionType = "Image";

// ✅ Good: PascalCase for enum values
public enum CollectionType
{
    Image,
    Video,
    Document,
    Mixed
}
```

---

## 🔧 Code Structure Standards

### **File Organization**
```csharp
// ✅ Good: One class per file
// File: CollectionService.cs
namespace ImageViewer.Application.Services;

public class CollectionService : ICollectionService
{
    // Implementation
}

// File: ICollectionService.cs
namespace ImageViewer.Application.Services;

public interface ICollectionService
{
    // Interface definition
}
```

### **Using Statements**
```csharp
// ✅ Good: Organize using statements
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

using Microsoft.Extensions.Logging;
using MongoDB.Bson;

using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Domain.Exceptions;
```

### **Method Structure**
```csharp
// ✅ Good: Well-structured method
public async Task<Collection> CreateCollectionAsync(ObjectId libraryId, string name, string path, CollectionType type)
{
    // 1. Input validation
    if (string.IsNullOrWhiteSpace(name))
        throw new ArgumentException("Name cannot be null or empty", nameof(name));
    
    if (string.IsNullOrWhiteSpace(path))
        throw new ArgumentException("Path cannot be null or empty", nameof(path));

    try
    {
        // 2. Business logic
        var existingCollection = await _repository.GetByPathAsync(path);
        if (existingCollection != null)
            throw new DuplicateEntityException($"Collection at path '{path}' already exists");

        // 3. Create entity
        var collection = new Collection(libraryId, name, path, type);
        
        // 4. Persist
        return await _repository.CreateAsync(collection);
    }
    catch (Exception ex) when (!(ex is ValidationException || ex is DuplicateEntityException))
    {
        // 5. Error handling
        _logger.LogError(ex, "Failed to create collection with name {Name} at path {Path}", name, path);
        throw new BusinessRuleException($"Failed to create collection with name '{name}' at path '{path}'", ex);
    }
}
```

---

## 🛡️ Error Handling Standards

### **Exception Hierarchy**
```csharp
// ✅ Good: Use custom exception hierarchy
try
{
    var collection = await _service.CreateCollectionAsync(libraryId, name, path, type);
    return Ok(collection);
}
catch (ValidationException ex)
{
    return BadRequest(new { message = ex.Message });
}
catch (DuplicateEntityException ex)
{
    return Conflict(new { message = ex.Message });
}
catch (EntityNotFoundException ex)
{
    return NotFound(new { message = ex.Message });
}
catch (BusinessRuleException ex)
{
    return StatusCode(500, new { message = ex.Message });
}
catch (Exception ex)
{
    _logger.LogError(ex, "Unexpected error occurred");
    return StatusCode(500, new { message = "Internal server error" });
}
```

### **Input Validation**
```csharp
// ✅ Good: Validate inputs early
public async Task<Collection> UpdateCollectionAsync(ObjectId collectionId, UpdateCollectionRequest request)
{
    // Validate collection ID
    if (collectionId == ObjectId.Empty)
        throw new ValidationException("Collection ID cannot be empty");

    // Validate request
    if (request == null)
        throw new ArgumentNullException(nameof(request));

    // Validate individual properties
    if (request.Name != null && string.IsNullOrWhiteSpace(request.Name))
        throw new ValidationException("Collection name cannot be null or empty");

    // Continue with business logic...
}
```

---

## 📊 Logging Standards

### **Structured Logging**
```csharp
// ✅ Good: Structured logging with Serilog
_logger.LogInformation("Collection {CollectionId} created successfully for library {LibraryId}", 
    collection.Id, collection.LibraryId);

_logger.LogWarning("Failed login attempt for user {Username} from IP {IpAddress}", 
    username, ipAddress);

_logger.LogError(ex, "Failed to create collection with name {Name} at path {Path}", 
    name, path);

// ❌ Bad: String concatenation
_logger.LogInformation("Collection " + collection.Id + " created successfully");
```

### **Log Levels**
```csharp
// ✅ Good: Appropriate log levels
_logger.LogTrace("Entering method {MethodName} with parameters {@Parameters}", nameof(GetCollectionAsync), parameters);
_logger.LogDebug("Processing collection {CollectionId} with {ItemCount} items", collectionId, itemCount);
_logger.LogInformation("Collection {CollectionId} created successfully", collectionId);
_logger.LogWarning("Collection {CollectionId} has no images", collectionId);
_logger.LogError(ex, "Failed to process collection {CollectionId}", collectionId);
_logger.LogCritical(ex, "Database connection lost during critical operation");
```

---

## 🔐 Security Standards

### **Authentication and Authorization**
```csharp
// ✅ Good: Secure authentication
[Authorize]
[HttpPost]
public async Task<IActionResult> CreateCollection([FromBody] CreateCollectionRequest request)
{
    // User context is automatically available through [Authorize]
    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userId))
        return Unauthorized();

    // Continue with business logic...
}

// ✅ Good: Role-based authorization
[Authorize(Roles = "Admin,Manager")]
[HttpDelete("{id}")]
public async Task<IActionResult> DeleteCollection(string id)
{
    // Only Admin or Manager can delete collections
    // Implementation...
}
```

### **Input Sanitization**
```csharp
// ✅ Good: Sanitize inputs
public async Task<Collection> CreateCollectionAsync(string name, string path)
{
    // Sanitize inputs
    name = name?.Trim();
    path = path?.Trim();
    
    // Validate after sanitization
    if (string.IsNullOrWhiteSpace(name))
        throw new ValidationException("Name cannot be null or empty");
    
    // Continue with business logic...
}
```

### **Password Security**
```csharp
// ✅ Good: Secure password handling
public class PasswordService : IPasswordService
{
    private const int WorkFactor = 12;

    public string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be null or empty", nameof(password));

        return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hashedPassword))
            return false;

        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }
        catch
        {
            return false;
        }
    }
}
```

---

## 🗄️ Database Standards

### **MongoDB Best Practices**
```csharp
// ✅ Good: Proper MongoDB entity structure
public class Collection : BaseEntity
{
    [BsonElement("libraryId")]
    public ObjectId LibraryId { get; private set; }
    
    [BsonElement("name")]
    public string Name { get; private set; }
    
    [BsonElement("path")]
    public string Path { get; private set; }
    
    [BsonElement("type")]
    public CollectionType Type { get; private set; }
    
    [BsonElement("isActive")]
    public bool IsActive { get; private set; }

    // Private constructor for MongoDB
    private Collection() { }

    public Collection(ObjectId libraryId, string name, string path, CollectionType type)
    {
        LibraryId = libraryId;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Path = path ?? throw new ArgumentNullException(nameof(path));
        Type = type;
        IsActive = true;
    }
}
```

### **Repository Pattern**
```csharp
// ✅ Good: Repository implementation
public class CollectionRepository : ICollectionRepository
{
    private readonly IMongoCollection<Collection> _collection;

    public CollectionRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<Collection>("collections");
    }

    public async Task<Collection?> GetByIdAsync(ObjectId id)
    {
        try
        {
            return await _collection.Find(c => c.Id == id).FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            throw new RepositoryException($"Failed to get collection with ID '{id}'", ex);
        }
    }

    public async Task<Collection> CreateAsync(Collection collection)
    {
        try
        {
            await _collection.InsertOneAsync(collection);
            return collection;
        }
        catch (Exception ex)
        {
            throw new RepositoryException($"Failed to create collection with name '{collection.Name}'", ex);
        }
    }
}
```

---

## 🧪 Testing Standards

### **Unit Testing**
```csharp
// ✅ Good: Unit test structure
[Test]
public async Task CreateCollectionAsync_ValidInput_ReturnsCollection()
{
    // Arrange
    var libraryId = ObjectId.GenerateNewId();
    var name = "Test Collection";
    var path = "/test/path";
    var type = CollectionType.Image;
    
    var mockRepository = new Mock<ICollectionRepository>();
    mockRepository.Setup(r => r.GetByPathAsync(path))
        .ReturnsAsync((Collection?)null);
    mockRepository.Setup(r => r.CreateAsync(It.IsAny<Collection>()))
        .ReturnsAsync((Collection c) => c);
    
    var service = new CollectionService(mockRepository.Object, Mock.Of<ILogger<CollectionService>>());

    // Act
    var result = await service.CreateCollectionAsync(libraryId, name, path, type);

    // Assert
    Assert.That(result, Is.Not.Null);
    Assert.That(result.Name, Is.EqualTo(name));
    Assert.That(result.Path, Is.EqualTo(path));
    Assert.That(result.Type, Is.EqualTo(type));
    Assert.That(result.LibraryId, Is.EqualTo(libraryId));
    
    mockRepository.Verify(r => r.CreateAsync(It.IsAny<Collection>()), Times.Once);
}

[Test]
public async Task CreateCollectionAsync_DuplicatePath_ThrowsDuplicateEntityException()
{
    // Arrange
    var libraryId = ObjectId.GenerateNewId();
    var name = "Test Collection";
    var path = "/existing/path";
    var type = CollectionType.Image;
    
    var existingCollection = new Collection(libraryId, "Existing", path, type);
    var mockRepository = new Mock<ICollectionRepository>();
    mockRepository.Setup(r => r.GetByPathAsync(path))
        .ReturnsAsync(existingCollection);
    
    var service = new CollectionService(mockRepository.Object, Mock.Of<ILogger<CollectionService>>());

    // Act & Assert
    var exception = await Assert.ThrowsAsync<DuplicateEntityException>(
        () => service.CreateCollectionAsync(libraryId, name, path, type));
    
    Assert.That(exception.Message, Does.Contain("already exists"));
}
```

### **Integration Testing**
```csharp
// ✅ Good: Integration test structure
[Test]
public async Task CollectionsController_CreateCollection_ReturnsCreatedResult()
{
    // Arrange
    var client = _factory.CreateClient();
    var request = new CreateCollectionRequest
    {
        LibraryId = _testLibraryId.ToString(),
        Name = "Integration Test Collection",
        Path = "/integration/test/path",
        Type = "Image"
    };

    // Act
    var response = await client.PostAsJsonAsync("/api/v1/collections", request);

    // Assert
    Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
    
    var content = await response.Content.ReadAsStringAsync();
    var collection = JsonSerializer.Deserialize<Collection>(content, new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    });
    
    Assert.That(collection, Is.Not.Null);
    Assert.That(collection.Name, Is.EqualTo(request.Name));
}
```

---

## 📚 Documentation Standards

### **XML Documentation**
```csharp
/// <summary>
/// Creates a new collection in the specified library
/// </summary>
/// <param name="libraryId">The ID of the library to create the collection in</param>
/// <param name="name">The name of the collection</param>
/// <param name="path">The file system path of the collection</param>
/// <param name="type">The type of collection (Image, Video, Document, Mixed)</param>
/// <returns>The created collection</returns>
/// <exception cref="ValidationException">Thrown when input validation fails</exception>
/// <exception cref="DuplicateEntityException">Thrown when a collection already exists at the specified path</exception>
/// <exception cref="BusinessRuleException">Thrown when business rules are violated</exception>
public async Task<Collection> CreateCollectionAsync(ObjectId libraryId, string name, string path, CollectionType type)
{
    // Implementation...
}
```

### **API Documentation**
```csharp
/// <summary>
/// Create a new collection
/// </summary>
/// <param name="request">The collection creation request</param>
/// <returns>The created collection</returns>
/// <response code="201">Collection created successfully</response>
/// <response code="400">Invalid request data</response>
/// <response code="409">Collection already exists at the specified path</response>
/// <response code="500">Internal server error</response>
[HttpPost]
[ProducesResponseType(typeof(Collection), StatusCodes.Status201Created)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
public async Task<IActionResult> CreateCollection([FromBody] CreateCollectionRequest request)
{
    // Implementation...
}
```

---

## 🚀 Performance Standards

### **Async/Await Usage**
```csharp
// ✅ Good: Proper async/await usage
public async Task<Collection> GetCollectionAsync(ObjectId id)
{
    return await _repository.GetByIdAsync(id);
}

// ✅ Good: Async all the way
public async Task<IEnumerable<Collection>> GetCollectionsAsync(int page, int pageSize)
{
    var skip = (page - 1) * pageSize;
    return await _repository.FindAsync(
        Builders<Collection>.Filter.Empty,
        Builders<Collection>.Sort.Descending(c => c.CreatedAt),
        pageSize,
        skip
    );
}

// ❌ Bad: Blocking async calls
public Collection GetCollection(ObjectId id)
{
    return _repository.GetByIdAsync(id).Result; // Don't do this!
}
```

### **Memory Management**
```csharp
// ✅ Good: Proper disposal
public async Task ProcessCollectionAsync(ObjectId collectionId)
{
    using var scope = _serviceProvider.CreateScope();
    var repository = scope.ServiceProvider.GetRequiredService<ICollectionRepository>();
    
    var collection = await repository.GetByIdAsync(collectionId);
    // Process collection...
}

// ✅ Good: Using statements for disposable resources
public async Task<byte[]> GenerateThumbnailAsync(string imagePath)
{
    using var image = Image.Load(imagePath);
    using var thumbnail = image.Clone(x => x.Resize(150, 150));
    using var memoryStream = new MemoryStream();
    
    thumbnail.SaveAsPng(memoryStream);
    return memoryStream.ToArray();
}
```

---

## 🔧 Configuration Standards

### **Configuration Management**
```csharp
// ✅ Good: Strongly typed configuration
public class JwtOptions
{
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpiryHours { get; set; } = 24;
}

// ✅ Good: Configuration binding
public void ConfigureServices(IServiceCollection services)
{
    services.Configure<JwtOptions>(_configuration.GetSection("Jwt"));
    services.AddScoped<IJwtService, JwtService>();
}

// ✅ Good: Using IOptions pattern
public class JwtService : IJwtService
{
    private readonly JwtOptions _options;

    public JwtService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }
}
```

### **Environment Variables**
```csharp
// ✅ Good: Environment-specific configuration
public void ConfigureServices(IServiceCollection services)
{
    var connectionString = Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING")
        ?? _configuration.GetConnectionString("MongoDB");
    
    services.AddMongoDb(connectionString);
}

// ❌ Bad: Hardcoded values
public void ConfigureServices(IServiceCollection services)
{
    services.AddMongoDb("mongodb://localhost:27017/imageviewer"); // Don't do this!
}
```

---

## 🚫 Anti-Patterns to Avoid

### **Code Smells**
```csharp
// ❌ Bad: God classes
public class ImageViewerService
{
    public async Task<Collection> CreateCollectionAsync() { /* ... */ }
    public async Task<Image> ProcessImageAsync() { /* ... */ }
    public async Task<User> CreateUserAsync() { /* ... */ }
    public async Task<Tag> CreateTagAsync() { /* ... */ }
    // 100+ methods...
}

// ❌ Bad: Long methods
public async Task<Collection> CreateCollectionAsync(ObjectId libraryId, string name, string path, CollectionType type)
{
    // 200+ lines of code...
}

// ❌ Bad: Deep nesting
public async Task<Collection> CreateCollectionAsync(ObjectId libraryId, string name, string path, CollectionType type)
{
    if (libraryId != ObjectId.Empty)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            if (!string.IsNullOrWhiteSpace(path))
            {
                if (type != CollectionType.None)
                {
                    // Actual implementation buried deep...
                }
            }
        }
    }
}
```

### **Security Anti-Patterns**
```csharp
// ❌ Bad: SQL injection vulnerability
public async Task<Collection> GetCollectionByNameAsync(string name)
{
    var query = $"SELECT * FROM Collections WHERE Name = '{name}'"; // Vulnerable!
    return await _connection.QueryAsync<Collection>(query);
}

// ❌ Bad: Hardcoded secrets
public class JwtService
{
    private const string SECRET_KEY = "my-secret-key"; // Don't do this!
    
    public string GenerateToken(User user)
    {
        // Implementation...
    }
}

// ❌ Bad: No input validation
public async Task<Collection> CreateCollectionAsync(string name, string path)
{
    // No validation - dangerous!
    var collection = new Collection(name, path);
    return await _repository.CreateAsync(collection);
}
```

---

## 📋 Code Review Checklist

### **Before Submitting PR:**
- [ ] ✅ All tests passing
- [ ] ✅ Code follows naming conventions
- [ ] ✅ No hardcoded values
- [ ] ✅ Proper error handling
- [ ] ✅ Input validation implemented
- [ ] ✅ Logging added where appropriate
- [ ] ✅ XML documentation added
- [ ] ✅ No security vulnerabilities
- [ ] ✅ Performance considerations addressed
- [ ] ✅ Code is readable and maintainable

### **During Code Review:**
- [ ] ✅ Architecture compliance
- [ ] ✅ Security best practices
- [ ] ✅ Error handling completeness
- [ ] ✅ Test coverage adequate
- [ ] ✅ Performance implications
- [ ] ✅ Documentation quality
- [ ] ✅ Code reusability
- [ ] ✅ Maintainability

---

## 🎯 Quality Metrics

### **Code Quality Targets:**
- **Cyclomatic Complexity:** < 8 per method
- **Lines of Code:** < 50 per method
- **Test Coverage:** > 90%
- **Code Duplication:** < 3%
- **Technical Debt Ratio:** < 5%

### **Performance Targets:**
- **API Response Time:** < 200ms (95th percentile)
- **Database Query Time:** < 100ms
- **Memory Usage:** < 500MB per service
- **CPU Usage:** < 70% under normal load

---

*Coding Standards & Best Practices created on 2025-01-03*  
*Next review: Quarterly*
