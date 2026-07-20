using Xunit;
using NSubstitute;

public class DoorIndicatorTests
{
    private readonly IDoorIndicatorStateNotifier _notifier;
    private readonly Guid _doorIndicatorId;
    private readonly DoorIndicator _doorIndicator;

    public DoorIndicatorTests()
    {
        _notifier = Substitute.For<IDoorIndicatorStateNotifier>();
        _doorIndicatorId = Guid.NewGuid();
        _doorIndicator = new DoorIndicator(_doorIndicatorId, _notifier);
    }

    [Fact]
    public void DoorIndicator_WhenCreated_IsIdle()
    {
        // Arrange
        // Act
        // Assert
        Assert.Equal(DoorIndicatorState.Idle, _doorIndicator.State);
    }

    [Theory]
    [MemberData(nameof(EventStateMap))]
    public void DoorIndicator_WhenEventReceived_BecomesExpectedState(
        EventType eventType, DoorIndicatorState expectedState)
    {
        // Arrange
        // Act
        _doorIndicator.OnEventReceived(eventType);

        // Assert
        Assert.Equal(expectedState, _doorIndicator.State);
    }

    [Theory]
    [MemberData(nameof(EventStateMap))]
    public void DoorIndicator_WhenEventReceived_NotifiesWithCorrectIdAndState(
        EventType eventType, DoorIndicatorState expectedState)
    {
        // Arrange
        // Act
        _doorIndicator.OnEventReceived(eventType);

        // Assert
        _notifier.Received(1).NotifyDoorIndicatorStateChanged(_doorIndicatorId, expectedState);
    }
    
    public static IEnumerable<object[]> EventStateMap =>
        new List<object[]>
        {
            new object[] { EventType.DoorBusy, DoorIndicatorState.Busy },
            new object[] { EventType.DoorIdle, DoorIndicatorState.Idle }
        };
}