# Unit Test Plan - ImageViewer Backend

## 📊 **Tổng quan**

**Mục tiêu**: Tạo comprehensive unit test coverage cho toàn bộ backend với 5 phases nhỏ, mỗi phase commit riêng biệt.

**Coverage Target**: 90%+ code coverage
**Test Framework**: xUnit, FluentAssertions, Moq, AutoFixture
**Approach**: Phase-by-phase implementation với commit sau mỗi phase

---

## 🏗️ **ARCHITECTURE OVERVIEW**

### **Layers to Test:**
1. **Domain Layer** - Entities, Value Objects, Domain Logic
2. **Application Layer** - Services, DTOs, Business Logic  
3. **Infrastructure Layer** - Repositories, External Services
4. **API Layer** - Controllers, Middleware
5. **Integration Layer** - End-to-end scenarios

---

## 📋 **PHASE 1: DOMAIN LAYER TESTS**

### **1.1 Entity Tests** 🏛️
- **Collection.cs** - Entity behavior, validation, business rules
- **Image.cs** - Entity behavior, metadata handling
- **Tag.cs** - Entity behavior, color validation
- **CacheFolder.cs** - Entity behavior, size management
- **BackgroundJob.cs** - Entity behavior, status transitions
- **ViewSession.cs** - Entity behavior, session management

### **1.2 Value Object Tests** 📦
- **CollectionSettings** - Value object behavior, validation
- **ImageMetadata** - Value object behavior, serialization
- **TagColor** - Value object behavior, color validation
- **ViewSessionSettings** - Value object behavior, JSON handling

### **1.3 Domain Event Tests** 📢
- **CollectionCreatedEvent** - Event behavior, data integrity
- **ImageAddedEvent** - Event behavior, data integrity
- **IDomainEvent** - Interface compliance

### **1.4 Enum Tests** 🔢
- **CollectionType** - Enum values, validation
- **JobStatus** - Enum values, state transitions

**Target**: 15-20 test files, 100+ test methods
**Estimated Time**: 2-3 hours

---

## 📋 **PHASE 2: APPLICATION LAYER TESTS**

### **2.1 Service Interface Tests** 🔌
- **ICollectionService** - Interface compliance, method signatures
- **IImageService** - Interface compliance, method signatures
- **ITagService** - Interface compliance, method signatures
- **ICacheService** - Interface compliance, method signatures
- **IStatisticsService** - Interface compliance, method signatures

### **2.2 Service Implementation Tests** ⚙️
- **CollectionService** - Business logic, validation, error handling
- **ImageService** - Business logic, file operations, caching
- **TagService** - Business logic, tag management, user context
- **CacheService** - Business logic, cache operations, cleanup
- **StatisticsService** - Business logic, data aggregation, reporting

### **2.3 DTO Tests** 📄
- **CollectionDto** - Data transfer, validation, mapping
- **ImageDto** - Data transfer, validation, mapping
- **TagDto** - Data transfer, validation, mapping
- **CacheDto** - Data transfer, validation, mapping
- **StatisticsDto** - Data transfer, validation, mapping

### **2.4 Extension Tests** 🔧
- **PaginationExtensions** - Extension methods, query building
- **Validation Extensions** - Custom validation logic

**Target**: 20-25 test files, 150+ test methods
**Estimated Time**: 3-4 hours

---

## 📋 **PHASE 3: INFRASTRUCTURE LAYER TESTS**

### **3.1 Repository Tests** 🗄️
- **CollectionRepository** - CRUD operations, query building
- **ImageRepository** - CRUD operations, complex queries
- **TagRepository** - CRUD operations, relationship management
- **CacheFolderRepository** - CRUD operations, cache management
- **BackgroundJobRepository** - CRUD operations, job management
- **UnitOfWork** - Transaction management, rollback scenarios

### **3.2 Service Tests** 🔧
- **FileScannerService** - File scanning, format detection
- **CompressedFileService** - Archive handling, extraction
- **AdvancedThumbnailService** - Thumbnail generation, smart selection
- **LongPathHandler** - Path handling, Windows compatibility
- **UserContextService** - User context management, HTTP integration
- **JwtService** - JWT token generation, validation

### **3.3 Database Context Tests** 🗃️
- **ImageViewerDbContext** - Context configuration, relationships
- **Migration Tests** - Database schema, data integrity
- **Connection Tests** - Database connectivity, error handling

**Target**: 15-20 test files, 120+ test methods
**Estimated Time**: 4-5 hours

---

## 📋 **PHASE 4: API LAYER TESTS**

### **4.1 Controller Tests** 🌐
- **CollectionsController** - CRUD operations, validation, error handling
- **ImagesController** - Image operations, file serving, caching
- **TagsController** - Tag management, relationships
- **CacheController** - Cache management, statistics
- **StatisticsController** - Statistics generation, reporting
- **AuthController** - Authentication, JWT handling
- **BulkController** - Bulk operations, batch processing
- **RandomController** - Random selection, algorithms
- **CompressedFilesController** - Archive operations, extraction
- **ThumbnailsController** - Thumbnail operations, generation
- **JobsController** - Background job management
- **HealthController** - Health checks, system status

### **4.2 Middleware Tests** 🔄
- **Authentication Middleware** - JWT validation, user context
- **Error Handling Middleware** - Exception handling, response formatting
- **Logging Middleware** - Request/response logging, performance tracking

