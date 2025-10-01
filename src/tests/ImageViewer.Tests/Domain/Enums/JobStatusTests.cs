using ImageViewer.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace ImageViewer.Tests.Domain.Enums;

public class JobStatusTests
{
    [Theory]
    [InlineData(JobStatus.Pending, 0)]
    [InlineData(JobStatus.Running, 1)]
    [InlineData(JobStatus.Completed, 2)]
    [InlineData(JobStatus.Failed, 3)]
    [InlineData(JobStatus.Cancelled, 4)]
    public void JobStatus_ShouldHaveCorrectValues(JobStatus jobStatus, int expectedValue)
    {
        // Act
        var actualValue = (int)jobStatus;

        // Assert
        actualValue.Should().Be(expectedValue);
    }

    [Fact]
    public void JobStatus_ShouldHaveAllExpectedValues()
    {
        // Arrange
        var expectedValues = new[] { 0, 1, 2, 3, 4 };
        var actualValues = Enum.GetValues<JobStatus>().Select(x => (int)x).ToArray();

        // Assert
        actualValues.Should().BeEquivalentTo(expectedValues);
    }

    [Fact]
    public void JobStatus_ShouldHaveCorrectCount()
    {
        // Act
        var count = Enum.GetValues<JobStatus>().Length;

        // Assert
        count.Should().Be(5);
    }

    [Theory]
    [InlineData(JobStatus.Pending, "Pending")]
    [InlineData(JobStatus.Running, "Running")]
    [InlineData(JobStatus.Completed, "Completed")]
    [InlineData(JobStatus.Failed, "Failed")]
    [InlineData(JobStatus.Cancelled, "Cancelled")]
    public void JobStatus_ShouldHaveCorrectStringRepresentation(JobStatus jobStatus, string expectedString)
    {
        // Act
        var actualString = jobStatus.ToString();

        // Assert
        actualString.Should().Be(expectedString);
    }

    [Fact]
    public void JobStatus_ShouldBeParseableFromString()
    {
        // Arrange
        var stringValues = new[] { "Pending", "Running", "Completed", "Failed", "Cancelled" };
        var expectedValues = new[] { JobStatus.Pending, JobStatus.Running, JobStatus.Completed, JobStatus.Failed, JobStatus.Cancelled };

        // Act & Assert
        for (int i = 0; i < stringValues.Length; i++)
        {
            var parsed = Enum.Parse<JobStatus>(stringValues[i]);
            parsed.Should().Be(expectedValues[i]);
        }
    }

    [Fact]
    public void JobStatus_ShouldBeParseableFromInt()
    {
        // Arrange
        var intValues = new[] { 0, 1, 2, 3, 4 };
        var expectedValues = new[] { JobStatus.Pending, JobStatus.Running, JobStatus.Completed, JobStatus.Failed, JobStatus.Cancelled };

        // Act & Assert
        for (int i = 0; i < intValues.Length; i++)
        {
            var parsed = (JobStatus)intValues[i];
            parsed.Should().Be(expectedValues[i]);
        }
    }

    [Theory]
    [InlineData(JobStatus.Pending, JobStatus.Running, true)]
    [InlineData(JobStatus.Running, JobStatus.Completed, true)]
    [InlineData(JobStatus.Running, JobStatus.Failed, true)]
    [InlineData(JobStatus.Running, JobStatus.Cancelled, true)]
    [InlineData(JobStatus.Completed, JobStatus.Running, false)]
    [InlineData(JobStatus.Failed, JobStatus.Running, false)]
    [InlineData(JobStatus.Cancelled, JobStatus.Running, false)]
    public void JobStatus_ShouldHaveValidTransitions(JobStatus from, JobStatus to, bool isValid)
    {
        // Act
        var canTransition = IsValidTransition(from, to);

        // Assert
        canTransition.Should().Be(isValid);
    }

    private static bool IsValidTransition(JobStatus from, JobStatus to)
    {
        return from switch
        {
            JobStatus.Pending => to == JobStatus.Running,
            JobStatus.Running => to is JobStatus.Completed or JobStatus.Failed or JobStatus.Cancelled,
            JobStatus.Completed => false,
            JobStatus.Failed => false,
            JobStatus.Cancelled => false,
            _ => false
        };
    }
}
