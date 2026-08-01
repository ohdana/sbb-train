using Xunit;
using NSubstitute;

public class ExitDoorTests
{
    private readonly ITimeoutTimer _timer;
    private readonly IDoorStateNotifier _notifier;
    private readonly IExitDoorMechanism _mechanism;
    private readonly IDoorEventHandler _handler;
    private readonly Guid _doorId;
    private readonly ExitDoor _door;

    public ExitDoorTests()
    {
        _timer = Substitute.For<ITimeoutTimer>();
        _handler = Substitute.For<IDoorEventHandler>();
        _notifier = Substitute.For<IDoorStateNotifier>();
        _mechanism = Substitute.For<IExitDoorMechanism>();
        _doorId = Guid.NewGuid();
        _door = new ExitDoor(_doorId, _timer, _mechanism, _handler, _notifier);
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
        _timer.Received(1).Reset();
    }

    [Fact]
    public async Task Door_WhenCloseCalled_StopsTimer()
    {
        // Arrange
        await _door.OpenAsync();
        _timer.ClearReceivedCalls();

        // Act
        await _door.CloseAsync();

        // Assert
        _timer.Received(1).Stop();
    }

    [Fact]
    public void Door_WhenTimeoutOccurs_CallsHandler()
    {
        // Arrange
        // Act
        _door.TimeOut();

        // Assert
        _handler.Received(1).HandleTimeout(_doorId);
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