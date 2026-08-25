using Xunit;
using NSubstitute;

public class TrainGapFillerTests
{
    private readonly ITimeoutTimer _autoRetractTimer;
    private readonly IGapFillerMechanism _mechanism;
    private readonly IGapFillerStateNotifier _notifier;
    private readonly Guid _gapFillerId;
    private readonly TrainGapFiller _gapFiller;

    public TrainGapFillerTests()
    {
        _autoRetractTimer = Substitute.For<ITimeoutTimer>();
        _mechanism = Substitute.For<IGapFillerMechanism>();
        _notifier = Substitute.For<IGapFillerStateNotifier>();
        _gapFillerId = Guid.NewGuid();

        var timerDuration = TimeSpan.FromSeconds(300);
        var timerFactory = Substitute.For<ITimeoutTimerFactory>();
        timerFactory.Create(Arg.Any<TimeSpan>(), Arg.Any<ITimeoutable>())
                    .Returns(_autoRetractTimer);

        _gapFiller = new TrainGapFiller(_gapFillerId, timerFactory, timerDuration, _mechanism, _notifier);
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
    public async Task GapFiller_WhenRetractCalled_BecomesRetracted()
    {
        // Arrange
        await _gapFiller.ExtendAsync();

        // Act
        await _gapFiller.RetractAsync();

        // Assert
        Assert.Equal(GapFillerState.Retracted, _gapFiller.State);
    }

    [Fact]
    public async Task GapFiller_WhenExtendCalled_BecomesExtended()
    {
        // Arrange
        await _gapFiller.RetractAsync();

        // Act
        await _gapFiller.ExtendAsync();

        // Assert
        Assert.Equal(GapFillerState.Extended, _gapFiller.State);
    }

    [Fact]
    public void GapFiller_WhenResetAutoRetractTimerCalled_ResetsTimer()
    {
        // Arrange
        _gapFiller.StopAutoRetractTimer();
        _autoRetractTimer.ClearReceivedCalls();

        // Act
        _gapFiller.ResetAutoRetractTimer();

        // Assert
        _autoRetractTimer.Received(1).Reset();
    }

    [Fact]
    public void GapFiller_WhenStopAutoRetractTimerCalled_StopsTimer()
    {
        // Arrange
        _gapFiller.ResetAutoRetractTimer();
        _autoRetractTimer.ClearReceivedCalls();

        // Act
        _gapFiller.StopAutoRetractTimer();

        // Assert
        _autoRetractTimer.Received(1).Stop();
    }

    [Fact]
    public void GapFiller_WhenTimeoutOccurs_RaisesTimedOut()
    {
        // Arrange
        var raised = false;
        _gapFiller.TimedOut += () => raised = true;

        // Act
        _gapFiller.TimeOut();

        // Assert
        Assert.True(raised);
    }

    [Fact]
    public async Task GapFiller_WhenMechanismThrowsUnexpectedExceptionOnExtend_TransitionsToFaultedAndRethrows()
    {
        // Arrange
        var mechanismException = new InvalidOperationException("Gap filler jammed");
        _mechanism
            .ExtendAsync()
            .Returns<Task>(_ => throw mechanismException);

        await _gapFiller.RetractAsync();
        _notifier.ClearReceivedCalls();

        // Act
        var thrown = await Record.ExceptionAsync(() => _gapFiller.ExtendAsync());

        // Assert
        Assert.Same(mechanismException, thrown);
        Assert.Equal(GapFillerState.Faulted, _gapFiller.State);
        _notifier.Received(1).NotifyGapFillerStateChanged(_gapFillerId, GapFillerState.Faulted);
    }

    [Fact]
    public async Task GapFiller_WhenMechanismThrowsUnexpectedExceptionOnRetract_TransitionsToFaultedAndRethrows()
    {
        // Arrange
        var mechanismException = new InvalidOperationException("Gap filler jammed");
        _mechanism
            .RetractAsync()
            .Returns<Task>(_ => throw mechanismException);

        await _gapFiller.ExtendAsync();
        _notifier.ClearReceivedCalls();

        // Act
        var thrown = await Record.ExceptionAsync(() => _gapFiller.RetractAsync());

        // Assert
        Assert.Same(mechanismException, thrown);
        Assert.Equal(GapFillerState.Faulted, _gapFiller.State);
        _notifier.Received(1).NotifyGapFillerStateChanged(_gapFillerId, GapFillerState.Faulted);
    }
}