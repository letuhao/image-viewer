using FluentAssertions;
using Moq;
using Xunit;
using ImageViewer.Application.Services;
using ImageViewer.Application.DTOs.Notifications;
using ImageViewer.Domain.Entities;
using ImageViewer.Domain.Interfaces;
using ImageViewer.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using NotificationTemplateEntity = ImageViewer.Domain.Entities.NotificationTemplate;

namespace ImageViewer.Test.Features.Notifications.Unit;

/// <summary>
/// Unit tests for NotificationTemplateService - Notification Template Management features
/// </summary>
public class NotificationTemplateServiceTests
{
    private readonly Mock<INotificationTemplateRepository> _mockTemplateRepository;
    private readonly Mock<ILogger<NotificationTemplateService>> _mockLogger;
    private readonly NotificationTemplateService _notificationTemplateService;

    public NotificationTemplateServiceTests()
    {
        _mockTemplateRepository = new Mock<INotificationTemplateRepository>();
        _mockLogger = new Mock<ILogger<NotificationTemplateService>>();
        _notificationTemplateService = new NotificationTemplateService(_mockTemplateRepository.Object, _mockLogger.Object);
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Arrange & Act
        var service = new NotificationTemplateService(_mockTemplateRepository.Object, _mockLogger.Object);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullRepository_ShouldThrowArgumentNullException()
    {
        // Arrange, Act & Assert
        var action = () => new NotificationTemplateService(null!, _mockLogger.Object);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("notificationTemplateRepository");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange, Act & Assert
        var action = () => new NotificationTemplateService(_mockTemplateRepository.Object, null!);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #region Template Creation Tests

    [Fact]
    public async Task CreateTemplateAsync_WithValidParameters_ShouldCreateTemplate()
    {
        // Arrange
        var templateName = "Welcome Email";
        var templateType = "email";
        var category = "system";
        var subject = "Welcome to ImageViewer";
        var content = "Hello {userName}, welcome to ImageViewer!";

        _mockTemplateRepository.Setup(x => x.GetByTemplateNameAsync(templateName))
            .ReturnsAsync((NotificationTemplateEntity?)null);
        _mockTemplateRepository.Setup(x => x.CreateAsync(It.IsAny<NotificationTemplateEntity>()))
            .Returns(Task.CompletedTask);

        // Act
        var request = new CreateNotificationTemplateRequest
        {
            TemplateName = templateName,
            TemplateType = templateType,
            Category = category,
            Subject = subject,
            Content = content
        };
        var result = await _notificationTemplateService.CreateTemplateAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.TemplateName.Should().Be(templateName);
        result.TemplateType.Should().Be(templateType);
        result.Category.Should().Be(category);
        result.Subject.Should().Be(subject);
        result.Content.Should().Be(content);
        result.Variables.Should().Contain("userName");
        result.Channels.Should().Contain(templateType);
        result.IsActive.Should().BeTrue();
        result.Version.Should().Be(1);

        _mockTemplateRepository.Verify(x => x.CreateAsync(It.IsAny<NotificationTemplateEntity>()), Times.Once);
    }

    [Fact]
    public async Task CreateTemplateAsync_WithHtmlContent_ShouldCreateTemplateWithHtml()
    {
        // Arrange
        var templateName = "Welcome Email";
        var templateType = "email";
        var category = "system";
        var subject = "Welcome to ImageViewer";
        var content = "Hello {userName}, welcome to ImageViewer!";
        var htmlContent = "<h1>Welcome {userName}!</h1><p>Welcome to ImageViewer!</p>";

        _mockTemplateRepository.Setup(x => x.GetByTemplateNameAsync(templateName))
            .ReturnsAsync((NotificationTemplateEntity?)null);
        _mockTemplateRepository.Setup(x => x.CreateAsync(It.IsAny<NotificationTemplateEntity>()))
            .Returns(Task.CompletedTask);

        // Act
        var request = new CreateNotificationTemplateRequest
        {
            TemplateName = templateName,
            TemplateType = templateType,
            Category = category,
            Subject = subject,
            Content = content,
            HtmlContent = htmlContent
        };
        var result = await _notificationTemplateService.CreateTemplateAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.HtmlContent.Should().Be(htmlContent);
    }

    [Fact]
    public async Task CreateTemplateAsync_WithExistingTemplateName_ShouldThrowDuplicateEntityException()
    {
        // Arrange
        var templateName = "Existing Template";
        var existingTemplate = new NotificationTemplateEntity("Existing Template", "email", "system", "Subject", "Content");

        _mockTemplateRepository.Setup(x => x.GetByTemplateNameAsync(templateName))
            .ReturnsAsync(existingTemplate);

        // Act & Assert
        var request = new CreateNotificationTemplateRequest
        {
            TemplateName = templateName,
            TemplateType = "email",
            Category = "system",
            Subject = "Subject",
            Content = "Content"
        };
        var action = async () => await _notificationTemplateService.CreateTemplateAsync(request);
        await action.Should().ThrowAsync<DuplicateEntityException>()
            .WithMessage($"Notification template with name '{templateName}' already exists.");
    }

    [Fact]
    public async Task CreateTemplateAsync_WithEmptyTemplateName_ShouldThrowValidationException()
    {
        // Act & Assert
        var request = new CreateNotificationTemplateRequest
        {
            TemplateName = "",
            TemplateType = "email",
            Category = "system",
            Subject = "Subject",
            Content = "Content"
        };
        var action = async () => await _notificationTemplateService.CreateTemplateAsync(request);
        await action.Should().ThrowAsync<ValidationException>()
            .WithMessage("Template name cannot be null or empty.");
    }

    [Fact]
    public async Task CreateTemplateAsync_WithEmptyTemplateType_ShouldThrowValidationException()
    {
        // Act & Assert
        var request = new CreateNotificationTemplateRequest
        {
            TemplateName = "Template",
            TemplateType = "",
            Category = "system",
            Subject = "Subject",
            Content = "Content"
        };
        var action = async () => await _notificationTemplateService.CreateTemplateAsync(request);
        await action.Should().ThrowAsync<ValidationException>()
            .WithMessage("Template type cannot be null or empty.");
    }

    [Fact]
    public async Task CreateTemplateAsync_WithEmptyCategory_ShouldThrowValidationException()
    {
        // Act & Assert
        var request = new CreateNotificationTemplateRequest
        {
            TemplateName = "Template",
            TemplateType = "email",
            Category = "",
            Subject = "Subject",
            Content = "Content"
        };
        var action = async () => await _notificationTemplateService.CreateTemplateAsync(request);
        await action.Should().ThrowAsync<ValidationException>()
            .WithMessage("Category cannot be null or empty.");
    }

    [Fact]
    public async Task CreateTemplateAsync_WithEmptySubject_ShouldThrowValidationException()
    {
        // Act & Assert
        var request = new CreateNotificationTemplateRequest
        {
            TemplateName = "Template",
            TemplateType = "email",
            Category = "system",
            Subject = "",
            Content = "Content"
        };
        var action = async () => await _notificationTemplateService.CreateTemplateAsync(request);
        await action.Should().ThrowAsync<ValidationException>()
            .WithMessage("Subject cannot be null or empty.");
    }

    [Fact]
    public async Task CreateTemplateAsync_WithEmptyContent_ShouldThrowValidationException()
    {
        // Act & Assert
        var request = new CreateNotificationTemplateRequest
        {
            TemplateName = "Template",
            TemplateType = "email",
            Category = "system",
            Subject = "Subject",
            Content = ""
        };
        var action = async () => await _notificationTemplateService.CreateTemplateAsync(request);
        await action.Should().ThrowAsync<ValidationException>()
            .WithMessage("Content cannot be null or empty.");
    }

    #endregion

    #region Template Retrieval Tests

    [Fact]
    public async Task GetTemplateByIdAsync_WithValidId_ShouldReturnTemplate()
    {
        // Arrange
        var templateId = ObjectId.GenerateNewId();
        var template = new NotificationTemplateEntity("Test Template", "email", "system", "Subject", "Content");

        _mockTemplateRepository.Setup(x => x.GetByIdAsync(templateId))
            .ReturnsAsync(template);

        // Act
        var result = await _notificationTemplateService.GetTemplateByIdAsync(templateId);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(template);
    }

    [Fact]
    public async Task GetTemplateByIdAsync_WithNonExistentId_ShouldReturnNull()
    {
        // Arrange
        var templateId = ObjectId.GenerateNewId();

        _mockTemplateRepository.Setup(x => x.GetByIdAsync(templateId))
            .ReturnsAsync((NotificationTemplateEntity?)null);

        // Act
        var result = await _notificationTemplateService.GetTemplateByIdAsync(templateId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetTemplateByNameAsync_WithValidName_ShouldReturnTemplate()
    {
        // Arrange
        var templateName = "Test Template";
        var template = new NotificationTemplateEntity(templateName, "email", "system", "Subject", "Content");

        _mockTemplateRepository.Setup(x => x.GetByTemplateNameAsync(templateName))
            .ReturnsAsync(template);

        // Act
        var result = await _notificationTemplateService.GetTemplateByNameAsync(templateName);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(template);
    }

    [Fact]
    public async Task GetTemplateByNameAsync_WithEmptyName_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = async () => await _notificationTemplateService.GetTemplateByNameAsync("");
        await action.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("templateName");
    }

    [Fact]
    public async Task GetAllTemplatesAsync_ShouldReturnAllTemplates()
    {
        // Arrange
        var templates = new List<NotificationTemplateEntity>
        {
            new NotificationTemplateEntity("Template 1", "email", "system", "Subject 1", "Content 1"),
            new NotificationTemplateEntity("Template 2", "push", "social", "Subject 2", "Content 2")
        };

        _mockTemplateRepository.Setup(x => x.GetAllAsync())
            .ReturnsAsync(templates);

        // Act
        var result = await _notificationTemplateService.GetAllTemplatesAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(templates);
    }

    [Fact]
    public async Task GetTemplatesByTypeAsync_WithValidType_ShouldReturnTemplates()
    {
        // Arrange
        var templateType = "email";
        var templates = new List<NotificationTemplateEntity>
        {
            new NotificationTemplateEntity("Email Template 1", templateType, "system", "Subject 1", "Content 1"),
            new NotificationTemplateEntity("Email Template 2", templateType, "social", "Subject 2", "Content 2")
        };

        _mockTemplateRepository.Setup(x => x.GetByTemplateTypeAsync(templateType))
            .ReturnsAsync(templates);

        // Act
        var result = await _notificationTemplateService.GetTemplatesByTypeAsync(templateType);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(t => t.TemplateType == templateType);
    }

    [Fact]
    public async Task GetTemplatesByTypeAsync_WithEmptyType_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = async () => await _notificationTemplateService.GetTemplatesByTypeAsync("");
        await action.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("templateType");
    }

    [Fact]
    public async Task GetTemplatesByCategoryAsync_WithValidCategory_ShouldReturnTemplates()
    {
        // Arrange
        var category = "system";
        var templates = new List<NotificationTemplateEntity>
        {
            new NotificationTemplateEntity("System Template 1", "email", category, "Subject 1", "Content 1"),
            new NotificationTemplateEntity("System Template 2", "push", category, "Subject 2", "Content 2")
        };

        _mockTemplateRepository.Setup(x => x.GetByCategoryAsync(category))
            .ReturnsAsync(templates);

        // Act
        var result = await _notificationTemplateService.GetTemplatesByCategoryAsync(category);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(t => t.Category == category);
    }

    [Fact]
    public async Task GetTemplatesByCategoryAsync_WithEmptyCategory_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = async () => await _notificationTemplateService.GetTemplatesByCategoryAsync("");
        await action.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("category");
    }

    [Fact]
    public async Task GetActiveTemplatesAsync_ShouldReturnActiveTemplates()
    {
        // Arrange
        var templates = new List<NotificationTemplateEntity>
        {
            new NotificationTemplateEntity("Active Template 1", "email", "system", "Subject 1", "Content 1"),
            new NotificationTemplateEntity("Active Template 2", "push", "social", "Subject 2", "Content 2")
        };
        templates[0].Activate();
        templates[1].Activate();

        _mockTemplateRepository.Setup(x => x.GetActiveTemplatesAsync())
            .ReturnsAsync(templates);

        // Act
        var result = await _notificationTemplateService.GetActiveTemplatesAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(t => t.IsActive);
    }

    [Fact]
    public async Task GetTemplatesByLanguageAsync_WithValidLanguage_ShouldReturnTemplates()
    {
        // Arrange
        var language = "en";
        var templates = new List<NotificationTemplateEntity>
        {
            new NotificationTemplateEntity("English Template 1", "email", "system", "Subject 1", "Content 1") { Language = language },
            new NotificationTemplateEntity("English Template 2", "push", "social", "Subject 2", "Content 2") { Language = language }
        };

        _mockTemplateRepository.Setup(x => x.GetByLanguageAsync(language))
            .ReturnsAsync(templates);

        // Act
        var result = await _notificationTemplateService.GetTemplatesByLanguageAsync(language);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(t => t.Language == language);
    }

    [Fact]
    public async Task GetTemplatesByLanguageAsync_WithEmptyLanguage_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = async () => await _notificationTemplateService.GetTemplatesByLanguageAsync("");
        await action.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("language");
    }

