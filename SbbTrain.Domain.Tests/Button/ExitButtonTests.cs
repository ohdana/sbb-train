using Xunit;
using NSubstitute;

public class ExitButtonTests
{
    private readonly IButtonEventHandler _handler;
    private readonly IButtonStateNotifier _notifier;
    private readonly Guid _buttonId;
    private readonly ExitButton _button;

    public ExitButtonTests()
    {
        _handler = Substitute.For<IButtonEventHandler>();
        _notifier = Substitute.For<IButtonStateNotifier>();
        _buttonId = Guid.NewGuid();
        _button = new ExitButton(_buttonId, _handler, _notifier);
    }

    [Fact]
    public void Button_WhenCreated_IsIdle()
    {
        // Arrange
        // Act
        // Assert
        Assert.Equal(ButtonState.Idle, _button.State);
    }

    [Fact]
    public void Button_WhenPressed_CallsHandler()
    {
        // Arrange
        // Act
        _button.Press();

        // Assert
        _handler.Received(1).HandleButtonPressed(_buttonId);
    }

    [Fact]
    public void Button_WhenPressed_DoesntUpdateState()
    {
        // Arrange
        var currentState = _button.State;

        // Act
        _button.Press();

        // Assert
        Assert.Equal(currentState, _button.State);
    }

    [Theory]
    [MemberData(nameof(EventStateMap))]
    public void Button_WhenEventReceived_BecomesExpectedState(
        EventType eventType, ButtonState expectedState)
    {
        // Arrange
        // Act
        _button.OnEventReceived(eventType);

        // Assert
        Assert.Equal(expectedState, _button.State);
    }

    [Theory]
    [MemberData(nameof(EventStateMap))]
    public void Button_WhenEventReceived_NotifiesWithCorrectIdAndState(
        EventType eventType, ButtonState expectedState)
    {
        // Arrange
        // Act
        _button.OnEventReceived(eventType);

        // Assert
        _notifier.Received(1).NotifyButtonStateChanged(_buttonId, expectedState);
    }

    [Fact]
    public void Button_WhenUnknownEventReceived_ThrowsException()
    {
        // Arrange
        var unknownEvent = 999;

        // Act
        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() 
            => _button.OnEventReceived((EventType)unknownEvent));
    }
    
    public static IEnumerable<object[]> EventStateMap =>
        new List<object[]>
        {
            new object[] { EventType.ExitOpenRequested, ButtonState.Active },
            new object[] { EventType.DoorBusy, ButtonState.Busy },
            new object[] { EventType.DoorIdle, ButtonState.Idle },
            new object[] { EventType.DoorDisabled, ButtonState.Disabled },
        };
}