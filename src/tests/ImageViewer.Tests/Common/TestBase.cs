using AutoFixture;
using AutoFixture.Xunit2;
using Moq;

namespace ImageViewer.Tests.Common;

/// <summary>
/// Base class for all tests with common setup
/// </summary>
public abstract class TestBase
{
    protected readonly IFixture Fixture;

    protected TestBase()
    {
        Fixture = new Fixture();
        Fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => Fixture.Behaviors.Remove(b));
        Fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    /// <summary>
    /// Creates a mock with auto-generated data
    /// </summary>
    protected Mock<T> CreateMock<T>() where T : class
    {
        return new Mock<T>();
    }

    /// <summary>
    /// Creates a mock with specific setup
    /// </summary>
    protected Mock<T> CreateMock<T>(Action<Mock<T>> setup) where T : class
    {
        var mock = new Mock<T>();
        setup(mock);
        return mock;
    }
}
