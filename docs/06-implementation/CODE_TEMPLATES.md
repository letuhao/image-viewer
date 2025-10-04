# Code Templates - ImageViewer Platform

## 📋 Overview

This document provides comprehensive code templates for the ImageViewer Platform implementation, ensuring consistency and best practices across all development work.

## 🏗️ Domain Layer Templates

### Entity Template
```csharp
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Domain.Events;

namespace ImageViewer.Domain.Entities
{
    /// <summary>
    /// [EntityName] entity representing [description]
    /// </summary>
    public class [EntityName] : BaseEntity, IAggregateRoot
    {
        #region Properties
        
        [BsonElement("propertyName")]
        public string PropertyName { get; private set; }
        
        [BsonElement("isActive")]
        public bool IsActive { get; private set; }
        
        [BsonElement("metadata")]
        public [ValueObjectName] Metadata { get; private set; }
        
        #endregion
        
        #region Constructors
        
        // Private constructor for MongoDB
        private [EntityName]() { }
        
        public [EntityName](string propertyName, [ValueObjectName] metadata)
        {
            Id = ObjectId.GenerateNewId();
            PropertyName = propertyName ?? throw new ArgumentNullException(nameof(propertyName));
            Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
            IsActive = true;
            
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            
            AddDomainEvent(new [EntityName]CreatedEvent(Id, PropertyName));
        }
        
        #endregion
        
        #region Domain Methods
        
        public void UpdatePropertyName(string newPropertyName)
        {
            if (string.IsNullOrWhiteSpace(newPropertyName))
                throw new ArgumentException("Property name cannot be null or empty", nameof(newPropertyName));
            
            PropertyName = newPropertyName;
            UpdateTimestamp();
            
            AddDomainEvent(new [EntityName]PropertyNameChangedEvent(Id, newPropertyName));
        }
        
        public void Activate()
        {
            if (!IsActive)
            {
                IsActive = true;
                UpdateTimestamp();
                
                AddDomainEvent(new [EntityName]ActivatedEvent(Id));
            }
        }
        
        public void Deactivate()
        {
            if (IsActive)
            {
                IsActive = false;
                UpdateTimestamp();
                
                AddDomainEvent(new [EntityName]DeactivatedEvent(Id));
            }
        }
        
        #endregion
        
        #region Private Methods
        
        private void UpdateTimestamp()
        {
            UpdatedAt = DateTime.UtcNow;
        }
        
        #endregion
    }
}
```

### Value Object Template
```csharp
using System.Collections.Generic;
using System.Linq;

namespace ImageViewer.Domain.ValueObjects
{
    /// <summary>
    /// [ValueObjectName] value object representing [description]
    /// </summary>
    public class [ValueObjectName] : ValueObject
    {
        #region Properties
        
        public string Property1 { get; private set; }
        public int Property2 { get; private set; }
        public List<string> Property3 { get; private set; }
        
        #endregion
        
        #region Constructors
        
        public [ValueObjectName](string property1, int property2, List<string> property3 = null)
        {
            Property1 = property1 ?? throw new ArgumentNullException(nameof(property1));
            Property2 = property2;
            Property3 = property3 ?? new List<string>();
        }
        
        #endregion
        
        #region Methods
        
        public void UpdateProperty1(string newProperty1)
        {
            if (string.IsNullOrWhiteSpace(newProperty1))
                throw new ArgumentException("Property1 cannot be null or empty", nameof(newProperty1));
            
            Property1 = newProperty1;
        }
        
        public void AddProperty3(string item)
        {
            if (!string.IsNullOrWhiteSpace(item) && !Property3.Contains(item))
            {
                Property3.Add(item);
            }
        }
        
        public void RemoveProperty3(string item)
        {
            Property3.Remove(item);
        }
        
        #endregion
        
        #region Equality
        
        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Property1;
            yield return Property2;
            foreach (var item in Property3.OrderBy(x => x))
            {
                yield return item;
            }
        }
        
        #endregion
    }
}
```

