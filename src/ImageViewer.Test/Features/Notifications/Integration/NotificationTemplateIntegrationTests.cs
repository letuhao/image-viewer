using ImageViewer.Application.Services;
using ImageViewer.Test.Shared.Fixtures;

namespace ImageViewer.Test.Features.Notifications.Integration;

/// <summary>
/// Integration tests for Notification Templates - End-to-end template management scenarios
/// </summary>
public class NotificationTemplateIntegrationTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;
    private readonly INotificationTemplateService _templateService;

    public NotificationTemplateIntegrationTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
        _templateService = _fixture.GetService<INotificationTemplateService>();
    }

    [Fact]
    public async Task TemplateManagement_CreateTemplate_ShouldCreateSuccessfully()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var request = new Application.DTOs.Notifications.CreateNotificationTemplateRequest
        {
            TemplateName = "Test Template",
            TemplateType = "email",
            Category = "system",
            Subject = "Test Subject",
            Content = "Test Body with {{userName}} and {{message}}",
            Priority = "normal",
            Language = "en",
            IsSystemTemplate = false
        };

        // Act
        var result = await _templateService.CreateTemplateAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.TemplateName.Should().Be("Test Template");
        result.Subject.Should().Be("Test Subject");
        result.Content.Should().Be("Test Body with {{userName}} and {{message}}");
        result.TemplateType.Should().Be("email");
        result.IsActive.Should().BeTrue();
        result.Variables.Should().HaveCount(2);
        result.Variables.Should().Contain("userName");
        result.Variables.Should().Contain("message");
    }

    [Fact]
    public async Task TemplateManagement_UpdateTemplate_ShouldUpdateSuccessfully()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var createRequest = new Application.DTOs.Notifications.CreateNotificationTemplateRequest
        {
            TemplateName = "Original Template",
            TemplateType = "email",
            Category = "system",
            Subject = "Original Subject",
            Content = "Original Body",
            Priority = "normal",
            Language = "en",
            IsSystemTemplate = false
        };

        var createdTemplate = await _templateService.CreateTemplateAsync(createRequest);

        var updateRequest = new Application.DTOs.Notifications.UpdateNotificationTemplateRequest
        {
            TemplateName = "Updated Template",
            Subject = "Updated Subject",
            Content = "Updated Body with {{userName}}",
            IsActive = false
        };

        // Act
        var result = await _templateService.UpdateTemplateAsync(createdTemplate.Id, updateRequest);

        // Assert
        result.Should().NotBeNull();
        result.TemplateName.Should().Be("Updated Template");
        result.Subject.Should().Be("Updated Subject");
        result.Content.Should().Be("Updated Body with {{userName}}");
        result.IsActive.Should().BeFalse();
        result.Variables.Should().HaveCount(1);
        result.Variables.Should().Contain("userName");
    }

    [Fact]
    public async Task TemplateManagement_DeleteTemplate_ShouldDeleteSuccessfully()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var createRequest = new Application.DTOs.Notifications.CreateNotificationTemplateRequest
        {
            TemplateName = "Template to Delete",
            TemplateType = "email",
            Category = "system",
            Subject = "Delete Subject",
            Content = "Delete Body",
            Priority = "normal",
            Language = "en",
            IsSystemTemplate = false
        };

        var createdTemplate = await _templateService.CreateTemplateAsync(createRequest);

        // Act
        await _templateService.DeleteTemplateAsync(createdTemplate.Id);

        // Assert
        var deletedTemplate = await _templateService.GetTemplateByIdAsync(createdTemplate.Id);
        deletedTemplate.Should().BeNull();
    }

    [Fact]
    public async Task TemplateManagement_GetTemplate_ShouldReturnTemplate()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var createRequest = new Application.DTOs.Notifications.CreateNotificationTemplateRequest
        {
            TemplateName = "Get Template",
            TemplateType = "email",
            Category = "system",
            Subject = "Get Subject",
            Content = "Get Body",
            Priority = "normal",
            Language = "en",
            IsSystemTemplate = false
        };

        var createdTemplate = await _templateService.CreateTemplateAsync(createRequest);

        // Act
        var result = await _templateService.GetTemplateByIdAsync(createdTemplate.Id);

        // Assert
        result.Should().NotBeNull();
        result.TemplateName.Should().Be("Get Template");
        result.Subject.Should().Be("Get Subject");
        result.Content.Should().Be("Get Body");
        result.TemplateType.Should().Be("email");
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task TemplateManagement_GetTemplates_ShouldReturnAllTemplates()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var templates = new List<Application.DTOs.Notifications.CreateNotificationTemplateRequest>
        {
            new Application.DTOs.Notifications.CreateNotificationTemplateRequest
            {
                TemplateName = "Template 1",
                TemplateType = "email",
                Category = "system",
                Subject = "Subject 1",
                Content = "Body 1",
                Priority = "normal",
                Language = "en",
                IsSystemTemplate = false
            },
            new Application.DTOs.Notifications.CreateNotificationTemplateRequest
            {
                TemplateName = "Template 2",
                TemplateType = "email",
                Category = "system",
                Subject = "Subject 2",
                Content = "Body 2",
                Priority = "normal",
                Language = "en",
                IsSystemTemplate = false
            }
        };

        foreach (var template in templates)
        {
            await _templateService.CreateTemplateAsync(template);
        }

        // Act
        var result = await _templateService.GetAllTemplatesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().Contain(t => t.TemplateName == "Template 1");
        result.Should().Contain(t => t.TemplateName == "Template 2");
    }

    [Fact]
    public async Task TemplateManagement_GetTemplatesByType_ShouldReturnFilteredTemplates()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var templates = new List<Application.DTOs.Notifications.CreateNotificationTemplateRequest>
        {
            new Application.DTOs.Notifications.CreateNotificationTemplateRequest
            {
                TemplateName = "Info Template",
                TemplateType = "email",
                Category = "system",
                Subject = "Info Subject",
                Content = "Info Body",
                Priority = "normal",
                Language = "en",
                IsSystemTemplate = false
            },
            new Application.DTOs.Notifications.CreateNotificationTemplateRequest
            {
                TemplateName = "Warning Template",
                TemplateType = "push",
                Category = "system",
                Subject = "Warning Subject",
                Content = "Warning Body",
                Priority = "high",
                Language = "en",
                IsSystemTemplate = false
            }
        };

        foreach (var template in templates)
        {
            await _templateService.CreateTemplateAsync(template);
        }

        // Act
        var result = await _templateService.GetTemplatesByTypeAsync("email");

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().TemplateName.Should().Be("Info Template");
        result.First().TemplateType.Should().Be("email");
    }

    [Fact]
    public async Task TemplateManagement_ValidateTemplate_ShouldValidateSuccessfully()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var request = new Application.DTOs.Notifications.CreateNotificationTemplateRequest
        {
            TemplateName = "Valid Template",
            TemplateType = "email",
            Category = "system",
            Subject = "Valid Subject",
            Content = "Valid Body with {{userName}} and {{message}}",
            Priority = "normal",
            Language = "en",
            IsSystemTemplate = false
        };

        // Act
        var result = await _templateService.CreateTemplateAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.TemplateName.Should().Be("Valid Template");
        result.Subject.Should().Be("Valid Subject");
        result.Content.Should().Be("Valid Body with {{userName}} and {{message}}");
    }

    [Fact]
    public async Task TemplateManagement_RenderTemplate_ShouldRenderSuccessfully()
    {
        // Arrange
        await _fixture.CleanupTestDataAsync();
        var createRequest = new Application.DTOs.Notifications.CreateNotificationTemplateRequest
        {
            TemplateName = "Stats Template",
            TemplateType = "email",
            Category = "system",
            Subject = "Stats Subject",
            Content = "Stats Body",
            Priority = "normal",
            Language = "en",
            IsSystemTemplate = false
        };

        var createdTemplate = await _templateService.CreateTemplateAsync(createRequest);

        // Act
        var result = await _templateService.GetTemplateByIdAsync(createdTemplate.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(createdTemplate.Id);
        result.TemplateName.Should().Be("Stats Template");
    }
}
