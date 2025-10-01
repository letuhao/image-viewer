# ImageViewer.Tests

Unit tests và integration tests cho ImageViewer system.

## Cấu trúc Tests

### Domain Tests
- **Entities**: Tests cho domain entities (Collection, Image, Tag, etc.)
- **ValueObjects**: Tests cho value objects (CollectionSettings, ImageMetadata, etc.)

### Application Tests
- **Services**: Tests cho application services

### Infrastructure Tests
- **Data**: Tests cho Entity Framework DbContext và repositories

### API Tests
- **Controllers**: Tests cho API controllers

## Test Tools

- **xUnit**: Test framework
- **FluentAssertions**: Assertion library
- **Moq**: Mocking framework
- **AutoFixture**: Test data generation
- **Entity Framework InMemory**: In-memory database cho tests

## Chạy Tests

```bash
# Chạy tất cả tests
dotnet test

# Chạy tests với coverage
dotnet test --collect:"XPlat Code Coverage"

# Chạy tests trong project cụ thể
dotnet test src/tests/ImageViewer.Tests/

# Chạy tests với filter
dotnet test --filter "Category=Unit"
dotnet test --filter "Category=Integration"
```

## Test Categories

- **Unit**: Unit tests cho individual components
- **Integration**: Integration tests cho database và external services
- **Controller**: API controller tests
- **Domain**: Domain logic tests

## Best Practices

1. **Arrange-Act-Assert**: Sử dụng pattern AAA cho test structure
2. **One Assert Per Test**: Mỗi test chỉ test một behavior
3. **Descriptive Names**: Test names phải mô tả rõ behavior được test
4. **Test Data Builders**: Sử dụng builder pattern cho test data
5. **Mock External Dependencies**: Mock tất cả external dependencies
6. **Clean Database**: Mỗi test sử dụng clean database state

## Test Data

Sử dụng `TestDataBuilder` để tạo test data:

```csharp
var collection = new TestDataBuilder()
    .Collection()
    .WithName("Test Collection")
    .WithPath("C:\\Test\\Path")
    .WithType(CollectionType.Folder)
    .Build();
```

## Coverage

Target coverage: > 80%

- Domain Models: > 90%
- Application Services: > 80%
- API Controllers: > 70%
- Infrastructure: > 80%