### Domain Event Template
```csharp
using MongoDB.Bson;
using ImageViewer.Domain.Interfaces;

namespace ImageViewer.Domain.Events
{
    /// <summary>
    /// Domain event raised when [event description]
    /// </summary>
    public class [EntityName][Action]Event : DomainEvent
    {
        #region Properties
        
        public ObjectId [EntityName]Id { get; }
        public string [PropertyName] { get; }
        
        #endregion
        
        #region Constructors
        
        public [EntityName][Action]Event(ObjectId [entityName]Id, string [propertyName])
            : base("[EntityName][Action]")
        {
            [EntityName]Id = [entityName]Id;
            [PropertyName] = [propertyName];
        }
        
        #endregion
    }
}
```

### Repository Interface Template
```csharp
using MongoDB.Bson;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;

namespace ImageViewer.Domain.Interfaces
{
    /// <summary>
    /// Repository interface for [EntityName] operations
    /// </summary>
    public interface I[EntityName]Repository : IRepository<[EntityName]>
    {
        #region Query Methods
        
        Task<IEnumerable<[EntityName]>> GetByPropertyAsync(string property);
        Task<IEnumerable<[EntityName]>> GetActiveAsync();
        Task<[EntityName]> GetByNameAsync(string name);
        Task<long> GetCountByPropertyAsync(string property);
        
        #endregion
        
        #region Search Methods
        
        Task<IEnumerable<[EntityName]>> SearchAsync(string query);
        Task<IEnumerable<[EntityName]>> GetByFilterAsync([FilterType] filter);
        
        #endregion
    }
}
```

## 🏗️ Application Layer Templates

### Application Service Template
```csharp
using MongoDB.Bson;
using ImageViewer.Application.DTOs;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Domain.Events;
using Microsoft.Extensions.Logging;

namespace ImageViewer.Application.Services
{
    /// <summary>
    /// Application service for [EntityName] operations
    /// </summary>
    public class [EntityName]ApplicationService : I[EntityName]ApplicationService
    {
        #region Fields
        
        private readonly I[EntityName]Repository _repository;
        private readonly I[EntityName]DomainService _domainService;
        private readonly IMessageQueueService _messageQueue;
        private readonly ILogger<[EntityName]ApplicationService> _logger;
        
        #endregion
        
        #region Constructors
        
        public [EntityName]ApplicationService(
            I[EntityName]Repository repository,
            I[EntityName]DomainService domainService,
            IMessageQueueService messageQueue,
            ILogger<[EntityName]ApplicationService> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _domainService = domainService ?? throw new ArgumentNullException(nameof(domainService));
            _messageQueue = messageQueue ?? throw new ArgumentNullException(nameof(messageQueue));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        #endregion
        
        #region Public Methods
        
        public async Task<[EntityName]Dto> Create[EntityName]Async(Create[EntityName]Request request)
        {
            try
            {
                _logger.LogInformation("Creating [EntityName] with name {Name}", request.Name);
                
                // Create using domain service
                var [entityName] = await _domainService.Create[EntityName]Async(
                    request.Property1,
                    request.Property2,
                    request.Property3);
                
                // Queue background job if needed
                await _messageQueue.PublishAsync(new [EntityName]CreatedMessage
                {
                    [EntityName]Id = [entityName].Id,
                    Property1 = [entityName].Property1,
                    Property2 = [entityName].Property2
                });
                
                // Map to DTO
                var dto = MapToDto([entityName]);
                
                _logger.LogInformation("Created [EntityName] with ID {Id}", [entityName].Id);
                
                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create [EntityName] with name {Name}", request.Name);
                throw;
            }
        }
        
        public async Task<[EntityName]Dto> Update[EntityName]Async(ObjectId id, Update[EntityName]Request request)
        {
            try
            {
                _logger.LogInformation("Updating [EntityName] with ID {Id}", id);
                
                var [entityName] = await _repository.GetByIdAsync(id);
                if ([entityName] == null)
                {
                    throw new [EntityName]NotFoundException($"[EntityName] with ID {id} not found");
                }
                
                // Update using domain service
                if (!string.IsNullOrEmpty(request.Property1) && request.Property1 != [entityName].Property1)
                {
                    [entityName] = await _domainService.Update[EntityName]Property1Async(id, request.Property1);
                }
                
                // Map to DTO
                var dto = MapToDto([entityName]);
                
                _logger.LogInformation("Updated [EntityName] with ID {Id}", id);
                
                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update [EntityName] with ID {Id}", id);
                throw;
            }
        }
        
        public async Task<[EntityName]Dto> Get[EntityName]Async(ObjectId id)
        {
            try
            {
                var [entityName] = await _repository.GetByIdAsync(id);
                if ([entityName] == null)
                {
                    throw new [EntityName]NotFoundException($"[EntityName] with ID {id} not found");
                }
                
                return MapToDto([entityName]);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get [EntityName] with ID {Id}", id);
                throw;
            }
        }
        
        public async Task<IEnumerable<[EntityName]Dto>> Get[EntityName]sAsync(Get[EntityName]sRequest request)
        {
            try
            {
                var [entityName]s = await _repository.Get[EntityName]sAsync(request);
                return [entityName]s.Select(MapToDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get [EntityName]s with request {Request}", request);
                throw;
            }
        }
        
        public async Task Delete[EntityName]Async(ObjectId id)
        {
            try
            {
                _logger.LogInformation("Deleting [EntityName] with ID {Id}", id);
                
                var [entityName] = await _repository.GetByIdAsync(id);
                if ([entityName] == null)
                {
                    throw new [EntityName]NotFoundException($"[EntityName] with ID {id} not found");
                }
                
                await _repository.DeleteAsync(id);
                
                _logger.LogInformation("Deleted [EntityName] with ID {Id}", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete [EntityName] with ID {Id}", id);
                throw;
            }
        }
        
        #endregion
        
        #region Private Methods
        
        private [EntityName]Dto MapToDto([EntityName] [entityName])
        {
            return new [EntityName]Dto
            {
                Id = [entityName].Id,
                Property1 = [entityName].Property1,
                Property2 = [entityName].Property2,
                Property3 = [entityName].Property3,
                IsActive = [entityName].IsActive,
                CreatedAt = [entityName].CreatedAt,
                UpdatedAt = [entityName].UpdatedAt
            };
        }
        
        #endregion
    }
}
```

