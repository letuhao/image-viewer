# 🧪 Testing Strategy - ImageViewer Platform

**Ngày tạo:** 2025-01-03  
**Version:** 1.0.0  
**Mục tiêu:** Định nghĩa chiến lược testing toàn diện cho ImageViewer Platform

---

## 📋 Overview

Tài liệu này định nghĩa chiến lược testing toàn diện cho ImageViewer Platform, bao gồm unit tests, integration tests, security tests, và performance tests. Mục tiêu là đạt được 90%+ test coverage và đảm bảo chất lượng cao của hệ thống.

---

## 🎯 Testing Objectives

### **Primary Goals:**
1. **Quality Assurance** - Đảm bảo code hoạt động đúng như thiết kế
2. **Regression Prevention** - Ngăn chặn bugs khi thay đổi code
3. **Documentation** - Tests serve as living documentation
4. **Confidence** - Tự tin deploy vào production
5. **Maintainability** - Dễ dàng refactor và maintain code

### **Success Metrics:**
- **Test Coverage:** > 90%
- **Unit Tests:** > 1000 test cases
- **Integration Tests:** > 100 test scenarios
- **Security Tests:** 100% security-critical paths covered
- **Performance Tests:** All critical paths benchmarked

---

## 🏗️ Testing Pyramid

```
        🔺 E2E Tests (10%)
       🔺🔺 Integration Tests (20%)
     🔺🔺🔺 Unit Tests (70%)
```

### **Unit Tests (70%)**
- **Scope:** Individual methods and classes
- **Speed:** Fast (< 1ms per test)
- **Isolation:** Complete isolation with mocks
- **Purpose:** Verify business logic correctness

### **Integration Tests (20%)**
- **Scope:** Service interactions and database operations
- **Speed:** Medium (10-100ms per test)
- **Isolation:** Partial isolation with test database
- **Purpose:** Verify component integration

### **End-to-End Tests (10%)**
- **Scope:** Complete user workflows
- **Speed:** Slow (1-10s per test)
- **Isolation:** Full system with test environment
- **Purpose:** Verify complete user journeys

---

## 🔧 Unit Testing Strategy

### **Test Structure (AAA Pattern)**
```csharp
[Test]
public async Task MethodName_Scenario_ExpectedResult()
{
    // Arrange
    var input = CreateTestInput();
    var mockDependencies = SetupMocks();
    var service = CreateServiceUnderTest(mockDependencies);

    // Act
    var result = await service.MethodUnderTest(input);

    // Assert
    Assert.That(result, Is.Not.Null);
    Assert.That(result.Property, Is.EqualTo(expectedValue));
    mockDependencies.Verify(d => d.Method(), Times.Once);
}
```

### **Test Categories**

#### **Business Logic Tests**
```csharp
[TestFixture]
public class CollectionServiceTests
{
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

    [Test]
    public async Task CreateCollectionAsync_InvalidInput_ThrowsValidationException()
    {
        // Arrange
        var libraryId = ObjectId.GenerateNewId();
        var name = ""; // Invalid empty name
        var path = "/test/path";
        var type = CollectionType.Image;
        
        var service = new CollectionService(Mock.Of<ICollectionRepository>(), Mock.Of<ILogger<CollectionService>>());

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => service.CreateCollectionAsync(libraryId, name, path, type));
        
        Assert.That(exception.Message, Does.Contain("cannot be null or empty"));
    }
}
```