    #endregion

    #region Template Update Tests

    [Fact]
    public async Task UpdateTemplateAsync_WithValidParameters_ShouldUpdateTemplate()
    {
        // Arrange
        var templateId = ObjectId.GenerateNewId();
        var template = new NotificationTemplateEntity("Test Template", "email", "system", "Old Subject", "Old Content");
        var newSubject = "New Subject";
        var newContent = "New Content with {userName}";

        _mockTemplateRepository.Setup(x => x.GetByIdAsync(templateId))
            .ReturnsAsync(template);
        _mockTemplateRepository.Setup(x => x.UpdateAsync(It.IsAny<NotificationTemplateEntity>()))
            .Returns(Task.CompletedTask);

        // Act
        var request = new UpdateNotificationTemplateRequest
        {
            Subject = newSubject,
            Content = newContent
        };
        var result = await _notificationTemplateService.UpdateTemplateAsync(templateId, request);

        // Assert
        result.Should().NotBeNull();
        result.Subject.Should().Be(newSubject);
        result.Content.Should().Be(newContent);
        result.Version.Should().Be(2); // Version should be incremented
        result.Variables.Should().Contain("userName");

        _mockTemplateRepository.Verify(x => x.UpdateAsync(It.IsAny<NotificationTemplateEntity>()), Times.Once);
    }