### DTO Templates
```csharp
using MongoDB.Bson;
using System.ComponentModel.DataAnnotations;

namespace ImageViewer.Application.DTOs
{
    /// <summary>
    /// Data transfer object for [EntityName]
    /// </summary>
    public class [EntityName]Dto
    {
        public ObjectId Id { get; set; }
        public string Property1 { get; set; }
        public int Property2 { get; set; }
        public List<string> Property3 { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
    
    /// <summary>
    /// Request object for creating [EntityName]
    /// </summary>
    public class Create[EntityName]Request
    {
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Property1 { get; set; }
        
        [Required]
        [Range(1, int.MaxValue)]
        public int Property2 { get; set; }
        
        public List<string> Property3 { get; set; } = new();
    }
    
    /// <summary>
    /// Request object for updating [EntityName]
    /// </summary>
    public class Update[EntityName]Request
    {
        [StringLength(100, MinimumLength = 1)]
        public string Property1 { get; set; }
        
        [Range(1, int.MaxValue)]
        public int? Property2 { get; set; }
        
        public List<string> Property3 { get; set; }
    }
    
    /// <summary>
    /// Request object for getting [EntityName]s
    /// </summary>
    public class Get[EntityName]sRequest
    {
        [Range(1, 1000)]
        public int Page { get; set; } = 1;
        
        [Range(1, 100)]
        public int Limit { get; set; } = 20;
        
        public string SortBy { get; set; } = "property1";
        public string SortOrder { get; set; } = "asc";
        
        public string Search { get; set; }
        public bool? IsActive { get; set; }
    }
}
```

## 🏗️ Infrastructure Layer Templates