#### **Security Tests**
```csharp
[TestFixture]
public class SecurityServiceTests
{
    [Test]
    public async Task LoginAsync_ValidCredentials_ReturnsAccessToken()
    {
        // Arrange
        var username = "testuser";
        var password = "validpassword";
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
        
        var user = new User
        {
            Id = ObjectId.GenerateNewId(),
            Username = username,
            Email = "test@example.com",
            PasswordHash = hashedPassword,
            IsActive = true
        };
        
        var mockUserRepository = new Mock<IUserRepository>();
        mockUserRepository.Setup(r => r.GetByUsernameAsync(username))
            .ReturnsAsync(user);
        mockUserRepository.Setup(r => r.LogSuccessfulLoginAsync(It.IsAny<ObjectId>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        
        var mockJwtService = new Mock<IJwtService>();
        mockJwtService.Setup(j => j.GenerateAccessToken(user))
            .Returns("valid-access-token");
        mockJwtService.Setup(j => j.GenerateRefreshToken())
            .Returns("valid-refresh-token");
        
        var service = new SecurityService(
            mockUserRepository.Object,
            Mock.Of<IPasswordService>(),
            mockJwtService.Object,
            Mock.Of<ILogger<SecurityService>>());

        var request = new LoginRequest
        {
            Username = username,
            Password = password,
            IpAddress = "127.0.0.1",
            UserAgent = "TestAgent"
        };

        // Act
        var result = await service.LoginAsync(request);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.AccessToken, Is.EqualTo("valid-access-token"));
        Assert.That(result.RefreshToken, Is.EqualTo("valid-refresh-token"));
        Assert.That(result.User, Is.Not.Null);
        Assert.That(result.User.Username, Is.EqualTo(username));
    }

    [Test]
    public async Task LoginAsync_InvalidCredentials_ThrowsAuthenticationException()
    {
        // Arrange
        var username = "testuser";
        var password = "wrongpassword";
        
        var user = new User
        {
            Id = ObjectId.GenerateNewId(),
            Username = username,
            Email = "test@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("correctpassword"),
            IsActive = true
        };
        
        var mockUserRepository = new Mock<IUserRepository>();
        mockUserRepository.Setup(r => r.GetByUsernameAsync(username))
            .ReturnsAsync(user);
        
        var service = new SecurityService(
            mockUserRepository.Object,
            new PasswordService(),
            Mock.Of<IJwtService>(),
            Mock.Of<ILogger<SecurityService>>());

        var request = new LoginRequest
        {
            Username = username,
            Password = password,
            IpAddress = "127.0.0.1",
            UserAgent = "TestAgent"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AuthenticationException>(
            () => service.LoginAsync(request));
        
        Assert.That(exception.Message, Does.Contain("Invalid username or password"));
    }

    [Test]
    public async Task LoginAsync_TooManyFailedAttempts_LocksAccount()
    {
        // Arrange
        var username = "testuser";
        var password = "wrongpassword";
        
        var user = new User
        {
            Id = ObjectId.GenerateNewId(),
            Username = username,
            Email = "test@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("correctpassword"),
            IsActive = true,
            FailedLoginAttempts = 4 // Already 4 failed attempts
        };
        
        var mockUserRepository = new Mock<IUserRepository>();
        mockUserRepository.Setup(r => r.GetByUsernameAsync(username))
            .ReturnsAsync(user);
        mockUserRepository.Setup(r => r.IncrementFailedLoginAttemptsAsync(It.IsAny<ObjectId>()))
            .Returns(Task.CompletedTask);
        
        var service = new SecurityService(
            mockUserRepository.Object,
            new PasswordService(),
            Mock.Of<IJwtService>(),
            Mock.Of<ILogger<SecurityService>>());

        var request = new LoginRequest
        {
            Username = username,
            Password = password,
            IpAddress = "127.0.0.1",
            UserAgent = "TestAgent"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AuthenticationException>(
            () => service.LoginAsync(request));
        
        Assert.That(exception.Message, Does.Contain("Invalid username or password"));
        mockUserRepository.Verify(r => r.IncrementFailedLoginAttemptsAsync(user.Id), Times.Once);
    }
}
```

---

## 🔗 Integration Testing Strategy

