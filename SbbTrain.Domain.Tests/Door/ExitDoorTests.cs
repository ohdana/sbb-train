using Xunit;
using NSubstitute;

public class ExitDoorTests
{
    private readonly ITimeoutTimer _autoCloseTimer;
    private readonly IExitDoorMechanism _mechanism;
    private readonly IDoorStateNotifier _notifier;
    private readonly Guid _doorId;
    private readonly ExitDoor _door;

    public ExitDoorTests()
    {
        _autoCloseTimer = Substitute.For<ITimeoutTimer>();
        _notifier = Substitute.For<IDoorStateNotifier>();
        _mechanism = Substitute.For<IExitDoorMechanism>();
        _doorId = Guid.NewGuid();

        var timerDuration = TimeSpan.FromSeconds(60);
        var timerFactory = Substitute.For<ITimeoutTimerFactory>();
        timerFactory.Create(Arg.Any<TimeSpan>(), Arg.Any<ITimeoutable>())
                    .Returns(_autoCloseTimer);

        _door = new ExitDoor(_doorId, timerFactory, timerDuration, _mechanism, _notifier);
    }

    [Fact]
    public void Door_WhenCreated_IsClosed()
    {
        // Arrange
        // Act
        // Assert
        Assert.Equal(DoorState.Closed, _door.State);
    }

    [Fact]
    public async Task Door_WhenOpenCalled_ResetsTimer()
    {
        // Arrange
        // Act
        await _door.OpenAsync();

        // Assert
        _autoCloseTimer.Received(1).Reset();
    }

    [Fact]
    public async Task Door_WhenCloseCalled_StopsTimer()
    {
        // Arrange
        await _door.OpenAsync();
        _autoCloseTimer.ClearReceivedCalls();

        // Act
        await _door.CloseAsync();

        // Assert
        _autoCloseTimer.Received(1).Stop();
    }

    [Fact]
    public void Door_WhenTimeoutOccurs_RaisesTimedOut()
    {
        // Arrange
        var raised = false;
        _door.TimedOut += () => raised = true;

        // Act
        _door.TimeOut();

        // Assert
        Assert.True(raised);
    }

    [Fact]
    public async Task Door_WhenOpenCalled_BecomesOpened()
    {
        // Arrange
        await _door.CloseAsync();

        // Act
        await _door.OpenAsync();

        // Assert
        Assert.Equal(DoorState.Opened, _door.State);
    }

    [Fact]
    public async Task Door_WhenCloseCalled_BecomesClosed()
    {
        // Arrange
        await _door.OpenAsync();

        // Act
        await _door.CloseAsync();

        // Assert
        Assert.Equal(DoorState.Closed, _door.State);
    }

    [Fact]
    public async Task Door_WhenOpenCalled_NotifiesOpeningThenOpened()
    {
        // Arrange
        await _door.CloseAsync();
        _notifier.ClearReceivedCalls();

        // Act
        await _door.OpenAsync();

        // Assert
        Received.InOrder(() =>
        {
            _notifier.NotifyDoorStateChanged(_doorId, DoorState.Opening);
            _notifier.NotifyDoorStateChanged(_doorId, DoorState.Opened);
        });
    }

    [Fact]
    public async Task Door_WhenCloseCalled_NotifiesClosingThenClosed()
    {
        // Arrange
        await _door.OpenAsync();
        _notifier.ClearReceivedCalls();

        // Act
        await _door.CloseAsync();

        // Assert
        Received.InOrder(() =>
        {
            _notifier.NotifyDoorStateChanged(_doorId, DoorState.Closing);
            _notifier.NotifyDoorStateChanged(_doorId, DoorState.Closed);
        });
    }

    [Fact]
    public async Task Door_WhenObstructionDetectedDuringClosing_Reopens()
    {
        // Arrange
        var doorMechanismTcs = new TaskCompletionSource();
        _mechanism
            .CloseAsync(Arg.Any<CancellationToken>())
            .Returns(callInfo => 
            {
                var token = callInfo.Arg<CancellationToken>();
                token.Register(() => doorMechanismTcs.TrySetCanceled(token));
                return doorMechanismTcs.Task;
            });
        await _door.OpenAsync();
        _notifier.ClearReceivedCalls();

        // Act
        var closeTask = _door.CloseAsync();
        _door.OnEventReceived(EventType.ObstructionDetected);

        await closeTask;

        // Assert
        Received.InOrder(() =>
        {
            _notifier.NotifyDoorStateChanged(_doorId, DoorState.Opening);
            _notifier.NotifyDoorStateChanged(_doorId, DoorState.Opened);
        });
        Assert.Equal(DoorState.Opened, _door.State);
    }

    [Fact]
    public async Task Door_WhenMechanismThrowsUnexpectedExceptionOnOpen_TransitionsToFaultedAndRethrows()
    {
        // Arrange
        var mechanismException = new InvalidOperationException("Door jammed");
        _mechanism
            .OpenAsync()
            .Returns<Task>(_ => throw mechanismException);

        // Act
        var thrown = await Record.ExceptionAsync(() => _door.OpenAsync());

        // Assert
        Assert.Same(mechanismException, thrown);
        Assert.Equal(DoorState.Faulted, _door.State);
        _notifier.Received(1).NotifyDoorStateChanged(_doorId, DoorState.Faulted);
    }

    [Fact]
    public async Task Door_WhenMechanismThrowsUnexpectedExceptionOnClose_TransitionsToFaultedAndRethrows()
    {
        // Arrange
        var mechanismException = new InvalidOperationException("Door jammed");
        _mechanism
            .CloseAsync(Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw mechanismException);

        await _door.OpenAsync();
        _notifier.ClearReceivedCalls();

        // Act
        var thrown = await Record.ExceptionAsync(() => _door.CloseAsync());

        // Assert
        Assert.Same(mechanismException, thrown);
        Assert.Equal(DoorState.Faulted, _door.State);
        _notifier.Received(1).NotifyDoorStateChanged(_doorId, DoorState.Faulted);
    }
}