### Repository Implementation Template
```csharp
using MongoDB.Driver;
using MongoDB.Bson;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace ImageViewer.Infrastructure.Data
{
    /// <summary>
    /// MongoDB repository implementation for [EntityName]
    /// </summary>
    public class Mongo[EntityName]Repository : I[EntityName]Repository
    {
        #region Fields
        
        private readonly IMongoCollection<[EntityName]> _collection;
        private readonly ILogger<Mongo[EntityName]Repository> _logger;
        
        #endregion
        
        #region Constructors
        
        public Mongo[EntityName]Repository(MongoDbContext context, ILogger<Mongo[EntityName]Repository> logger)
        {
            _collection = context.[EntityName]s;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        #endregion
        
        #region IRepository Implementation
        
        public async Task<[EntityName]> GetByIdAsync(ObjectId id)
        {
            try
            {
                return await _collection.Find(e => e.Id == id).FirstOrDefaultAsync();
            }
            catch (MongoException ex)
            {
                _logger.LogError(ex, "Failed to get [EntityName] with ID {Id}", id);
                throw new RepositoryException($"Failed to get [EntityName] with ID {id}", ex);
            }
        }
        
        public async Task<IEnumerable<[EntityName]>> GetAllAsync()
        {
            try
            {
                return await _collection.Find(_ => true).ToListAsync();
            }
            catch (MongoException ex)
            {
                _logger.LogError(ex, "Failed to get all [EntityName]s");
                throw new RepositoryException("Failed to get all [EntityName]s", ex);
            }
        }
        
        public async Task<[EntityName]> CreateAsync([EntityName] entity)
        {
            try
            {
                await _collection.InsertOneAsync(entity);
                return entity;
            }
            catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                throw new Duplicate[EntityName]Exception($"[EntityName] with property '{entity.Property1}' already exists", ex);
            }
            catch (MongoException ex)
            {
                _logger.LogError(ex, "Failed to create [EntityName] {Property1}", entity.Property1);
                throw new RepositoryException($"Failed to create [EntityName] {entity.Property1}", ex);
            }
        }
        
        public async Task<[EntityName]> UpdateAsync([EntityName] entity)
        {
            try
            {
                var filter = Builders<[EntityName]>.Filter.Eq(e => e.Id, entity.Id);
                var result = await _collection.ReplaceOneAsync(filter, entity);
                
                if (result.MatchedCount == 0)
                {
                    throw new [EntityName]NotFoundException($"[EntityName] with ID {entity.Id} not found");
                }
                
                return entity;
            }
            catch (MongoException ex)
            {
                _logger.LogError(ex, "Failed to update [EntityName] {Id}", entity.Id);
                throw new RepositoryException($"Failed to update [EntityName] {entity.Id}", ex);
            }
        }
        
        public async Task DeleteAsync(ObjectId id)
        {
            try
            {
                var result = await _collection.DeleteOneAsync(e => e.Id == id);
                
                if (result.DeletedCount == 0)
                {
                    throw new [EntityName]NotFoundException($"[EntityName] with ID {id} not found");
                }
            }
            catch (MongoException ex)
            {
                _logger.LogError(ex, "Failed to delete [EntityName] {Id}", id);
                throw new RepositoryException($"Failed to delete [EntityName] {id}", ex);
            }
        }
        
        public async Task<bool> ExistsAsync(ObjectId id)
        {
            try
            {
                var count = await _collection.CountDocumentsAsync(e => e.Id == id);
                return count > 0;
            }
            catch (MongoException ex)
            {
                _logger.LogError(ex, "Failed to check if [EntityName] exists with ID {Id}", id);
                throw new RepositoryException($"Failed to check if [EntityName] exists with ID {id}", ex);
            }
        }
        
        #endregion
        
        #region I[EntityName]Repository Implementation
        
        public async Task<IEnumerable<[EntityName]>> GetByPropertyAsync(string property)
        {
            try
            {
                return await _collection.Find(e => e.Property1 == property).ToListAsync();
            }
            catch (MongoException ex)
            {
                _logger.LogError(ex, "Failed to get [EntityName]s by property {Property}", property);
                throw new RepositoryException($"Failed to get [EntityName]s by property {property}", ex);
            }
        }
        
        public async Task<IEnumerable<[EntityName]>> GetActiveAsync()
        {
            try
            {
                return await _collection.Find(e => e.IsActive).ToListAsync();
            }
            catch (MongoException ex)
            {
                _logger.LogError(ex, "Failed to get active [EntityName]s");
                throw new RepositoryException("Failed to get active [EntityName]s", ex);
            }
        }
        
        public async Task<[EntityName]> GetByNameAsync(string name)
        {
            try
            {
                return await _collection.Find(e => e.Property1 == name).FirstOrDefaultAsync();
            }
            catch (MongoException ex)
            {
                _logger.LogError(ex, "Failed to get [EntityName] by name {Name}", name);
                throw new RepositoryException($"Failed to get [EntityName] by name {name}", ex);
            }
        }
        
        public async Task<long> GetCountByPropertyAsync(string property)
        {
            try
            {
                return await _collection.CountDocumentsAsync(e => e.Property1 == property);
            }
            catch (MongoException ex)
            {
                _logger.LogError(ex, "Failed to get count of [EntityName]s by property {Property}", property);
                throw new RepositoryException($"Failed to get count of [EntityName]s by property {property}", ex);
            }
        }
        
        public async Task<IEnumerable<[EntityName]>> SearchAsync(string query)
        {
            try
            {
                var filter = Builders<[EntityName]>.Filter.Text(query);
                return await _collection.Find(filter).ToListAsync();
            }
            catch (MongoException ex)
            {
                _logger.LogError(ex, "Failed to search [EntityName]s with query {Query}", query);
                throw new RepositoryException($"Failed to search [EntityName]s with query {query}", ex);
            }
        }
        
        #endregion
    }
}
```

