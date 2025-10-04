# Development Guidelines - ImageViewer Platform

## 📋 Overview

This document provides comprehensive development guidelines for the ImageViewer Platform implementation, ensuring consistency, quality, and maintainability across the entire development process.

## 🎯 Development Principles

### 1. Clean Code Principles
- **Readable Code**: Write code that is self-documenting and easy to understand
- **Simple Design**: Keep solutions simple and avoid over-engineering
- **Consistent Style**: Follow established coding standards and conventions
- **Meaningful Names**: Use descriptive names for variables, methods, and classes
- **Small Functions**: Keep functions small and focused on single responsibility

### 2. SOLID Principles
- **Single Responsibility**: Each class should have only one reason to change
- **Open/Closed**: Open for extension, closed for modification
- **Liskov Substitution**: Derived classes must be substitutable for base classes
- **Interface Segregation**: Clients should not depend on interfaces they don't use
- **Dependency Inversion**: Depend on abstractions, not concretions

### 3. Domain-Driven Design (DDD)
- **Ubiquitous Language**: Use consistent terminology across code and documentation
- **Bounded Contexts**: Define clear boundaries between different domains
- **Aggregates**: Design aggregates to maintain consistency boundaries
- **Value Objects**: Use value objects for concepts without identity
- **Domain Events**: Use events for inter-aggregate communication

## 🏗️ Project Structure

### Solution Structure
```
ImageViewer.sln
├── src/
│   ├── ImageViewer.Domain/           # Domain layer
│   ├── ImageViewer.Application/      # Application layer
│   ├── ImageViewer.Infrastructure/   # Infrastructure layer
│   ├── ImageViewer.Api/              # Presentation layer
│   └── ImageViewer.Worker/           # Background services
├── tests/
│   ├── ImageViewer.Tests/            # Unit tests
│   └── ImageViewer.IntegrationTests/ # Integration tests
└── docs/                             # Documentation
```

### Domain Layer Structure
```
ImageViewer.Domain/
├── Entities/                         # Domain entities
├── ValueObjects/                     # Value objects
├── Interfaces/                       # Domain interfaces
├── Events/                          # Domain events
├── Enums/                           # Domain enums
└── Exceptions/                      # Domain exceptions
```

### Application Layer Structure
```
ImageViewer.Application/
├── Services/                        # Application services
├── DTOs/                           # Data transfer objects
├── Commands/                       # Command objects
├── Queries/                        # Query objects
├── Handlers/                       # Command/query handlers
├── Mappings/                       # Object mappings
└── Extensions/                     # Extension methods
```

### Infrastructure Layer Structure
```
ImageViewer.Infrastructure/
├── Data/                           # Data access
│   ├── Contexts/                   # Database contexts
│   ├── Repositories/               # Repository implementations
│   └── Migrations/                 # Database migrations
├── Services/                       # Infrastructure services
├── Configuration/                  # Configuration classes
└── Extensions/                     # Service extensions
```

## 📝 Coding Standards

### Naming Conventions

#### Classes and Interfaces
```csharp
// Good
public class CollectionService : ICollectionService
public interface IUserRepository
public class UserRegistrationCommand

// Bad
public class collectionService
public interface IuserRepository
public class user_registration_command
```

#### Methods and Properties
```csharp
// Good
public async Task<Collection> GetCollectionAsync(ObjectId id)
public bool IsEnabled { get; set; }
public void UpdateName(string newName)

// Bad
public async Task<Collection> getCollection(ObjectId id)
public bool enabled { get; set; }
public void update_name(string newName)
```

#### Variables and Parameters
```csharp
// Good
var collectionId = ObjectId.GenerateNewId();
var userSettings = await GetUserSettingsAsync(userId);

// Bad
var collection_id = ObjectId.GenerateNewId();
var user_settings = await GetUserSettingsAsync(user_id);
```

### File Organization

#### One Class Per File
```csharp
// File: CollectionService.cs
public class CollectionService : ICollectionService
{
    // Implementation
}

// File: ICollectionService.cs
public interface ICollectionService
{
    // Interface definition
}
```