### **Database Integration Tests**
```csharp
[TestFixture]
public class CollectionRepositoryIntegrationTests : IClassFixture<MongoDbTestFixture>
{
    private readonly IMongoDatabase _database;
    private readonly CollectionRepository _repository;

    public CollectionRepositoryIntegrationTests(MongoDbTestFixture fixture)
    {
        _database = fixture.Database;
        _repository = new CollectionRepository(_database);
    }

    [Test]
    public async Task CreateAsync_ValidCollection_StoresInDatabase()
    {
        // Arrange
        var libraryId = ObjectId.GenerateNewId();
        var collection = new Collection(libraryId, "Test Collection", "/test/path", CollectionType.Image);

        // Act
        var result = await _repository.CreateAsync(collection);

        // Assert
        Assert.That(result.Id, Is.Not.EqualTo(ObjectId.Empty));
        Assert.That(result.Name, Is.EqualTo("Test Collection"));
        
        // Verify in database
        var stored = await _repository.GetByIdAsync(result.Id);
        Assert.That(stored, Is.Not.Null);
        Assert.That(stored.Name, Is.EqualTo("Test Collection"));
    }

    [Test]
    public async Task GetByPathAsync_ExistingPath_ReturnsCollection()
    {
        // Arrange
        var libraryId = ObjectId.GenerateNewId();
        var path = "/existing/path";
        var collection = new Collection(libraryId, "Existing Collection", path, CollectionType.Image);
        await _repository.CreateAsync(collection);

        // Act
        var result = await _repository.GetByPathAsync(path);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Path, Is.EqualTo(path));
        Assert.That(result.Name, Is.EqualTo("Existing Collection"));
    }

    [Test]
    public async Task UpdateAsync_ValidCollection_UpdatesInDatabase()
    {
        // Arrange
        var libraryId = ObjectId.GenerateNewId();
        var collection = new Collection(libraryId, "Original Name", "/test/path", CollectionType.Image);
        await _repository.CreateAsync(collection);
        
        collection.UpdateName("Updated Name");

        // Act
        var result = await _repository.UpdateAsync(collection);

        // Assert
        Assert.That(result.Name, Is.EqualTo("Updated Name"));
        
        // Verify in database
        var stored = await _repository.GetByIdAsync(collection.Id);
        Assert.That(stored, Is.Not.Null);
        Assert.That(stored.Name, Is.EqualTo("Updated Name"));
    }
}
```

### **Service Integration Tests**
```csharp
[TestFixture]
public class CollectionServiceIntegrationTests : IClassFixture<MongoDbTestFixture>
{
    private readonly IMongoDatabase _database;
    private readonly CollectionService _service;
    private readonly CollectionRepository _repository;

    public CollectionServiceIntegrationTests(MongoDbTestFixture fixture)
    {
        _database = fixture.Database;
        _repository = new CollectionRepository(_database);
        _service = new CollectionService(_repository, Mock.Of<ILogger<CollectionService>>());
    }

    [Test]
    public async Task CreateCollectionAsync_ValidInput_CreatesAndReturnsCollection()
    {
        // Arrange
        var libraryId = ObjectId.GenerateNewId();
        var name = "Integration Test Collection";
        var path = "/integration/test/path";
        var type = CollectionType.Image;

        // Act
        var result = await _service.CreateCollectionAsync(libraryId, name, path, type);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Name, Is.EqualTo(name));
        Assert.That(result.Path, Is.EqualTo(path));
        Assert.That(result.Type, Is.EqualTo(type));
        Assert.That(result.LibraryId, Is.EqualTo(libraryId));
        
        // Verify in database
        var stored = await _repository.GetByIdAsync(result.Id);
        Assert.That(stored, Is.Not.Null);
        Assert.That(stored.Name, Is.EqualTo(name));
    }

    [Test]
    public async Task CreateCollectionAsync_DuplicatePath_ThrowsDuplicateEntityException()
    {
        // Arrange
        var libraryId = ObjectId.GenerateNewId();
        var path = "/duplicate/path";
        
        // Create first collection
        await _service.CreateCollectionAsync(libraryId, "First Collection", path, CollectionType.Image);
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<DuplicateEntityException>(
            () => _service.CreateCollectionAsync(libraryId, "Second Collection", path, CollectionType.Video));
        
        Assert.That(exception.Message, Does.Contain("already exists"));
    }
}
```

---

## 🌐 API Integration Tests

