using Xunit;
using NSubstitute;

public class TrainExitIntegrationTests
{
    private readonly IExitDoorMechanism _doorMechanismA;
    private readonly IExitDoorMechanism _doorMechanismB;
    private readonly IGapFillerMechanism _gapFillerMechanismA;
    private readonly IGapFillerMechanism _gapFillerMechanismB;
    private readonly ITrainNotifier _notifier;
    private readonly ITrainLogger _logger;
    private readonly TrainExitTestGraph _graph;
    private const int BUTTONS_IDLE_TIMEOUT_SECONDS = 1;

    public TrainExitIntegrationTests()
    {
        _doorMechanismA = Substitute.For<IExitDoorMechanism>();
        _doorMechanismB = Substitute.For<IExitDoorMechanism>();
        _gapFillerMechanismA = Substitute.For<IGapFillerMechanism>();
        _gapFillerMechanismB = Substitute.For<IGapFillerMechanism>();
        _notifier = Substitute.For<ITrainNotifier>();
        _logger = Substitute.For<ITrainLogger>();

        _graph = TestCompositionRoot.CreateTrainExit(
            _doorMechanismA, _doorMechanismB,
            _gapFillerMechanismA, _gapFillerMechanismB,
            _notifier, _logger);
    }

    [Theory]
    [MemberData(nameof(SafeSideTypeAndButtonIndexCombinations))]
    public async Task TrainExit_WhenEnabledThenButtonPressedOnSafeSide_GapFillerExtendedAndDoorOpenedAndButtonsIdleOnSafeSide(
        TrainSideType safeSideType, int buttonIndex)
    {
        // Arrange
        var (safeSideGraph, otherSideGraph) = GetSideGraphs(safeSideType);
        var button = safeSideGraph.Buttons.ElementAt(buttonIndex);

        _graph.Exit.Enable(safeSideType);

        // Act
        await PressButtonsAndWaitUntilIdle(new List<IExitButton> { button });

        // Assert
        AssertSafeSideOpenedAndAllButtonsIdle(safeSideGraph, otherSideGraph);
    }

    [Theory]
    [MemberData(nameof(SafeSideTypeAndButtonIndexCombinations))]
    public async Task TrainExit_WhenEnabledThenButtonPressedOnUnsafeSide_GapFillerExtendedAndDoorOpenedAndButtonsIdleOnSafeSide(
        TrainSideType safeSideType, int buttonIndex)
    {
        // Arrange
        var (safeSideGraph, otherSideGraph) = GetSideGraphs(safeSideType);
        var button = otherSideGraph.Buttons.ElementAt(buttonIndex);

        _graph.Exit.Enable(safeSideType);

        // Act
        await PressButtonsAndWaitUntilIdle(new List<IExitButton> { button });

        // Assert
        AssertSafeSideOpenedAndAllButtonsIdle(safeSideGraph, otherSideGraph);
    }

    [Theory]
    [InlineData(TrainSideType.A)]
    [InlineData(TrainSideType.B)]
    public async Task TrainExit_WhenEnabledThenAllButtonsPressed_GapFillerExtendedAndDoorOpenedAndButtonsIdleOnSafeSide(
        TrainSideType safeSideType)
    {
        // Arrange
        var (safeSideGraph, otherSideGraph) = GetSideGraphs(safeSideType);
        var allButtons = safeSideGraph.Buttons.Concat(otherSideGraph.Buttons);

        _graph.Exit.Enable(safeSideType);

        // Act
        await PressButtonsAndWaitUntilIdle(allButtons);

        // Assert
        AssertSafeSideOpenedAndAllButtonsIdle(safeSideGraph, otherSideGraph);
    }

    [Theory]
    [MemberData(nameof(SafeSideTypeAndButtonIndexCombinations))]
    public async Task TrainExit_WhenDisabledThenButtonPressedOnSafeSideThenEnabled_GapFillerExtendedAndDoorOpenedAndButtonsIdleOnSafeSide(
        TrainSideType safeSideType, int buttonIndex)
    {
        // Arrange
        var (safeSideGraph, otherSideGraph) = GetSideGraphs(safeSideType);
        var button = safeSideGraph.Buttons.ElementAt(buttonIndex);
        var buttonsIdle = WaitUntilButtonsIdle(new List<IExitButton> { button });

        _graph.Exit.Disable();
        button.Press();

        // Act
        _graph.Exit.Enable(safeSideType);
        await buttonsIdle.WaitAsync(TimeSpan.FromSeconds(BUTTONS_IDLE_TIMEOUT_SECONDS));

        // Assert
        AssertSafeSideOpenedAndAllButtonsIdle(safeSideGraph, otherSideGraph);
    }

    [Theory]
    [InlineData(TrainSideType.A)]
    [InlineData(TrainSideType.B)]
    public async Task TrainExit_WhenDisabledThenAllButtonsPressedThenEnabled_GapFillerExtendedAndDoorOpenedAndButtonsIdleOnSafeSide(
        TrainSideType safeSideType)
    {
        // Arrange
        var (safeSideGraph, otherSideGraph) = GetSideGraphs(safeSideType);
        var allButtons = safeSideGraph.Buttons.Concat(otherSideGraph.Buttons);
        var buttonsIdle = WaitUntilButtonsIdle(allButtons);

        _graph.Exit.Disable();
        await PressButtonsSimultaneously(allButtons);

        // Act
        _graph.Exit.Enable(safeSideType);
        await buttonsIdle.WaitAsync(TimeSpan.FromSeconds(BUTTONS_IDLE_TIMEOUT_SECONDS));

        // Assert
        AssertSafeSideOpenedAndAllButtonsIdle(safeSideGraph, otherSideGraph);
    }

