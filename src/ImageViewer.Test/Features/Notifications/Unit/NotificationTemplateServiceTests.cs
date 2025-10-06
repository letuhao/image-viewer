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
        var request = new CreateNotificationTemplateRequest
        {
            TemplateName = "Welcome Email",
            TemplateType = "email",
            Category = "system",
            Subject = "Welcome to ImageViewer",
            Content = "Hello {userName}, welcome to ImageViewer!"
        };

        _mockTemplateRepository.Setup(x => x.GetByTemplateNameAsync(request.TemplateName, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NotificationTemplate?)null);
        _mockTemplateRepository.Setup(x => x.CreateAsync(It.IsAny<NotificationTemplate>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _notificationTemplateService.CreateTemplateAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.TemplateName.Should().Be(request.TemplateName);
        result.TemplateType.Should().Be(request.TemplateType);
        result.Category.Should().Be(request.Category);
        result.Subject.Should().Be(request.Subject);
        result.Content.Should().Be(request.Content);
        result.Variables.Should().Contain("userName");
        result.Channels.Should().Contain(request.TemplateType);
        result.IsActive.Should().BeTrue();
        result.Version.Should().Be(1);

        _mockTemplateRepository.Verify(x => x.CreateAsync(It.IsAny<NotificationTemplate>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateTemplateAsync_WithHtmlContent_ShouldCreateTemplateWithHtml()
    {
        // Arrange
        var request = new CreateNotificationTemplateRequest
        {
            TemplateName = "Welcome Email",
            TemplateType = "email",
            Category = "system",
            Subject = "Welcome to ImageViewer",
            Content = "Hello {userName}, welcome to ImageViewer!",
            HtmlContent = "<h1>Welcome {userName}!</h1><p>Welcome to ImageViewer!</p>"
        };

        _mockTemplateRepository.Setup(x => x.GetByTemplateNameAsync(request.TemplateName, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NotificationTemplate?)null);
        _mockTemplateRepository.Setup(x => x.CreateAsync(It.IsAny<NotificationTemplate>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _notificationTemplateService.CreateTemplateAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.HtmlContent.Should().Be(request.HtmlContent);
    }

    [Fact]
    public async Task CreateTemplateAsync_WithExistingTemplateName_ShouldThrowDuplicateEntryException()
    {
        // Arrange
        var request = new CreateNotificationTemplateRequest
        {
            TemplateName = "Existing Template",
            TemplateType = "email",
            Category = "system",
            Subject = "Subject",
            Content = "Content"
        };
        var existingTemplate = new NotificationTemplate("Existing Template", "email", "system", "Subject", "Content");

        _mockTemplateRepository.Setup(x => x.GetByTemplateNameAsync(request.TemplateName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTemplate);

        // Act & Assert
        var action = async () => await _notificationTemplateService.CreateTemplateAsync(request);
        await action.Should().ThrowAsync<DuplicateEntryException>()
            .WithMessage($"Notification template with name '{request.TemplateName}' already exists.");
    }

    [Fact]
    public async Task CreateTemplateAsync_WithEmptyTemplateName_ShouldThrowValidationException()
    {
        // Arrange
        var request = new CreateNotificationTemplateRequest
        {
            TemplateName = "",
            TemplateType = "email",
            Category = "system",
            Subject = "Subject",
            Content = "Content"
        };

        // Act & Assert
        var action = async () => await _notificationTemplateService.CreateTemplateAsync(request);
        await action.Should().ThrowAsync<ValidationException>()
            .WithMessage("Template name cannot be null or empty.");
    }

    [Fact]
    public async Task CreateTemplateAsync_WithNullRequest_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = async () => await _notificationTemplateService.CreateTemplateAsync(null!);
        await action.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("request");
    }

    #endregion

    #region Template Retrieval Tests

    [Fact]
    public async Task GetTemplateByIdAsync_WithValidId_ShouldReturnTemplate()
    {
        // Arrange
        var templateId = ObjectId.GenerateNewId();
        var template = new NotificationTemplate("Test Template", "email", "system", "Subject", "Content");

        _mockTemplateRepository.Setup(x => x.GetByIdAsync(templateId, It.IsAny<CancellationToken>()))
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

        _mockTemplateRepository.Setup(x => x.GetByIdAsync(templateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NotificationTemplate?)null);

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
        var template = new NotificationTemplate(templateName, "email", "system", "Subject", "Content");

        _mockTemplateRepository.Setup(x => x.GetByTemplateNameAsync(templateName, It.IsAny<CancellationToken>()))
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
        var templates = new List<NotificationTemplate>
        {
            new NotificationTemplate("Template 1", "email", "system", "Subject 1", "Content 1"),
            new NotificationTemplate("Template 2", "push", "social", "Subject 2", "Content 2")
        };

        _mockTemplateRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
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
        var templates = new List<NotificationTemplate>
        {
            new NotificationTemplate("Email Template 1", templateType, "system", "Subject 1", "Content 1"),
            new NotificationTemplate("Email Template 2", templateType, "social", "Subject 2", "Content 2")
        };

        _mockTemplateRepository.Setup(x => x.GetByTemplateTypeAsync(templateType, It.IsAny<CancellationToken>()))
            .ReturnsAsync(templates);

        // Act
        var result = await _notificationTemplateService.GetTemplatesByTypeAsync(templateType);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(t => t.TemplateType == templateType);
    }

    [Fact]
    public async Task GetTemplatesByCategoryAsync_WithValidCategory_ShouldReturnTemplates()
    {
        // Arrange
        var category = "system";
        var templates = new List<NotificationTemplate>
        {
            new NotificationTemplate("System Template 1", "email", category, "Subject 1", "Content 1"),
            new NotificationTemplate("System Template 2", "push", category, "Subject 2", "Content 2")
        };

        _mockTemplateRepository.Setup(x => x.GetByCategoryAsync(category, It.IsAny<CancellationToken>()))
            .ReturnsAsync(templates);

        // Act
        var result = await _notificationTemplateService.GetTemplatesByCategoryAsync(category);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(t => t.Category == category);
    }

    [Fact]
    public async Task GetActiveTemplatesAsync_ShouldReturnActiveTemplates()
    {
        // Arrange
        var templates = new List<NotificationTemplate>
        {
            new NotificationTemplate("Active Template 1", "email", "system", "Subject 1", "Content 1"),
            new NotificationTemplate("Active Template 2", "push", "social", "Subject 2", "Content 2")
        };

        _mockTemplateRepository.Setup(x => x.GetActiveTemplatesAsync(It.IsAny<CancellationToken>()))
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
        var templates = new List<NotificationTemplate>
        {
            new NotificationTemplate("English Template 1", "email", "system", "Subject 1", "Content 1"),
            new NotificationTemplate("English Template 2", "push", "social", "Subject 2", "Content 2")
        };

        _mockTemplateRepository.Setup(x => x.GetByLanguageAsync(language, It.IsAny<CancellationToken>()))
            .ReturnsAsync(templates);

        // Act
        var result = await _notificationTemplateService.GetTemplatesByLanguageAsync(language);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(t => t.Language == language);
    }

    #endregion

    #region Template Update Tests

    [Fact]
    public async Task UpdateTemplateAsync_WithValidParameters_ShouldUpdateTemplate()
    {
        // Arrange
        var templateId = ObjectId.GenerateNewId();
        var template = new NotificationTemplate("Test Template", "email", "system", "Old Subject", "Old Content");
        var request = new UpdateNotificationTemplateRequest
        {
            Subject = "New Subject",
            Content = "New Content with {userName}"
        };

        _mockTemplateRepository.Setup(x => x.GetByIdAsync(templateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
        _mockTemplateRepository.Setup(x => x.UpdateAsync(It.IsAny<NotificationTemplate>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _notificationTemplateService.UpdateTemplateAsync(templateId, request);

        // Assert
        result.Should().NotBeNull();
        result.Subject.Should().Be(request.Subject);
        result.Content.Should().Be(request.Content);
        result.Version.Should().Be(2); // Version should be incremented
        result.Variables.Should().Contain("userName");

        _mockTemplateRepository.Verify(x => x.UpdateAsync(It.IsAny<NotificationTemplate>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateTemplateAsync_WithNonExistentId_ShouldThrowEntityNotFoundException()
    {
        // Arrange
        var templateId = ObjectId.GenerateNewId();
        var request = new UpdateNotificationTemplateRequest
        {
            Subject = "Subject",
            Content = "Content"
        };

        _mockTemplateRepository.Setup(x => x.GetByIdAsync(templateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NotificationTemplate?)null);

        // Act & Assert
        var action = async () => await _notificationTemplateService.UpdateTemplateAsync(templateId, request);
        await action.Should().ThrowAsync<EntityNotFoundException>()
            .WithMessage($"Notification template with ID '{templateId}' not found.");
    }

    [Fact]
    public async Task UpdateTemplateAsync_WithNullRequest_ShouldThrowArgumentNullException()
    {
        // Arrange
        var templateId = ObjectId.GenerateNewId();

        // Act & Assert
        var action = async () => await _notificationTemplateService.UpdateTemplateAsync(templateId, null!);
        await action.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("request");
    }

    #endregion

    #region Template Deletion Tests

    [Fact]
    public async Task DeleteTemplateAsync_WithValidId_ShouldDeleteTemplate()
    {
        // Arrange
        var templateId = ObjectId.GenerateNewId();
        var template = new NotificationTemplate("Test Template", "email", "system", "Subject", "Content");

        _mockTemplateRepository.Setup(x => x.GetByIdAsync(templateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
        _mockTemplateRepository.Setup(x => x.DeleteAsync(templateId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _notificationTemplateService.DeleteTemplateAsync(templateId);

        // Assert
        result.Should().BeTrue();

        _mockTemplateRepository.Verify(x => x.DeleteAsync(templateId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteTemplateAsync_WithNonExistentId_ShouldReturnFalse()
    {
        // Arrange
        var templateId = ObjectId.GenerateNewId();

        _mockTemplateRepository.Setup(x => x.GetByIdAsync(templateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NotificationTemplate?)null);

        // Act
        var result = await _notificationTemplateService.DeleteTemplateAsync(templateId);

        // Assert
        result.Should().BeFalse();

        _mockTemplateRepository.Verify(x => x.DeleteAsync(It.IsAny<ObjectId>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Template Rendering Tests

    [Fact]
    public async Task RenderTemplateAsync_WithValidParameters_ShouldRenderTemplate()
    {
        // Arrange
        var templateName = "Test Template";
        var template = new NotificationTemplate(templateName, "email", "system", "Hello {userName}", "Welcome {userName} to ImageViewer!");
        var variables = new Dictionary<string, string> { { "userName", "John Doe" } };

        _mockTemplateRepository.Setup(x => x.GetByTemplateNameAsync(templateName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
        _mockTemplateRepository.Setup(x => x.UpdateAsync(It.IsAny<NotificationTemplate>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _notificationTemplateService.RenderTemplateAsync(templateName, variables);

        // Assert
        result.Should().Be("Welcome John Doe to ImageViewer!");
        _mockTemplateRepository.Verify(x => x.UpdateAsync(It.IsAny<NotificationTemplate>(), It.IsAny<CancellationToken>()), Times.Once);
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
    public async Task RenderTemplateAsync_WithNonExistentTemplate_ShouldThrowEntityNotFoundException()
    {
        // Arrange
        var templateName = "Non-existent Template";
        var variables = new Dictionary<string, string> { { "userName", "John Doe" } };

        _mockTemplateRepository.Setup(x => x.GetByTemplateNameAsync(templateName, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NotificationTemplate?)null);

        // Act & Assert
        var action = async () => await _notificationTemplateService.RenderTemplateAsync(templateName, variables);
        await action.Should().ThrowAsync<EntityNotFoundException>()
            .WithMessage($"Notification template with name '{templateName}' not found.");
    }

    #endregion

    #region Template Activation Tests

    [Fact]
    public async Task ActivateTemplateAsync_WithValidId_ShouldActivateTemplate()
    {
        // Arrange
        var templateId = ObjectId.GenerateNewId();
        var template = new NotificationTemplate("Test Template", "email", "system", "Subject", "Content");
        template.Deactivate(); // Start with deactivated template

        _mockTemplateRepository.Setup(x => x.GetByIdAsync(templateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
        _mockTemplateRepository.Setup(x => x.UpdateAsync(It.IsAny<NotificationTemplate>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _notificationTemplateService.ActivateTemplateAsync(templateId);

        // Assert
        result.Should().NotBeNull();
        result.IsActive.Should().BeTrue();

        _mockTemplateRepository.Verify(x => x.UpdateAsync(It.IsAny<NotificationTemplate>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeactivateTemplateAsync_WithValidId_ShouldDeactivateTemplate()
    {
        // Arrange
        var templateId = ObjectId.GenerateNewId();
        var template = new NotificationTemplate("Test Template", "email", "system", "Subject", "Content");

        _mockTemplateRepository.Setup(x => x.GetByIdAsync(templateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
        _mockTemplateRepository.Setup(x => x.UpdateAsync(It.IsAny<NotificationTemplate>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _notificationTemplateService.DeactivateTemplateAsync(templateId);

        // Assert
        result.Should().NotBeNull();
        result.IsActive.Should().BeFalse();

        _mockTemplateRepository.Verify(x => x.UpdateAsync(It.IsAny<NotificationTemplate>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ActivateTemplateAsync_WithNonExistentId_ShouldThrowEntityNotFoundException()
    {
        // Arrange
        var templateId = ObjectId.GenerateNewId();

        _mockTemplateRepository.Setup(x => x.GetByIdAsync(templateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NotificationTemplate?)null);

        // Act & Assert
        var action = async () => await _notificationTemplateService.ActivateTemplateAsync(templateId);
        await action.Should().ThrowAsync<EntityNotFoundException>()
            .WithMessage($"Notification template with ID '{templateId}' not found.");
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task GetTemplatesByTypeAsync_WithEmptyType_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = async () => await _notificationTemplateService.GetTemplatesByTypeAsync("");
        await action.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("templateType");
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
    public async Task GetTemplatesByLanguageAsync_WithEmptyLanguage_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = async () => await _notificationTemplateService.GetTemplatesByLanguageAsync("");
        await action.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("language");
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
}