## 🏗️ API Layer Templates

### Controller Template
```csharp
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using ImageViewer.Application.DTOs;
using ImageViewer.Application.Services;
using Microsoft.Extensions.Logging;

namespace ImageViewer.Api.Controllers
{
    /// <summary>
    /// Controller for [EntityName] operations
    /// </summary>
    [ApiController]
    [Route("api/v1/[controller]")]
    [Produces("application/json")]
    public class [EntityName]sController : ControllerBase
    {
        #region Fields
        
        private readonly I[EntityName]ApplicationService _service;
        private readonly ILogger<[EntityName]sController> _logger;
        
        #endregion
        
        #region Constructors
        
        public [EntityName]sController(
            I[EntityName]ApplicationService service,
            ILogger<[EntityName]sController> logger)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        #endregion
        
        #region HTTP Methods
        
        /// <summary>
        /// Gets a collection of [EntityName]s with optional filtering and pagination
        /// </summary>
        /// <param name="request">Query parameters for filtering and pagination</param>
        /// <returns>A paged result of [EntityName]s</returns>
        /// <response code="200">Returns the requested [EntityName]s</response>
        /// <response code="400">If the request parameters are invalid</response>
        /// <response code="401">If the user is not authenticated</response>
        /// <response code="403">If the user does not have permission to read [EntityName]s</response>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<[EntityName]Dto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<PagedResult<[EntityName]Dto>>> Get[EntityName]s(
            [FromQuery] Get[EntityName]sRequest request)
        {
            try
            {
                var result = await _service.Get[EntityName]sAsync(request);
                return Ok(result);
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation error in Get[EntityName]s");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Get[EntityName]s");
                return StatusCode(500, "An error occurred while processing your request");
            }
        }
        
        /// <summary>
        /// Gets a [EntityName] by its unique identifier
        /// </summary>
        /// <param name="id">The [EntityName] identifier</param>
        /// <returns>The [EntityName] if found</returns>
        /// <response code="200">Returns the requested [EntityName]</response>
        /// <response code="404">If the [EntityName] is not found</response>
        /// <response code="401">If the user is not authenticated</response>
        /// <response code="403">If the user does not have permission to read [EntityName]s</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof([EntityName]Dto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<[EntityName]Dto>> Get[EntityName](ObjectId id)
        {
            try
            {
                var [entityName] = await _service.Get[EntityName]Async(id);
                return Ok([entityName]);
            }
            catch ([EntityName]NotFoundException ex)
            {
                _logger.LogWarning(ex, "[EntityName] not found: {Id}", id);
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Get[EntityName] for ID {Id}", id);
                return StatusCode(500, "An error occurred while processing your request");
            }
        }
        
        /// <summary>
        /// Creates a new [EntityName]
        /// </summary>
        /// <param name="request">The [EntityName] creation request</param>
        /// <returns>The created [EntityName]</returns>
        /// <response code="201">Returns the created [EntityName]</response>
        /// <response code="400">If the request data is invalid</response>
        /// <response code="401">If the user is not authenticated</response>
        /// <response code="403">If the user does not have permission to create [EntityName]s</response>
        /// <response code="409">If a [EntityName] with the same name already exists</response>
        [HttpPost]
        [ProducesResponseType(typeof([EntityName]Dto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<[EntityName]Dto>> Create[EntityName](
            [FromBody] Create[EntityName]Request request)
        {
            try
            {
                var [entityName] = await _service.Create[EntityName]Async(request);
                return CreatedAtAction(nameof(Get[EntityName]), new { id = [entityName].Id }, [entityName]);
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation error in Create[EntityName]");
                return BadRequest(ex.Message);
            }
            catch (Duplicate[EntityName]Exception ex)
            {
                _logger.LogWarning(ex, "Duplicate [EntityName] in Create[EntityName]");
                return Conflict(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Create[EntityName]");
                return StatusCode(500, "An error occurred while processing your request");
            }
        }
        
        /// <summary>
        /// Updates an existing [EntityName]
        /// </summary>
        /// <param name="id">The [EntityName] identifier</param>
        /// <param name="request">The [EntityName] update request</param>
        /// <returns>The updated [EntityName]</returns>
        /// <response code="200">Returns the updated [EntityName]</response>
        /// <response code="400">If the request data is invalid</response>
        /// <response code="401">If the user is not authenticated</response>
        /// <response code="403">If the user does not have permission to update [EntityName]s</response>
        /// <response code="404">If the [EntityName] is not found</response>
        /// <response code="409">If a [EntityName] with the same name already exists</response>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof([EntityName]Dto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<[EntityName]Dto>> Update[EntityName](
            ObjectId id, 
            [FromBody] Update[EntityName]Request request)
        {
            try
            {
                var [entityName] = await _service.Update[EntityName]Async(id, request);
                return Ok([entityName]);
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation error in Update[EntityName] for ID {Id}", id);
                return BadRequest(ex.Message);
            }
            catch ([EntityName]NotFoundException ex)
            {
                _logger.LogWarning(ex, "[EntityName] not found in Update[EntityName]: {Id}", id);
                return NotFound(ex.Message);
            }
            catch (Duplicate[EntityName]Exception ex)
            {
                _logger.LogWarning(ex, "Duplicate [EntityName] in Update[EntityName] for ID {Id}", id);
                return Conflict(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Update[EntityName] for ID {Id}", id);
                return StatusCode(500, "An error occurred while processing your request");
            }
        }
        
        /// <summary>
        /// Deletes a [EntityName]
        /// </summary>
        /// <param name="id">The [EntityName] identifier</param>
        /// <returns>No content</returns>
        /// <response code="204">The [EntityName] was successfully deleted</response>
        /// <response code="401">If the user is not authenticated</response>
        /// <response code="403">If the user does not have permission to delete [EntityName]s</response>
        /// <response code="404">If the [EntityName] is not found</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> Delete[EntityName](ObjectId id)
        {
            try
            {
                await _service.Delete[EntityName]Async(id);
                return NoContent();
            }
            catch ([EntityName]NotFoundException ex)
            {
                _logger.LogWarning(ex, "[EntityName] not found in Delete[EntityName]: {Id}", id);
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Delete[EntityName] for ID {Id}", id);
                return StatusCode(500, "An error occurred while processing your request");
            }
        }
        
        #endregion
    }
}
```