    [Theory]
    [MemberData(nameof(SafeSideTypeAndButtonIndexCombinations))]
    public async Task TrainExit_WhenDisabledThenButtonPressed_AllButtonsActive(
        TrainSideType safeSideType, int buttonIndex)
    {
        // Arrange
        var (oneSideGraph, otherSideGraph) = GetSideGraphs(safeSideType);
        var button = oneSideGraph.Buttons.ElementAt(buttonIndex);
        var allButtons = oneSideGraph.Buttons.Concat(otherSideGraph.Buttons);
        var buttonsActive = WaitUntilButtonsActive(allButtons);

        _graph.Exit.Disable();

        // Act
        button.Press();
        await buttonsActive.WaitAsync(TimeSpan.FromSeconds(BUTTONS_IDLE_TIMEOUT_SECONDS));

        // Assert
        foreach (var b in allButtons)
        {
            Assert.Equal(ButtonState.Active, b.State);   
        }
    }

    [Theory]
    [InlineData(TrainSideType.A)]
    [InlineData(TrainSideType.B)]
    public async Task TrainExit_WhenDoorOpening_DoorIndicatorAndSButtonsBusy(TrainSideType safeSideType)
    {
        // Arrange
        var (safeSideGraph, otherSideGraph) = GetSideGraphs(safeSideType);
        var allButtons = safeSideGraph.Buttons.Concat(otherSideGraph.Buttons);

        _graph.Exit.Enable(safeSideType);

        DoorIndicatorState? capturedIndicatorState  = null;
        ButtonState? capturedButtonState = null;

        safeSideGraph.Door.StateChanged += () =>
        {
            if (safeSideGraph.Door.State == DoorState.Opening)
            {
                capturedIndicatorState = safeSideGraph.DoorIndicator.State;
                var buttonsStates = allButtons.Select(b => b.State).Distinct().ToList();
                capturedButtonState = buttonsStates is [var soleState]
                    ? soleState 
                    : null;
            }
        };

        // Act
        await _graph.Exit.OpenAsync();

        // Assert
        Assert.Equal(DoorIndicatorState.Busy, capturedIndicatorState);
        Assert.Equal(ButtonState.Busy, capturedButtonState);
    }

    private Task WaitUntilButtonsIdle(IEnumerable<IExitButton> buttons)
        => WaitUntilButtonsInState(buttons, ButtonState.Idle);

    private Task WaitUntilButtonsActive(IEnumerable<IExitButton> buttons)
        => WaitUntilButtonsInState(buttons, ButtonState.Active);

    private Task WaitUntilButtonsInState(IEnumerable<IExitButton> buttons, ButtonState state)
    {
        var tcs = new TaskCompletionSource();
        _notifier
            .When(x => x.NotifyButtonStateChanged(Arg.Any<Guid>(), Arg.Any<ButtonState>()))
            .Do(_ =>
            {
                if (buttons.All(b => b.State == state))
                {
                    tcs.TrySetResult();
                }
            });   

        return tcs.Task;
    }

    private async Task PressButtonsAndWaitUntilIdle(IEnumerable<IExitButton> buttons)
    {
        var buttonsIdle = WaitUntilButtonsIdle(buttons);
        await PressButtonsSimultaneously(buttons);
        await buttonsIdle.WaitAsync(TimeSpan.FromSeconds(BUTTONS_IDLE_TIMEOUT_SECONDS));
    }

    private async Task PressButtonsSimultaneously(IEnumerable<IExitButton> buttons)
    {
        var buttonsPressed = buttons.Select(button => Task.Run(() => button.Press()))
                                    .ToArray();
        await Task.WhenAll(buttonsPressed);
    }

    private (TrainExitSideTestGraph, TrainExitSideTestGraph) GetSideGraphs(TrainSideType safeSideType)
    {
        return safeSideType == TrainSideType.A ?
            (_graph.SideA, _graph.SideB) : (_graph.SideB, _graph.SideA);
    }

    private (IExitDoorMechanism, IExitDoorMechanism) GetDoorMechanisms(TrainSideType safeSideType)
    {
        return safeSideType == TrainSideType.A ?
            (_doorMechanismA, _doorMechanismB) : (_doorMechanismB, _doorMechanismA);
    }

    public static IEnumerable<object[]> SafeSideTypeAndButtonIndexCombinations()
    {
        var sideTypes = Enum.GetValues<TrainSideType>();
        var nOfButtons = TestCompositionRoot.NOfButtonsPerSide;

        foreach (var sideType in sideTypes)
        {
            foreach (var buttonIndex in Enumerable.Range(0, nOfButtons))
            {
                yield return new object[] { sideType, buttonIndex };
            }
        }
    }

    private void AssertSafeSideOpenedAndAllButtonsIdle(
        TrainExitSideTestGraph safeSideGraph, TrainExitSideTestGraph otherSideGraph)
    {
        Assert.Equal(DoorState.Opened, safeSideGraph.Door.State);
        Assert.Equal(DoorState.Closed, otherSideGraph.Door.State);

        Assert.Equal(GapFillerState.Extended, safeSideGraph.GapFiller.State);
        Assert.Equal(GapFillerState.Retracted, otherSideGraph.GapFiller.State);

        var allButtons = safeSideGraph.Buttons.Concat(otherSideGraph.Buttons);
        foreach (var button in allButtons)
        {
            Assert.Equal(ButtonState.Idle, button.State);   
        }
    }
}
