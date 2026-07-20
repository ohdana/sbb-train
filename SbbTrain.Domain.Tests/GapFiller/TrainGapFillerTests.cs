using Xunit;
using NSubstitute;

public class TrainGapFillerTests
{
    private readonly IGapFillerTimer _timer;
    private readonly IGapFillerEventHandler _handler;
    private readonly Guid _gapFillerId;
    private readonly TrainGapFiller _gapFiller;

    public TrainGapFillerTests()
    {
        _timer = Substitute.For<IGapFillerTimer>();
        _handler = Substitute.For<IGapFillerEventHandler>();
        _gapFillerId = Guid.NewGuid();
        _gapFiller = new TrainGapFiller(_gapFillerId, _timer, _handler);
    }

    [Fact]
    public void GapFiller_WhenCreated_IsRetracted()
    {
        // Arrange
        // Act
        // Assert
        Assert.Equal(GapFillerState.Retracted, _gapFiller.State);
    }

    [Fact]
    public void GapFiller_WhenRetractCalled_IsRetracted()
    {
        // Arrange
        _gapFiller.Extend();

        // Act
        _gapFiller.Retract();

        // Assert
        Assert.Equal(GapFillerState.Retracted, _gapFiller.State);
    }

    [Fact]
    public void GapFiller_WhenExtendCalled_IsExtended()
    {
        // Arrange
        _gapFiller.Retract();

        // Act
        _gapFiller.Extend();

        // Assert
        Assert.Equal(GapFillerState.Extended, _gapFiller.State);
    }

    [Fact]
    public void GapFiller_WhenExtendCalled_ResetsTimer()
    {
        // Arrange
        // Act
        _gapFiller.Extend();

        // Assert
        _timer.Received(1).Reset();
    }

    [Fact]
    public void GapFiller_WhenRetractCalled_StopsTimer()
    {
        // Arrange
        _gapFiller.Extend();

        // Act
        _gapFiller.Retract();

        // Assert
        _timer.Received(1).Stop();
    }

    [Fact]
    public void GapFiller_WhenTimeoutOccurs_CallsHandler()
    {
        // Arrange
        // Act
        _gapFiller.TimeOut();

        // Assert
        _handler.Received(1).HandleTimeout(_gapFillerId);
    }

    [Fact]
    public void GapFiller_WhenExitOpenRequested_ResetsTimer()
    {
        // Arrange
        // Act
        _gapFiller.OnEventReceived(EventType.ExitOpenRequested);

        // Assert
        _timer.Received(1).Reset();
    }
}