## 🧪 Testing Templates

### Unit Test Template
```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using MongoDB.Bson;
using ImageViewer.Application.Services;
using ImageViewer.Application.DTOs;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ImageViewer.Tests.Application.Services
{
    [TestClass]
    public class [EntityName]ApplicationServiceTests
    {
        #region Fields
        
        private Mock<I[EntityName]Repository> _mockRepository;
        private Mock<I[EntityName]DomainService> _mockDomainService;
        private Mock<IMessageQueueService> _mockMessageQueue;
        private Mock<ILogger<[EntityName]ApplicationService>> _mockLogger;
        private [EntityName]ApplicationService _service;
        
        #endregion
        
        #region Test Setup
        
        [TestInitialize]
        public void Setup()
        {
            _mockRepository = new Mock<I[EntityName]Repository>();
            _mockDomainService = new Mock<I[EntityName]DomainService>();
            _mockMessageQueue = new Mock<IMessageQueueService>();
            _mockLogger = new Mock<ILogger<[EntityName]ApplicationService>>();
            
            _service = new [EntityName]ApplicationService(
                _mockRepository.Object,
                _mockDomainService.Object,
                _mockMessageQueue.Object,
                _mockLogger.Object);
        }
        
        #endregion
        
        #region Create[EntityName]Async Tests
        
        [TestMethod]
        public async Task Create[EntityName]Async_ValidRequest_Returns[EntityName]Dto()
        {
            // Arrange
            var request = new Create[EntityName]Request
            {
                Property1 = "Test Property",
                Property2 = 123,
                Property3 = new List<string> { "tag1", "tag2" }
            };
            
            var expected[EntityName] = new [EntityName](
                request.Property1,
                request.Property2,
                request.Property3);
            
            _mockDomainService.Setup(s => s.Create[EntityName]Async(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<List<string>>()))
                .ReturnsAsync(expected[EntityName]);
            
            // Act
            var result = await _service.Create[EntityName]Async(request);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(request.Property1, result.Property1);
            Assert.AreEqual(request.Property2, result.Property2);
            Assert.AreEqual(request.Property3.Count, result.Property3.Count);
            
            _mockDomainService.Verify(s => s.Create[EntityName]Async(
                request.Property1,
                request.Property2,
                request.Property3), Times.Once);
            
            _mockMessageQueue.Verify(m => m.PublishAsync(It.IsAny<[EntityName]CreatedMessage>()), Times.Once);
        }
        
        [TestMethod]
        public async Task Create[EntityName]Async_InvalidRequest_ThrowsValidationException()
        {
            // Arrange
            var request = new Create[EntityName]Request
            {
                Property1 = "", // Invalid: empty property
                Property2 = 123,
                Property3 = new List<string>()
            };
            
            _mockDomainService.Setup(s => s.Create[EntityName]Async(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<List<string>>()))
                .ThrowsAsync(new ValidationException("Property1 cannot be empty"));
            
            // Act & Assert
            await Assert.ThrowsExceptionAsync<ValidationException>(
                () => _service.Create[EntityName]Async(request));
        }
        
        #endregion
        
        #region Get[EntityName]Async Tests
        
        [TestMethod]
        public async Task Get[EntityName]Async_ValidId_Returns[EntityName]Dto()
        {
            // Arrange
            var [entityName]Id = ObjectId.GenerateNewId();
            var expected[EntityName] = new [EntityName](
                "Test Property",
                123,
                new List<string> { "tag1" });
            
            _mockRepository.Setup(r => r.GetByIdAsync([entityName]Id))
                .ReturnsAsync(expected[EntityName]);
            
            // Act
            var result = await _service.Get[EntityName]Async([entityName]Id);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(expected[EntityName].Id, result.Id);
            Assert.AreEqual(expected[EntityName].Property1, result.Property1);
            
            _mockRepository.Verify(r => r.GetByIdAsync([entityName]Id), Times.Once);
        }
        
        [TestMethod]
        public async Task Get[EntityName]Async_InvalidId_Throws[EntityName]NotFoundException()
        {
            // Arrange
            var [entityName]Id = ObjectId.GenerateNewId();
            
            _mockRepository.Setup(r => r.GetByIdAsync([entityName]Id))
                .ReturnsAsync(([EntityName])null);
            
            // Act & Assert
            await Assert.ThrowsExceptionAsync<[EntityName]NotFoundException>(
                () => _service.Get[EntityName]Async([entityName]Id));
        }
        
        #endregion
        
        #region Update[EntityName]Async Tests
        
        [TestMethod]
        public async Task Update[EntityName]Async_ValidRequest_ReturnsUpdated[EntityName]Dto()
        {
            // Arrange
            var [entityName]Id = ObjectId.GenerateNewId();
            var request = new Update[EntityName]Request
            {
                Property1 = "Updated Property",
                Property2 = 456
            };
            
            var existing[EntityName] = new [EntityName](
                "Original Property",
                123,
                new List<string>());
            
            var updated[EntityName] = new [EntityName](
                request.Property1,
                request.Property2.Value,
                new List<string>());
            
            _mockRepository.Setup(r => r.GetByIdAsync([entityName]Id))
                .ReturnsAsync(existing[EntityName]);
            
            _mockDomainService.Setup(s => s.Update[EntityName]Property1Async(
                [entityName]Id,
                request.Property1))
                .ReturnsAsync(updated[EntityName]);
            
            // Act
            var result = await _service.Update[EntityName]Async([entityName]Id, request);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(request.Property1, result.Property1);
            Assert.AreEqual(request.Property2, result.Property2);
            
            _mockRepository.Verify(r => r.GetByIdAsync([entityName]Id), Times.Once);
            _mockDomainService.Verify(s => s.Update[EntityName]Property1Async(
                [entityName]Id,
                request.Property1), Times.Once);
        }
        
        #endregion
        
        #region Delete[EntityName]Async Tests
        
        [TestMethod]
        public async Task Delete[EntityName]Async_ValidId_Deletes[EntityName]()
        {
            // Arrange
            var [entityName]Id = ObjectId.GenerateNewId();
            var existing[EntityName] = new [EntityName](
                "Test Property",
                123,
                new List<string>());
            
            _mockRepository.Setup(r => r.GetByIdAsync([entityName]Id))
                .ReturnsAsync(existing[EntityName]);
            
            _mockRepository.Setup(r => r.DeleteAsync([entityName]Id))
                .Returns(Task.CompletedTask);
            
            // Act
            await _service.Delete[EntityName]Async([entityName]Id);
            
            // Assert
            _mockRepository.Verify(r => r.GetByIdAsync([entityName]Id), Times.Once);
            _mockRepository.Verify(r => r.DeleteAsync([entityName]Id), Times.Once);
        }
        
        [TestMethod]
        public async Task Delete[EntityName]Async_InvalidId_Throws[EntityName]NotFoundException()
        {
            // Arrange
            var [entityName]Id = ObjectId.GenerateNewId();
            
            _mockRepository.Setup(r => r.GetByIdAsync([entityName]Id))
                .ReturnsAsync(([EntityName])null);
            
            // Act & Assert
            await Assert.ThrowsExceptionAsync<[EntityName]NotFoundException>(
                () => _service.Delete[EntityName]Async([entityName]Id));
        }
        
        #endregion
    }
}
```

## 📋 Usage Instructions

### Template Usage
1. **Copy the template** for the component you need to implement
2. **Replace placeholders** with actual names and values:
   - `[EntityName]` → Your entity name (e.g., `Collection`, `User`, `MediaItem`)
   - `[PropertyName]` → Your property names (e.g., `Name`, `Email`, `Path`)
   - `[ValueObjectName]` → Your value object names (e.g., `CollectionSettings`, `UserProfile`)
   - `[Action]` → Your action names (e.g., `Created`, `Updated`, `Deleted`)
3. **Customize the implementation** based on your specific requirements
4. **Add additional methods** as needed for your use case
5. **Update validation rules** and business logic as required

### Best Practices
- **Follow naming conventions** consistently
- **Add proper validation** for all inputs
- **Include comprehensive error handling**
- **Write unit tests** for all methods
- **Document all public APIs** with XML comments
- **Use dependency injection** properly
- **Follow SOLID principles** in your implementation

---

**Document Version**: 1.0  
**Last Updated**: 2025-01-04  
**Next Review**: 2025-01-11