### **HTTP Client Tests**
```csharp
[TestFixture]
public class CollectionsControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public CollectionsControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Test]
    public async Task PostCollections_ValidRequest_ReturnsCreatedResult()
    {
        // Arrange
        var request = new CreateCollectionRequest
        {
            LibraryId = ObjectId.GenerateNewId().ToString(),
            Name = "API Test Collection",
            Path = "/api/test/path",
            Type = "Image"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/collections", request);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        
        var content = await response.Content.ReadAsStringAsync();
        var collection = JsonSerializer.Deserialize<Collection>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        Assert.That(collection, Is.Not.Null);
        Assert.That(collection.Name, Is.EqualTo(request.Name));
        Assert.That(collection.Path, Is.EqualTo(request.Path));
    }

    [Test]
    public async Task PostCollections_InvalidRequest_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateCollectionRequest
        {
            LibraryId = ObjectId.GenerateNewId().ToString(),
            Name = "", // Invalid empty name
            Path = "/api/test/path",
            Type = "Image"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/collections", request);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        
        var content = await response.Content.ReadAsStringAsync();
        var error = JsonSerializer.Deserialize<ErrorResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        Assert.That(error, Is.Not.Null);
        Assert.That(error.Message, Does.Contain("validation"));
    }

    [Test]
    public async Task GetCollections_ValidRequest_ReturnsOkResult()
    {
        // Arrange
        var page = 1;
        var pageSize = 10;

        // Act
        var response = await _client.GetAsync($"/api/v1/collections?page={page}&pageSize={pageSize}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        
        var content = await response.Content.ReadAsStringAsync();
        var collections = JsonSerializer.Deserialize<List<Collection>>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        Assert.That(collections, Is.Not.Null);
        Assert.That(collections.Count, Is.LessThanOrEqualTo(pageSize));
    }
}
```

---

## 🔐 Security Testing Strategy

### **Authentication Tests**
```csharp
[TestFixture]
public class AuthenticationSecurityTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AuthenticationSecurityTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Test]
    public async Task PostAuthLogin_ValidCredentials_ReturnsAccessToken()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Username = "testuser",
            Password = "validpassword"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        
        var content = await response.Content.ReadAsStringAsync();
        var loginResult = JsonSerializer.Deserialize<LoginResult>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        Assert.That(loginResult, Is.Not.Null);
        Assert.That(loginResult.AccessToken, Is.Not.Empty);
        Assert.That(loginResult.RefreshToken, Is.Not.Empty);
        Assert.That(loginResult.User, Is.Not.Null);
    }

    [Test]
    public async Task PostAuthLogin_InvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Username = "testuser",
            Password = "wrongpassword"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task PostAuthLogin_TooManyAttempts_LocksAccount()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Username = "testuser",
            Password = "wrongpassword"
        };

        // Act - Make 5 failed login attempts
        for (int i = 0; i < 5; i++)
        {
            var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        }

        // Try with correct password - should still be locked
        loginRequest.Password = "validpassword";
        var finalResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // Assert
        Assert.That(finalResponse.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task GetCollections_WithoutToken_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/collections");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task GetCollections_WithInvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");

        // Act
        var response = await _client.GetAsync("/api/v1/collections");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task GetCollections_WithValidToken_ReturnsOk()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Username = "testuser",
            Password = "validpassword"
        };
        
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        var loginContent = await loginResponse.Content.ReadAsStringAsync();
        var loginResult = JsonSerializer.Deserialize<LoginResult>(loginContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResult.AccessToken);

        // Act
        var response = await _client.GetAsync("/api/v1/collections");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }
}
```

### **Input Validation Tests**
```csharp
[TestFixture]
public class InputValidationSecurityTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public InputValidationSecurityTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Test]
    public async Task PostCollections_SqlInjectionAttempt_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateCollectionRequest
        {
            LibraryId = ObjectId.GenerateNewId().ToString(),
            Name = "'; DROP TABLE Collections; --",
            Path = "/test/path",
            Type = "Image"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/collections", request);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task PostCollections_XssAttempt_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateCollectionRequest
        {
            LibraryId = ObjectId.GenerateNewId().ToString(),
            Name = "<script>alert('XSS')</script>",
            Path = "/test/path",
            Type = "Image"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/collections", request);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task PostCollections_ExcessiveLength_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateCollectionRequest
        {
            LibraryId = ObjectId.GenerateNewId().ToString(),
            Name = new string('A', 10000), // Very long name
            Path = "/test/path",
            Type = "Image"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/collections", request);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }
}
```

---

## ⚡ Performance Testing Strategy