    [Fact]
    public async Task UpdateTemplateAsync_WithNonExistentId_ShouldThrowEntityNotFoundException()
    {
        // Arrange
        var templateId = ObjectId.GenerateNewId();

        _mockTemplateRepository.Setup(x => x.GetByIdAsync(templateId))
            .ReturnsAsync((NotificationTemplateEntity?)null);

        // Act & Assert
        var request = new UpdateNotificationTemplateRequest
        {
            Subject = "Subject",
            Content = "Content"
        };
        var action = async () => await _notificationTemplateService.UpdateTemplateAsync(templateId, request);
        await action.Should().ThrowAsync<EntityNotFoundException>()
            .WithMessage($"Notification template with ID '{templateId}' not found.");
    }

    [Fact]
    public async Task UpdateTemplateAsync_WithEmptySubject_ShouldNotUpdateSubject()
    {
        // Arrange
        var templateId = ObjectId.GenerateNewId();
        var template = new NotificationTemplateEntity("Test Template", "email", "system", "Original Subject", "Content");
        var originalSubject = template.Subject;

        _mockTemplateRepository.Setup(x => x.GetByIdAsync(templateId))
            .ReturnsAsync(template);
        _mockTemplateRepository.Setup(x => x.UpdateAsync(It.IsAny<NotificationTemplateEntity>()))
            .Returns(Task.CompletedTask);

        // Act
        var request = new UpdateNotificationTemplateRequest
        {
            Subject = "", // Empty subject
            Content = "New Content"
        };
        var result = await _notificationTemplateService.UpdateTemplateAsync(templateId, request);

        // Assert
        result.Should().NotBeNull();
        result.Subject.Should().Be(originalSubject); // Subject should remain unchanged
        result.Content.Should().Be("New Content");
    }