#### Namespace Organization
```csharp
namespace ImageViewer.Domain.Entities;
namespace ImageViewer.Domain.Interfaces;
namespace ImageViewer.Application.Services;
namespace ImageViewer.Infrastructure.Data;
```

## 🔧 Development Tools

### Required Tools
- **Visual Studio 2022** or **VS Code** with C# extension
- **.NET 8 SDK**
- **MongoDB Compass** for database management
- **RabbitMQ Management UI** for message queue monitoring
- **Git** for version control
- **Postman** or **Insomnia** for API testing

### Recommended Extensions
- **C# Dev Kit** (VS Code)
- **MongoDB for VS Code**
- **REST Client** (VS Code)
- **GitLens** (VS Code)
- **SonarLint** for code quality
- **Auto Rename Tag** (VS Code)

### Development Environment Setup
```bash
# Install .NET 8 SDK
dotnet --version

# Install MongoDB
# Download from https://www.mongodb.com/try/download/community

# Install RabbitMQ
# Download from https://www.rabbitmq.com/download.html

# Clone repository
git clone <repository-url>
cd image-viewer

# Restore packages
dotnet restore

# Build solution
dotnet build

# Run tests
dotnet test
```

## 🧪 Testing Guidelines

### Unit Testing
```csharp
[TestClass]
public class CollectionServiceTests
{
    private Mock<ICollectionRepository> _mockRepository;
    private CollectionService _service;
    
    [TestInitialize]
    public void Setup()
    {
        _mockRepository = new Mock<ICollectionRepository>();
        _service = new CollectionService(_mockRepository.Object);
    }
    
    [TestMethod]
    public async Task CreateCollectionAsync_ValidRequest_ReturnsCollection()
    {
        // Arrange
        var request = new CreateCollectionRequest
        {
            Name = "Test Collection",
            Path = "/test/path",
            Type = CollectionType.Image
        };
        
        // Act
        var result = await _service.CreateCollectionAsync(request);
        
        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(request.Name, result.Name);
    }
}
```

### Integration Testing
```csharp
[TestClass]
public class CollectionControllerIntegrationTests : IntegrationTestBase
{
    [TestMethod]
    public async Task GetCollections_ValidRequest_ReturnsOk()
    {
        // Arrange
        var request = new GetCollectionsRequest { Page = 1, Limit = 10 };
        
        // Act
        var response = await Client.GetAsync("/api/v1/collections?page=1&limit=10");
        
        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }
}
```

### Test Naming Convention
- **MethodName_Scenario_ExpectedResult**
- Use descriptive names that explain the test case
- Include the method being tested, the scenario, and expected outcome

## 📊 Code Quality

### Code Review Checklist
- [ ] **Functionality**: Does the code work as expected?
- [ ] **Readability**: Is the code easy to understand?
- [ ] **Performance**: Are there any performance issues?
- [ ] **Security**: Are there any security vulnerabilities?
- [ ] **Testing**: Are there adequate tests?
- [ ] **Documentation**: Is the code properly documented?
- [ ] **Standards**: Does the code follow established standards?

### Code Metrics
- **Cyclomatic Complexity**: Maximum 10 per method
- **Lines of Code**: Maximum 50 per method
- **Code Coverage**: Minimum 80%
- **Technical Debt**: Monitor and reduce technical debt

### Static Analysis
```bash
# Run SonarLint analysis
dotnet sonarscanner begin /k:"ImageViewer" /d:sonar.host.url="http://localhost:9000"
dotnet build
dotnet sonarscanner end
```

## 🔒 Security Guidelines

### Input Validation
```csharp
public class CreateCollectionRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; }
    
    [Required]
    [StringLength(500, MinimumLength = 1)]
    public string Path { get; set; }
}
```

### Authentication and Authorization
```csharp
[Authorize]
[ApiController]
public class CollectionsController : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "ReadCollections")]
    public async Task<ActionResult<PagedResult<CollectionDto>>> GetCollections()
    {
        // Implementation
    }
}
```

### Data Protection
```csharp
// Encrypt sensitive data
public class User
{
    [BsonElement("email")]
    [Encrypted]
    public string Email { get; set; }
}
```

## 📈 Performance Guidelines