### **Load Testing**
```csharp
[TestFixture]
public class PerformanceTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public PerformanceTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Test]
    public async Task GetCollections_UnderLoad_MeetsPerformanceRequirements()
    {
        // Arrange
        var client = _factory.CreateClient();
        var tasks = new List<Task<HttpResponseMessage>>();
        var concurrentRequests = 100;
        var maxResponseTime = TimeSpan.FromMilliseconds(200);

        // Act
        var stopwatch = Stopwatch.StartNew();
        for (int i = 0; i < concurrentRequests; i++)
        {
            tasks.Add(client.GetAsync("/api/v1/collections"));
        }
        
        var responses = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        Assert.That(responses.All(r => r.IsSuccessStatusCode), Is.True);
        Assert.That(stopwatch.Elapsed, Is.LessThan(maxResponseTime));
    }

    [Test]
    public async Task CreateCollection_UnderLoad_MeetsPerformanceRequirements()
    {
        // Arrange
        var client = _factory.CreateClient();
        var tasks = new List<Task<HttpResponseMessage>>();
        var concurrentRequests = 50;
        var maxResponseTime = TimeSpan.FromMilliseconds(500);

        // Act
        var stopwatch = Stopwatch.StartNew();
        for (int i = 0; i < concurrentRequests; i++)
        {
            var request = new CreateCollectionRequest
            {
                LibraryId = ObjectId.GenerateNewId().ToString(),
                Name = $"Performance Test Collection {i}",
                Path = $"/performance/test/path/{i}",
                Type = "Image"
            };
            tasks.Add(client.PostAsJsonAsync("/api/v1/collections", request));
        }
        
        var responses = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        Assert.That(responses.All(r => r.IsSuccessStatusCode), Is.True);
        Assert.That(stopwatch.Elapsed, Is.LessThan(maxResponseTime));
    }
}
```

### **Memory Usage Tests**
```csharp
[TestFixture]
public class MemoryUsageTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public MemoryUsageTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Test]
    public async Task CreateManyCollections_MemoryUsage_StaysWithinLimits()
    {
        // Arrange
        var client = _factory.CreateClient();
        var initialMemory = GC.GetTotalMemory(true);
        var maxMemoryIncrease = 100 * 1024 * 1024; // 100MB

        // Act
        for (int i = 0; i < 1000; i++)
        {
            var request = new CreateCollectionRequest
            {
                LibraryId = ObjectId.GenerateNewId().ToString(),
                Name = $"Memory Test Collection {i}",
                Path = $"/memory/test/path/{i}",
                Type = "Image"
            };
            
            var response = await client.PostAsJsonAsync("/api/v1/collections", request);
            Assert.That(response.IsSuccessStatusCode, Is.True);
        }

        // Force garbage collection
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var finalMemory = GC.GetTotalMemory(false);
        var memoryIncrease = finalMemory - initialMemory;

        // Assert
        Assert.That(memoryIncrease, Is.LessThan(maxMemoryIncrease));
    }
}
```

---

## 🧪 Test Infrastructure

### **Test Fixtures**
```csharp
public class MongoDbTestFixture : IDisposable
{
    public IMongoDatabase Database { get; }
    private readonly MongoClient _client;

    public MongoDbTestFixture()
    {
        var connectionString = Environment.GetEnvironmentVariable("MONGODB_TEST_CONNECTION_STRING")
            ?? "mongodb://localhost:27017/imageviewer_test";
        
        _client = new MongoClient(connectionString);
        Database = _client.GetDatabase("imageviewer_test");
        
        // Clean database before tests
        CleanDatabase();
    }

    private void CleanDatabase()
    {
        var collections = new[]
        {
            "collections", "libraries", "users", "tags", "mediaItems",
            "backgroundJobs", "viewSessions", "cacheFolders"
        };

        foreach (var collectionName in collections)
        {
            Database.DropCollection(collectionName);
        }
    }

    public void Dispose()
    {
        CleanDatabase();
        _client?.Dispose();
    }
}

public class WebApplicationFactory<TStartup> : Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<TStartup>
    where TStartup : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Replace production services with test services
            services.AddSingleton<ICollectionRepository, TestCollectionRepository>();
            services.AddSingleton<IUserRepository, TestUserRepository>();
            
            // Use test database
            services.Configure<MongoDbOptions>(options =>
            {
                options.ConnectionString = "mongodb://localhost:27017/imageviewer_test";
                options.DatabaseName = "imageviewer_test";
            });
        });
    }
}
```