    [Fact]
    public async Task UpdateTemplateAsync_WithEmptyContent_ShouldNotUpdateContent()
    {
        // Arrange
        var templateId = ObjectId.GenerateNewId();
        var template = new NotificationTemplateEntity("Test Template", "email", "system", "Subject", "Original Content");
        var originalContent = template.Content;

        _mockTemplateRepository.Setup(x => x.GetByIdAsync(templateId))
            .ReturnsAsync(template);
        _mockTemplateRepository.Setup(x => x.UpdateAsync(It.IsAny<NotificationTemplateEntity>()))
            .Returns(Task.CompletedTask);

        // Act
        var request = new UpdateNotificationTemplateRequest
        {
            Subject = "New Subject",
            Content = "" // Empty content
        };
        var result = await _notificationTemplateService.UpdateTemplateAsync(templateId, request);

        // Assert
        result.Should().NotBeNull();
        result.Subject.Should().Be("New Subject");
        result.Content.Should().Be(originalContent); // Content should remain unchanged
    }

    [Fact]
    public async Task UpdateTemplateAsync_WithNewTemplateName_ShouldUpdateTemplateName()
    {
        // Arrange
        var templateId = ObjectId.GenerateNewId();
        var template = new NotificationTemplateEntity("Old Name", "email", "system", "Subject", "Content");
        var newTemplateName = "New Name";

        _mockTemplateRepository.Setup(x => x.GetByIdAsync(templateId))
            .ReturnsAsync(template);
        _mockTemplateRepository.Setup(x => x.GetByTemplateNameAsync(newTemplateName))
            .ReturnsAsync((NotificationTemplateEntity?)null); // No existing template with new name
        _mockTemplateRepository.Setup(x => x.UpdateAsync(It.IsAny<NotificationTemplateEntity>()))
            .Returns(Task.CompletedTask);

        // Act
        var request = new UpdateNotificationTemplateRequest
        {
            TemplateName = newTemplateName
        };
        var result = await _notificationTemplateService.UpdateTemplateAsync(templateId, request);

        // Assert
        result.Should().NotBeNull();
        result.TemplateName.Should().Be(newTemplateName);
        _mockTemplateRepository.Verify(x => x.UpdateAsync(It.IsAny<NotificationTemplateEntity>()), Times.Once);
    }

    [Fact]
    public async Task UpdateTemplateAsync_WithExistingTemplateName_ShouldThrowDuplicateEntityException()
    {
        // Arrange
        var templateId = ObjectId.GenerateNewId();
        var template = new NotificationTemplateEntity("Original Name", "email", "system", "Subject", "Content");
        var existingTemplate = new NotificationTemplateEntity("Existing Name", "email", "system", "Subject", "Content");
        var newTemplateName = "Existing Name";

        _mockTemplateRepository.Setup(x => x.GetByIdAsync(templateId))
            .ReturnsAsync(template);
        _mockTemplateRepository.Setup(x => x.GetByTemplateNameAsync(newTemplateName))
            .ReturnsAsync(existingTemplate);

        // Act & Assert
        var request = new UpdateNotificationTemplateRequest
        {
            TemplateName = newTemplateName
        };
        var action = async () => await _notificationTemplateService.UpdateTemplateAsync(templateId, request);
        await action.Should().ThrowAsync<DuplicateEntityException>()
            .WithMessage($"Notification template with name '{newTemplateName}' already exists.");
    }

