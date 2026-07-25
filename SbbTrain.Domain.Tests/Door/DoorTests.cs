using Xunit;
using NSubstitute;

public class DoorTests
{
    private readonly ITimeoutTimer _timer;
    private readonly IDoorStateNotifier _notifier;
    private readonly ITrainDoorMechanism _mechanism;
    private readonly IDoorEventHandler _handler;
    private readonly Guid _doorId;
    private readonly TrainDoor _door;

    public DoorTests()
    {
        _timer = Substitute.For<ITimeoutTimer>();
        _handler = Substitute.For<IDoorEventHandler>();
        _notifier = Substitute.For<IDoorStateNotifier>();
        _mechanism = Substitute.For<ITrainDoorMechanism>();
        _doorId = Guid.NewGuid();
        _door = new TrainDoor(_doorId, _timer, _mechanism, _handler, _notifier);
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
}