### Database Optimization
```csharp
// Use projection to limit returned fields
var collections = await _collections
    .Find(c => c.LibraryId == libraryId)
    .Project(c => new { c.Id, c.Name, c.Type })
    .ToListAsync();

// Use appropriate indexes
db.collections.createIndex({ "libraryId": 1, "type": 1 });
```

### Caching Strategy
```csharp
[ResponseCache(Duration = 300, VaryByQueryKeys = new[] { "id" })]
public async Task<ActionResult<CollectionDto>> GetCollection(ObjectId id)
{
    // Implementation
}
```

### Async/Await Best Practices
```csharp
// Good: Use async/await properly
public async Task<Collection> GetCollectionAsync(ObjectId id)
{
    return await _repository.GetByIdAsync(id);
}

// Bad: Blocking async calls
public Collection GetCollection(ObjectId id)
{
    return _repository.GetByIdAsync(id).Result;
}
```

## 🚀 Deployment Guidelines

### Environment Configuration
```json
{
  "ConnectionStrings": {
    "MongoDB": "mongodb://localhost:27017/imageviewer"
  },
  "RabbitMQ": {
    "HostName": "localhost",
    "Port": 5672,
    "UserName": "guest",
    "Password": "guest"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### Build and Deployment
```bash
# Build for production
dotnet build --configuration Release

# Publish application
dotnet publish --configuration Release --output ./publish

# Run application
dotnet run --configuration Release
```

## 📚 Documentation Standards

### XML Documentation
```csharp
/// <summary>
/// Gets a collection by its unique identifier
/// </summary>
/// <param name="id">The collection identifier</param>
/// <returns>The collection if found, null otherwise</returns>
/// <exception cref="ArgumentNullException">Thrown when id is null</exception>
public async Task<Collection> GetCollectionAsync(ObjectId id)
{
    // Implementation
}
```

### README Files
Each project should have a README.md file with:
- Project description
- Setup instructions
- Usage examples
- API documentation
- Contributing guidelines

### Code Comments
```csharp
// Use comments to explain "why", not "what"
// This algorithm uses a hash-based approach for performance
var hash = ComputeHash(filePath);

// Bad: Obvious comments
// Increment the counter
counter++;
```

## 🔄 Version Control

### Git Workflow
```bash
# Feature branch workflow
git checkout -b feature/collection-management
git add .
git commit -m "feat: add collection management functionality"
git push origin feature/collection-management
```

### Commit Message Convention
```
type(scope): description

feat(api): add collection management endpoints
fix(auth): resolve authentication token expiration
docs(readme): update installation instructions
test(unit): add collection service unit tests
```

### Branch Naming
- `feature/feature-name` - New features
- `bugfix/bug-description` - Bug fixes
- `hotfix/critical-fix` - Critical fixes
- `release/version-number` - Release preparation

## 📋 Development Checklist

### Before Starting Development
- [ ] Read and understand requirements
- [ ] Set up development environment
- [ ] Create feature branch
- [ ] Review existing code and patterns
- [ ] Plan implementation approach

### During Development
- [ ] Follow coding standards
- [ ] Write unit tests
- [ ] Update documentation
- [ ] Perform code reviews
- [ ] Test functionality

### Before Committing
- [ ] Run all tests
- [ ] Check code coverage
- [ ] Run static analysis
- [ ] Update documentation
- [ ] Clean up code

### After Development
- [ ] Create pull request
- [ ] Address review comments
- [ ] Merge to main branch
- [ ] Update project documentation
- [ ] Deploy to staging environment

## 🎯 Best Practices Summary

1. **Write Clean Code**: Follow clean code principles and SOLID design
2. **Test Everything**: Write comprehensive unit and integration tests
3. **Document Code**: Use XML documentation and meaningful comments
4. **Review Code**: Perform thorough code reviews
5. **Monitor Performance**: Optimize for performance and scalability
6. **Secure Code**: Implement proper security measures
7. **Version Control**: Use proper Git workflow and commit messages
8. **Continuous Integration**: Automate testing and deployment
9. **Monitor Quality**: Use static analysis and code metrics
10. **Stay Updated**: Keep dependencies and tools up to date

---

**Document Version**: 1.0  
**Last Updated**: 2025-01-04  
**Next Review**: 2025-01-11