    [Fact]
    public async Task UpdateTemplateAsync_WithSameTemplateName_ShouldNotThrowDuplicateEntityException()
    {
        // Arrange
        var templateId = ObjectId.GenerateNewId();
        var template = new NotificationTemplateEntity("Same Name", "email", "system", "Subject", "Content");
        template.Id = templateId; // Ensure the ID matches for the check
        var sameTemplateName = "Same Name";

        _mockTemplateRepository.Setup(x => x.GetByIdAsync(templateId))
            .ReturnsAsync(template);
        _mockTemplateRepository.Setup(x => x.GetByTemplateNameAsync(sameTemplateName))
            .ReturnsAsync(template); // Returns the same template

        _mockTemplateRepository.Setup(x => x.UpdateAsync(It.IsAny<NotificationTemplateEntity>()))
            .Returns(Task.CompletedTask);

        // Act
        var request = new UpdateNotificationTemplateRequest
        {
            TemplateName = sameTemplateName,
            Subject = "Updated Subject"
        };
        var result = await _notificationTemplateService.UpdateTemplateAsync(templateId, request);

        // Assert
        result.Should().NotBeNull();
        result.TemplateName.Should().Be(sameTemplateName);
        result.Subject.Should().Be("Updated Subject");
        _mockTemplateRepository.Verify(x => x.UpdateAsync(It.IsAny<NotificationTemplateEntity>()), Times.Once);
    }

    [Fact]
    public async Task UpdateTemplateAsync_WithNewPriority_ShouldUpdatePriority()
    {
        // Arrange
        var templateId = ObjectId.GenerateNewId();
        var template = new NotificationTemplateEntity("Test Template", "email", "system", "Subject", "Content");
        var newPriority = "high";

        _mockTemplateRepository.Setup(x => x.GetByIdAsync(templateId))
            .ReturnsAsync(template);
        _mockTemplateRepository.Setup(x => x.UpdateAsync(It.IsAny<NotificationTemplateEntity>()))
            .Returns(Task.CompletedTask);

        // Act
        var request = new UpdateNotificationTemplateRequest
        {
            Priority = newPriority
        };
        var result = await _notificationTemplateService.UpdateTemplateAsync(templateId, request);

        // Assert
        result.Should().NotBeNull();
        result.Priority.Should().Be(newPriority);
        _mockTemplateRepository.Verify(x => x.UpdateAsync(It.IsAny<NotificationTemplateEntity>()), Times.Once);
    }

    [Fact]
    public async Task UpdateTemplateAsync_WithNewLanguage_ShouldUpdateLanguage()
    {
        // Arrange
        var templateId = ObjectId.GenerateNewId();
        var template = new NotificationTemplateEntity("Test Template", "email", "system", "Subject", "Content") { Language = "en" };
        var newLanguage = "es";

        _mockTemplateRepository.Setup(x => x.GetByIdAsync(templateId))
            .ReturnsAsync(template);
        _mockTemplateRepository.Setup(x => x.UpdateAsync(It.IsAny<NotificationTemplateEntity>()))
            .Returns(Task.CompletedTask);

        // Act
        var request = new UpdateNotificationTemplateRequest
        {
            Language = newLanguage
        };
        var result = await _notificationTemplateService.UpdateTemplateAsync(templateId, request);

        // Assert
        result.Should().NotBeNull();
        result.Language.Should().Be(newLanguage);
        _mockTemplateRepository.Verify(x => x.UpdateAsync(It.IsAny<NotificationTemplateEntity>()), Times.Once);
    }

    [Fact]
    public async Task UpdateTemplateAsync_WithNewChannels_ShouldUpdateChannels()
    {
        // Arrange
        var templateId = ObjectId.GenerateNewId();
        var template = new NotificationTemplateEntity("Test Template", "email", "system", "Subject", "Content");
        template.AddChannel("sms");
        var newChannels = new List<string> { "email", "push" };

        _mockTemplateRepository.Setup(x => x.GetByIdAsync(templateId))
            .ReturnsAsync(template);
        _mockTemplateRepository.Setup(x => x.UpdateAsync(It.IsAny<NotificationTemplateEntity>()))
            .Returns(Task.CompletedTask);

        // Act
        var request = new UpdateNotificationTemplateRequest
        {
            Channels = newChannels
        };
        var result = await _notificationTemplateService.UpdateTemplateAsync(templateId, request);

        // Assert
        result.Should().NotBeNull();
        result.Channels.Should().BeEquivalentTo(newChannels);
        _mockTemplateRepository.Verify(x => x.UpdateAsync(It.IsAny<NotificationTemplateEntity>()), Times.Once);
    }