### **4.3 API Integration Tests** 🔗
- **Request/Response Flow** - End-to-end API calls
- **Authentication Flow** - Login, token validation, logout
- **File Upload/Download** - File operations, streaming
- **Cache Operations** - Cache management, invalidation

**Target**: 15-20 test files, 100+ test methods
**Estimated Time**: 3-4 hours

---

## 📋 **PHASE 5: INTEGRATION TESTS**

### **5.1 End-to-End Tests** 🔄
- **Collection Management Flow** - Create, scan, update, delete collections
- **Image Processing Flow** - Upload, process, cache, serve images
- **Tag Management Flow** - Create, assign, manage tags
- **Cache Management Flow** - Generate, manage, cleanup cache
- **Background Job Flow** - Schedule, execute, monitor jobs

### **5.2 Performance Tests** ⚡
- **Load Testing** - High volume operations
- **Memory Testing** - Memory usage, leaks
- **Database Performance** - Query optimization, indexing
- **Cache Performance** - Cache hit rates, eviction policies

### **5.3 Security Tests** 🔒
- **Authentication Security** - JWT validation, token expiration
- **Authorization Security** - Access control, permissions
- **Input Validation** - SQL injection, XSS prevention
- **File Security** - File upload validation, path traversal

**Target**: 10-15 test files, 80+ test methods
**Estimated Time**: 2-3 hours

---

## 🛠️ **TESTING TOOLS & FRAMEWORKS**

### **Core Testing Framework**
- **xUnit** - Test framework
- **FluentAssertions** - Assertion library
- **Moq** - Mocking framework
- **AutoFixture** - Test data generation

### **Database Testing**
- **Entity Framework InMemory** - In-memory database
- **TestContainers** - Containerized database testing
- **SQLite** - Lightweight database for testing

### **API Testing**
- **Microsoft.AspNetCore.Mvc.Testing** - Integration testing
- **WebApplicationFactory** - Test server setup
- **HttpClient** - HTTP client testing

### **Performance Testing**
- **BenchmarkDotNet** - Performance benchmarking
- **Memory Diagnostics** - Memory leak detection

---

## 📊 **COVERAGE TARGETS**

| Layer | Target Coverage | Test Files | Test Methods |
|-------|----------------|------------|--------------|
| **Domain** | 95% | 15-20 | 100+ |
| **Application** | 90% | 20-25 | 150+ |
| **Infrastructure** | 85% | 15-20 | 120+ |
| **API** | 80% | 15-20 | 100+ |
| **Integration** | 75% | 10-15 | 80+ |
| **TOTAL** | **90%** | **75-100** | **550+** |

---

## 🚀 **IMPLEMENTATION WORKFLOW**

### **Phase 1: Domain Layer** 🏛️
1. **Setup** test project structure
2. **Implement** entity tests
3. **Implement** value object tests
4. **Implement** domain event tests
5. **Run tests** and verify coverage
6. **Commit** with message: "Phase 1: Domain Layer Tests Complete"

### **Phase 2: Application Layer** ⚙️
1. **Implement** service interface tests
2. **Implement** service implementation tests
3. **Implement** DTO tests
4. **Implement** extension tests
5. **Run tests** and verify coverage
6. **Commit** with message: "Phase 2: Application Layer Tests Complete"

### **Phase 3: Infrastructure Layer** 🗄️
1. **Implement** repository tests
2. **Implement** service tests
3. **Implement** database context tests
4. **Run tests** and verify coverage
5. **Commit** with message: "Phase 3: Infrastructure Layer Tests Complete"

### **Phase 4: API Layer** 🌐
1. **Implement** controller tests
2. **Implement** middleware tests
3. **Implement** API integration tests
4. **Run tests** and verify coverage
5. **Commit** with message: "Phase 4: API Layer Tests Complete"

### **Phase 5: Integration Tests** 🔄
1. **Implement** end-to-end tests
2. **Implement** performance tests
3. **Implement** security tests
4. **Run tests** and verify coverage
5. **Commit** with message: "Phase 5: Integration Tests Complete"

---

## 📈 **SUCCESS CRITERIA**

### **Coverage Metrics**
- **Overall Coverage**: 90%+
- **Domain Layer**: 95%+
- **Application Layer**: 90%+
- **Infrastructure Layer**: 85%+
- **API Layer**: 80%+
- **Integration Layer**: 75%+

### **Quality Metrics**
- **All Tests Pass**: 100%
- **No Flaky Tests**: 0%
- **Fast Execution**: < 30 seconds
- **Clear Test Names**: 100%
- **Good Assertions**: 100%

### **Documentation**
- **Test Documentation**: Complete
- **Coverage Reports**: Generated
- **Performance Reports**: Generated
- **Security Reports**: Generated

---

## 🎯 **NEXT STEPS**

1. **Review** this plan
2. **Start Phase 1** - Domain Layer Tests
3. **Implement** tests systematically
4. **Run tests** after each implementation
5. **Commit** after each phase completion
6. **Generate** coverage reports
7. **Document** findings and improvements

---

**Total Estimated Time**: 14-19 hours
**Total Test Files**: 75-100
**Total Test Methods**: 550+
**Target Coverage**: 90%+

---

**Created**: 2/10/2025
**Status**: Ready for Implementation
**Priority**: High