### **Test Data Builders**
```csharp
public class CollectionBuilder
{
    private ObjectId _libraryId = ObjectId.GenerateNewId();
    private string _name = "Test Collection";
    private string _path = "/test/path";
    private CollectionType _type = CollectionType.Image;
    private bool _isActive = true;

    public CollectionBuilder WithLibraryId(ObjectId libraryId)
    {
        _libraryId = libraryId;
        return this;
    }

    public CollectionBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public CollectionBuilder WithPath(string path)
    {
        _path = path;
        return this;
    }

    public CollectionBuilder WithType(CollectionType type)
    {
        _type = type;
        return this;
    }

    public CollectionBuilder WithActiveStatus(bool isActive)
    {
        _isActive = isActive;
        return this;
    }

    public Collection Build()
    {
        var collection = new Collection(_libraryId, _name, _path, _type);
        if (!_isActive)
        {
            collection.Deactivate();
        }
        return collection;
    }
}

// Usage in tests
var collection = new CollectionBuilder()
    .WithName("Custom Collection")
    .WithPath("/custom/path")
    .WithType(CollectionType.Video)
    .Build();
```

---

## 📊 Test Metrics and Reporting

### **Coverage Requirements**
```csharp
// Minimum coverage requirements
public class CoverageRequirements
{
    public const double MinimumUnitTestCoverage = 90.0;
    public const double MinimumIntegrationTestCoverage = 80.0;
    public const double MinimumSecurityTestCoverage = 100.0;
    public const double MinimumPerformanceTestCoverage = 70.0;
}
```

### **Test Categories**
```csharp
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class CategoryAttribute : Attribute
{
    public string Name { get; }

    public CategoryAttribute(string name)
    {
        Name = name;
    }
}

// Usage
[Test]
[Category("Unit")]
public void TestMethod() { }

[Test]
[Category("Integration")]
public void IntegrationTestMethod() { }

[Test]
[Category("Security")]
public void SecurityTestMethod() { }

[Test]
[Category("Performance")]
public void PerformanceTestMethod() { }
```

---

## 🚀 CI/CD Integration

### **GitHub Actions Workflow**
```yaml
name: Test Suite

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  test:
    runs-on: ubuntu-latest
    
    services:
      mongodb:
        image: mongo:7.0
        ports:
          - 27017:27017
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '9.0.x'
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --no-restore
    
    - name: Run unit tests
      run: dotnet test --no-build --verbosity normal --collect:"XPlat Code Coverage" --filter Category=Unit
    
    - name: Run integration tests
      run: dotnet test --no-build --verbosity normal --filter Category=Integration
    
    - name: Run security tests
      run: dotnet test --no-build --verbosity normal --filter Category=Security
    
    - name: Generate coverage report
      uses: danielpalme/ReportGenerator-GitHub-Action@5.1.8
      with:
        reports: '**/coverage.cobertura.xml'
        targetdir: 'coverage'
        reporttypes: 'Html'
    
    - name: Upload coverage to Codecov
      uses: codecov/codecov-action@v3
      with:
        file: ./coverage/coverage.cobertura.xml
```

---

## 📋 Testing Checklist

### **Before Writing Tests:**
- [ ] ✅ Understand the requirements and expected behavior
- [ ] ✅ Identify edge cases and error scenarios
- [ ] ✅ Plan test data and setup requirements
- [ ] ✅ Consider performance implications

### **During Test Development:**
- [ ] ✅ Follow AAA pattern (Arrange, Act, Assert)
- [ ] ✅ Use descriptive test names
- [ ] ✅ Test both positive and negative scenarios
- [ ] ✅ Verify all assertions
- [ ] ✅ Clean up test data

### **After Writing Tests:**
- [ ] ✅ Verify tests pass consistently
- [ ] ✅ Check test coverage meets requirements
- [ ] ✅ Review test readability and maintainability
- [ ] ✅ Update documentation if needed

---

## 🎯 Success Criteria

### **Testing Success Metrics:**
- **Unit Test Coverage:** > 90%
- **Integration Test Coverage:** > 80%
- **Security Test Coverage:** 100% of critical paths
- **Performance Test Coverage:** > 70% of critical operations
- **Test Execution Time:** < 5 minutes for full suite
- **Test Reliability:** > 99% pass rate

### **Quality Gates:**
- [ ] ✅ All tests must pass before merge
- [ ] ✅ Coverage thresholds must be met
- [ ] ✅ Performance benchmarks must be satisfied
- [ ] ✅ Security tests must pass
- [ ] ✅ No critical or high-severity bugs

---

*Testing Strategy created on 2025-01-03*  
*Next review: After implementation completion*