    [Fact]
    public async Task UpdateTemplateAsync_WithNewTags_ShouldUpdateTags()
    {
        // Arrange
        var templateId = ObjectId.GenerateNewId();
        var template = new NotificationTemplateEntity("Test Template", "email", "system", "Subject", "Content");
        template.AddTag("welcome");
        var newTags = new List<string> { "onboarding", "newuser" };

        _mockTemplateRepository.Setup(x => x.GetByIdAsync(templateId))
            .ReturnsAsync(template);
        _mockTemplateRepository.Setup(x => x.UpdateAsync(It.IsAny<NotificationTemplateEntity>()))
            .Returns(Task.CompletedTask);

        // Act
        var request = new UpdateNotificationTemplateRequest
        {
            Tags = newTags
        };
        var result = await _notificationTemplateService.UpdateTemplateAsync(templateId, request);

        // Assert
        result.Should().NotBeNull();
        result.Tags.Should().BeEquivalentTo(newTags);
        _mockTemplateRepository.Verify(x => x.UpdateAsync(It.IsAny<NotificationTemplateEntity>()), Times.Once);
    }

    [Fact]
    public async Task UpdateTemplateAsync_WithParentTemplateId_ShouldSetParentTemplate()
    {
        // Arrange
        var templateId = ObjectId.GenerateNewId();
        var parentTemplateId = ObjectId.GenerateNewId();
        var template = new NotificationTemplateEntity("Child Template", "email", "system", "Subject", "Content");

        _mockTemplateRepository.Setup(x => x.GetByIdAsync(templateId))
            .ReturnsAsync(template);
        _mockTemplateRepository.Setup(x => x.UpdateAsync(It.IsAny<NotificationTemplateEntity>()))
            .Returns(Task.CompletedTask);

        // Act
        var request = new UpdateNotificationTemplateRequest
        {
            ParentTemplateId = parentTemplateId
        };
        var result = await _notificationTemplateService.UpdateTemplateAsync(templateId, request);

        // Assert
        result.Should().NotBeNull();
        result.ParentTemplateId.Should().Be(parentTemplateId);
        _mockTemplateRepository.Verify(x => x.UpdateAsync(It.IsAny<NotificationTemplateEntity>()), Times.Once);
    }

    [Fact]
    public async Task UpdateTemplateAsync_WithNullParentTemplateId_ShouldClearParentTemplate()
    {
        // Arrange
        var templateId = ObjectId.GenerateNewId();
        var template = new NotificationTemplateEntity("Child Template", "email", "system", "Subject", "Content");
        template.SetParentTemplate(ObjectId.GenerateNewId()); // Set an initial parent

        _mockTemplateRepository.Setup(x => x.GetByIdAsync(templateId))
            .ReturnsAsync(template);
        _mockTemplateRepository.Setup(x => x.UpdateAsync(It.IsAny<NotificationTemplateEntity>()))
            .Returns(Task.CompletedTask);

        // Act
        var request = new UpdateNotificationTemplateRequest
        {
            ParentTemplateId = ObjectId.Empty // Represents null in this context
        };
        var result = await _notificationTemplateService.UpdateTemplateAsync(templateId, request);

        // Assert
        result.Should().NotBeNull();
        result.ParentTemplateId.Should().BeNull();
        _mockTemplateRepository.Verify(x => x.UpdateAsync(It.IsAny<NotificationTemplateEntity>()), Times.Once);
    }

    #endregion

    #region Template Activation/Deactivation Tests

