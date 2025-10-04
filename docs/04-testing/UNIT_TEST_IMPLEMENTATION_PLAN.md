# Unit Test Implementation Plan

## Vấn đề hiện tại
- File test bị xóa mà không có dấu vết
- Không tuân theo quy trình phát triển phần mềm
- Unit test không có hiệu quả
- Thiếu tracking và documentation

## Quy trình TDD (Test-Driven Development) đúng cách

### 1. Red-Green-Refactor Cycle
```
1. RED: Viết test fail trước
2. GREEN: Viết code tối thiểu để test pass
3. REFACTOR: Cải thiện code mà không làm test fail
```

### 2. Test-First Development
- Viết test trước khi viết implementation
- Mỗi test phải test một behavior cụ thể
- Test phải độc lập và có thể chạy song song

### 3. Test Categories
- **Unit Tests**: Test individual components
- **Integration Tests**: Test component interactions
- **End-to-End Tests**: Test complete workflows

## Implementation Plan

### Phase 1: Foundation Setup ✅
- [x] Test project structure
- [x] Basic test framework setup
- [x] Test data builders

### Phase 2: Domain Layer Tests ✅
- [x] Entity tests
- [x] Value object tests
- [x] Domain event tests

### Phase 3: Application Layer Tests ✅
- [x] Service tests
- [x] DTO tests
- [x] Business logic tests

### Phase 4: Infrastructure Layer Tests ⚠️
- [x] Repository tests
- [x] Service tests
- [x] Database tests
- [ ] **MISSING**: BackgroundJobServiceTests
- [ ] **MISSING**: CacheServiceTests
- [ ] **MISSING**: ErrorHandlingServiceTests

### Phase 5: API Layer Tests ❌
- [ ] Controller tests
- [ ] Authentication tests
- [ ] Authorization tests
- [ ] API endpoint tests

### Phase 6: Integration Tests ❌
- [ ] Database integration
- [ ] External service integration
- [ ] End-to-end workflows

## File Tracking System

### Missing Files (Cần khôi phục)
```
src/tests/ImageViewer.Tests/Infrastructure/Services/BackgroundJobServiceTests.cs
src/tests/ImageViewer.Tests/Infrastructure/Services/CacheServiceTests.cs
src/tests/ImageViewer.Tests/Infrastructure/Services/ErrorHandlingServiceTests.cs
```

### Current Status
- **Total Tests**: 70
- **Passed**: 57 (81%)
- **Failed**: 13 (19%)
- **Missing**: 3+ files

## Next Steps

### 1. Immediate Actions
1. **Khôi phục missing files**
2. **Fix failing tests**
3. **Complete Infrastructure layer**
4. **Implement API layer tests**

### 2. Process Improvements
1. **Git workflow**: Không xóa file mà không có permission
2. **Test documentation**: Track mỗi test case
3. **Continuous integration**: Auto-run tests on commit
4. **Code coverage**: Target 90%+ coverage

### 3. Quality Gates
- [ ] All tests pass
- [ ] Code coverage > 90%
- [ ] No duplicate test logic
- [ ] Proper test isolation
- [ ] Clear test naming

## Test Naming Convention
```
[MethodName]_[Scenario]_[ExpectedResult]
```

Example:
```csharp
GetUserById_WithValidId_ShouldReturnUser
GetUserById_WithInvalidId_ShouldThrowNotFoundException
```

## Test Structure Template
```csharp
[Fact]
public void MethodName_Scenario_ExpectedResult()
{
    // Arrange
    // Act
    // Assert
}
```

## Recovery Plan
1. **Audit current state**
2. **Identify missing files**
3. **Recreate missing tests**
4. **Fix failing tests**
5. **Complete remaining phases**
6. **Implement CI/CD pipeline**

## Success Criteria
- [ ] 100% test coverage for critical paths
- [ ] All tests pass consistently
- [ ] No flaky tests
- [ ] Fast test execution (< 30 seconds)
- [ ] Clear test documentation
