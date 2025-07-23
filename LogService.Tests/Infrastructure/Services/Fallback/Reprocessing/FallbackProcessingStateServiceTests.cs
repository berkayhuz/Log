using LogService.Application.Options;
using LogService.Infrastructure.Services.Fallback.Reprocessing;

namespace LogService.Tests.Infrastructure.Services.Fallback.Reprocessing;

public class FallbackProcessingStateServiceTests
{
    [Fact]
    public void Current_Should_Return_Default_Options()
    {
        // Arrange
        var service = new FallbackProcessingStateService();

        // Act
        var current = service.Current;

        // Assert
        Assert.NotNull(current);
        Assert.True(current.EnableResilient);
        Assert.True(current.EnableRetry);
        Assert.True(current.EnableDirect);
        Assert.Equal(60, current.IntervalSeconds);
    }


    [Fact]
    public void UpdateOptions_Should_Update_All_Fields()
    {
        // Arrange
        var service = new FallbackProcessingStateService();
        var newOptions = new FallbackProcessingRuntimeOptions
        {
            EnableResilient = true,
            EnableRetry = true,
            EnableDirect = true,
            IntervalSeconds = 42
        };

        // Act
        service.UpdateOptions(newOptions);

        // Assert
        var updated = service.Current;

        Assert.True(updated.EnableResilient);
        Assert.True(updated.EnableRetry);
        Assert.True(updated.EnableDirect);
        Assert.Equal(42, updated.IntervalSeconds);
    }

    [Fact]
    public void UpdateOptions_Should_Throw_When_Null()
    {
        // Arrange
        var service = new FallbackProcessingStateService();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => service.UpdateOptions(null!));
    }

    [Fact]
    public void Should_Be_Thread_Safe()
    {
        // Arrange
        var service = new FallbackProcessingStateService();
        var options = new FallbackProcessingRuntimeOptions
        {
            EnableResilient = true,
            EnableRetry = true,
            EnableDirect = true,
            IntervalSeconds = 99
        };

        var threads = new List<Thread>();

        // Act
        for (int i = 0; i < 50; i++)
        {
            threads.Add(new Thread(() => service.UpdateOptions(options)));
            threads.Add(new Thread(() =>
            {
                var c = service.Current;
                _ = c.IntervalSeconds;
            }));
        }

        threads.ForEach(t => t.Start());
        threads.ForEach(t => t.Join());

        // Assert
        var final = service.Current;
        Assert.Equal(99, final.IntervalSeconds);
    }
}