    [Fact]
    public async Task ActivateTemplateAsync_WithValidId_ShouldActivateTemplate()
    {
        // Arrange
        var templateId = ObjectId.GenerateNewId();
        var template = new NotificationTemplateEntity("Test Template", "email", "system", "Subject", "Content");
        template.Deactivate(); // Start with deactivated template

        _mockTemplateRepository.Setup(x => x.GetByIdAsync(templateId))
            .ReturnsAsync(template);
        _mockTemplateRepository.Setup(x => x.UpdateAsync(It.IsAny<NotificationTemplateEntity>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _notificationTemplateService.ActivateTemplateAsync(templateId);

        // Assert
        result.Should().NotBeNull();
        result.IsActive.Should().BeTrue();

        _mockTemplateRepository.Verify(x => x.UpdateAsync(It.IsAny<NotificationTemplateEntity>()), Times.Once);
    }

    [Fact]
    public async Task ActivateTemplateAsync_WithNonExistentId_ShouldThrowEntityNotFoundException()
    {
        // Arrange
        var templateId = ObjectId.GenerateNewId();

        _mockTemplateRepository.Setup(x => x.GetByIdAsync(templateId))
            .ReturnsAsync((NotificationTemplateEntity?)null);

        // Act & Assert
        var action = async () => await _notificationTemplateService.ActivateTemplateAsync(templateId);
        await action.Should().ThrowAsync<EntityNotFoundException>()
            .WithMessage($"Notification template with ID '{templateId}' not found.");
    }

    [Fact]
    public async Task DeactivateTemplateAsync_WithValidId_ShouldDeactivateTemplate()
    {
        // Arrange
        var templateId = ObjectId.GenerateNewId();
        var template = new NotificationTemplateEntity("Test Template", "email", "system", "Subject", "Content");

        _mockTemplateRepository.Setup(x => x.GetByIdAsync(templateId))
            .ReturnsAsync(template);
        _mockTemplateRepository.Setup(x => x.UpdateAsync(It.IsAny<NotificationTemplateEntity>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _notificationTemplateService.DeactivateTemplateAsync(templateId);

        // Assert
        result.Should().NotBeNull();
        result.IsActive.Should().BeFalse();

        _mockTemplateRepository.Verify(x => x.UpdateAsync(It.IsAny<NotificationTemplateEntity>()), Times.Once);
    }

    [Fact]
    public async Task DeactivateTemplateAsync_WithNonExistentId_ShouldThrowEntityNotFoundException()
    {
        // Arrange
        var templateId = ObjectId.GenerateNewId();

        _mockTemplateRepository.Setup(x => x.GetByIdAsync(templateId))
            .ReturnsAsync((NotificationTemplateEntity?)null);

        // Act & Assert
        var action = async () => await _notificationTemplateService.DeactivateTemplateAsync(templateId);
        await action.Should().ThrowAsync<EntityNotFoundException>()
            .WithMessage($"Notification template with ID '{templateId}' not found.");
    }

    #endregion

    #region Template Deletion Tests

    [Fact]
    public async Task DeleteTemplateAsync_WithValidId_ShouldDeleteTemplate()
    {
        // Arrange
        var templateId = ObjectId.GenerateNewId();
        var template = new NotificationTemplateEntity("Test Template", "email", "system", "Subject", "Content");

        _mockTemplateRepository.Setup(x => x.GetByIdAsync(templateId))
            .ReturnsAsync(template);
        _mockTemplateRepository.Setup(x => x.DeleteAsync(templateId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _notificationTemplateService.DeleteTemplateAsync(templateId);

        // Assert
        result.Should().BeTrue();

        _mockTemplateRepository.Verify(x => x.DeleteAsync(templateId), Times.Once);
    }

    [Fact]
    public async Task DeleteTemplateAsync_WithNonExistentId_ShouldReturnFalse()
    {
        // Arrange
        var templateId = ObjectId.GenerateNewId();

        _mockTemplateRepository.Setup(x => x.GetByIdAsync(templateId))
            .ReturnsAsync((NotificationTemplateEntity?)null);

        // Act
        var result = await _notificationTemplateService.DeleteTemplateAsync(templateId);

        // Assert
        result.Should().BeFalse();

        _mockTemplateRepository.Verify(x => x.DeleteAsync(It.IsAny<ObjectId>()), Times.Never);
    }

    #endregion

    #region Template Rendering Tests

    [Fact]
    public async Task RenderTemplateAsync_WithValidParameters_ShouldRenderTemplate()
    {
        // Arrange
        var templateName = "Test Template";
        var template = new NotificationTemplateEntity(templateName, "email", "system", "Hello {userName}", "Welcome {userName} to ImageViewer!");
        var variables = new Dictionary<string, string> { { "userName", "John Doe" } };

        _mockTemplateRepository.Setup(x => x.GetByTemplateNameAsync(templateName))
            .ReturnsAsync(template);
        _mockTemplateRepository.Setup(x => x.UpdateAsync(It.IsAny<NotificationTemplateEntity>()))
            .Returns(Task.CompletedTask); // To verify MarkAsUsed updates

        // Act
        var result = await _notificationTemplateService.RenderTemplateAsync(templateName, variables);

        // Assert
        result.Should().Be("Welcome John Doe to ImageViewer!");
        _mockTemplateRepository.Verify(x => x.UpdateAsync(It.IsAny<NotificationTemplateEntity>()), Times.Once);
    }

    [Fact]
    public async Task RenderTemplateAsync_WithNonExistentName_ShouldThrowEntityNotFoundException()
    {
        // Arrange
        var templateName = "NonExistent Template";
        var variables = new Dictionary<string, string> { { "userName", "John Doe" } };

        _mockTemplateRepository.Setup(x => x.GetByTemplateNameAsync(templateName))
            .ReturnsAsync((NotificationTemplateEntity?)null);

        // Act & Assert
        var action = async () => await _notificationTemplateService.RenderTemplateAsync(templateName, variables);
        await action.Should().ThrowAsync<EntityNotFoundException>()
            .WithMessage($"Notification template with name '{templateName}' not found.");
    }

    [Fact]
    public async Task RenderTemplateAsync_WithNullVariables_ShouldThrowArgumentNullException()
    {
        // Arrange
        var templateName = "Test Template";

        // Act & Assert
        var action = async () => await _notificationTemplateService.RenderTemplateAsync(templateName, null!);
        await action.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("variables");
    }

    [Fact]
    public async Task RenderTemplateAsync_WithEmptyTemplateName_ShouldThrowArgumentException()
    {
        // Arrange
        var variables = new Dictionary<string, string> { { "userName", "John Doe" } };

        // Act & Assert
        var action = async () => await _notificationTemplateService.RenderTemplateAsync("", variables);
        await action.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("templateName");
    }

    #endregion

    #region Template Validation Tests

    [Fact]
    public async Task ValidateTemplateVariablesAsync_WithValidVariables_ShouldReturnValidResult()
    {
        // Arrange
        var templateId = ObjectId.GenerateNewId();
        var template = new NotificationTemplateEntity("Test Template", "email", "system", "Hello {userName}", "Welcome {userName} to ImageViewer!");
        var variables = new Dictionary<string, string> { { "userName", "John Doe" } };

        _mockTemplateRepository.Setup(x => x.GetByIdAsync(templateId))
            .ReturnsAsync(template);

        // Act
        var result = await _notificationTemplateService.ValidateTemplateVariablesAsync(templateId, variables);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.MissingVariables.Should().BeEmpty();
        result.ExtraVariables.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateTemplateVariablesAsync_WithMissingVariables_ShouldReturnInvalidResult()
    {
        // Arrange
        var templateId = ObjectId.GenerateNewId();
        var template = new NotificationTemplateEntity("Test Template", "email", "system", "Hello {userName}", "Welcome {userName} to ImageViewer!");
        var variables = new Dictionary<string, string>(); // Missing userName

        _mockTemplateRepository.Setup(x => x.GetByIdAsync(templateId))
            .ReturnsAsync(template);

        // Act
        var result = await _notificationTemplateService.ValidateTemplateVariablesAsync(templateId, variables);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.MissingVariables.Should().Contain("userName");
    }

    [Fact]
    public async Task ValidateTemplateVariablesAsync_WithExtraVariables_ShouldReturnResultWithExtraVariables()
    {
        // Arrange
        var templateId = ObjectId.GenerateNewId();
        var template = new NotificationTemplateEntity("Test Template", "email", "system", "Hello {userName}", "Welcome {userName} to ImageViewer!");
        var variables = new Dictionary<string, string>
        {
            { "userName", "John Doe" },
            { "extraVariable", "Extra Value" } // Extra variable not in template
        };

        _mockTemplateRepository.Setup(x => x.GetByIdAsync(templateId))
            .ReturnsAsync(template);

        // Act
        var result = await _notificationTemplateService.ValidateTemplateVariablesAsync(templateId, variables);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue(); // Still valid, just has extra variables
        result.MissingVariables.Should().BeEmpty();
        result.ExtraVariables.Should().Contain("extraVariable");
    }

    [Fact]
    public async Task ValidateTemplateVariablesAsync_WithNullVariables_ShouldThrowArgumentNullException()
    {
        // Arrange
        var templateId = ObjectId.GenerateNewId();

        // Act & Assert
        var action = async () => await _notificationTemplateService.ValidateTemplateVariablesAsync(templateId, null!);
        await action.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("variables");
    }

    [Fact]
    public async Task ValidateTemplateVariablesAsync_WithNonExistentId_ShouldThrowEntityNotFoundException()
    {
        // Arrange
        var templateId = ObjectId.GenerateNewId();
        var variables = new Dictionary<string, string> { { "userName", "John Doe" } };

        _mockTemplateRepository.Setup(x => x.GetByIdAsync(templateId))
            .ReturnsAsync((NotificationTemplateEntity?)null);

        // Act & Assert
        var action = async () => await _notificationTemplateService.ValidateTemplateVariablesAsync(templateId, variables);
        await action.Should().ThrowAsync<EntityNotFoundException>()
            .WithMessage($"Notification template with ID '{templateId}' not found.");
    }

    #endregion

    #region Template Usage Statistics Tests

    [Fact]
    public async Task GetTemplateUsageStatisticsAsync_WithValidId_ShouldReturnStatistics()
    {
        // Arrange
        var templateId = ObjectId.GenerateNewId();
        var template = new NotificationTemplateEntity("Test Template", "email", "system", "Subject", "Content");
        template.Id = templateId; // Ensure ID is set for the DTO
        template.MarkAsUsed();
        template.MarkAsUsed(); // Usage count = 2

        _mockTemplateRepository.Setup(x => x.GetByIdAsync(templateId))
            .ReturnsAsync(template);

        // Act
        var result = await _notificationTemplateService.GetTemplateUsageStatisticsAsync(templateId);

        // Assert
        result.Should().NotBeNull();
        result.TemplateId.Should().Be(templateId);
        result.TemplateName.Should().Be("Test Template");
        result.UsageCount.Should().Be(2);
        result.Version.Should().Be(1);
        result.MostUsedChannels.Should().Contain("email");
    }

    [Fact]
    public async Task GetTemplateUsageStatisticsAsync_WithNonExistentId_ShouldThrowEntityNotFoundException()
    {
        // Arrange
        var templateId = ObjectId.GenerateNewId();

        _mockTemplateRepository.Setup(x => x.GetByIdAsync(templateId))
            .ReturnsAsync((NotificationTemplateEntity?)null);

        // Act & Assert
        var action = async () => await _notificationTemplateService.GetTemplateUsageStatisticsAsync(templateId);
        await action.Should().ThrowAsync<EntityNotFoundException>()
            .WithMessage($"Notification template with ID '{templateId}' not found.");
    }

    #endregion
}