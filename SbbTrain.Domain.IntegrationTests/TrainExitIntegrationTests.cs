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

        _graph = new TrainExitTestGraphBuilder()
            .WithDoorMechanismA(_doorMechanismA)
            .WithDoorMechanismB(_doorMechanismB)
            .WithGapFillerMechanismA(_gapFillerMechanismA)
            .WithGapFillerMechanismB(_gapFillerMechanismB)
            .WithNotifier(_notifier)
            .WithLogger(_logger)
            .Build();

    }

    [Theory]
    [MemberData(nameof(SafeSideTypeAndButtonIndexCombinations))]
    public async Task TrainExit_WhenEnabledThenButtonPressedOnSafeSide_GapFillerExtendedAndDoorOpenedAndButtonsIdleOnSafeSide(
        TrainSideType safeSideType, int buttonIndex)
    {
        // Arrange
        var (safeSideGraph, otherSideGraph) = GetSideGraphs(_graph, safeSideType);
        var button = safeSideGraph.Buttons.ElementAt(buttonIndex);

        _graph.Exit.Enable(safeSideType);

        // Act
        await PressButtonsAndWaitUntilIdle(new List<IExitButton> { button });

        // Assert
        AssertOnlySafeSideOpenedAndAllButtonsIdle(safeSideGraph, otherSideGraph);
    }

    [Theory]
    [MemberData(nameof(SafeSideTypeAndButtonIndexCombinations))]
    public async Task TrainExit_WhenEnabledThenButtonPressedOnUnsafeSide_GapFillerExtendedAndDoorOpenedAndButtonsIdleOnSafeSide(
        TrainSideType safeSideType, int buttonIndex)
    {
        // Arrange
        var (safeSideGraph, otherSideGraph) = GetSideGraphs(_graph, safeSideType);
        var button = otherSideGraph.Buttons.ElementAt(buttonIndex);

        _graph.Exit.Enable(safeSideType);

        // Act
        await PressButtonsAndWaitUntilIdle(new List<IExitButton> { button });

        // Assert
        AssertOnlySafeSideOpenedAndAllButtonsIdle(safeSideGraph, otherSideGraph);
    }

    [Theory]
    [InlineData(TrainSideType.A)]
    [InlineData(TrainSideType.B)]
    public async Task TrainExit_WhenEnabledThenAllButtonsPressed_GapFillerExtendedAndDoorOpenedAndButtonsIdleOnSafeSide(
        TrainSideType safeSideType)
    {
        // Arrange
        var (safeSideGraph, otherSideGraph) = GetSideGraphs(_graph, safeSideType);
        var allButtons = safeSideGraph.Buttons.Concat(otherSideGraph.Buttons);

        _graph.Exit.Enable(safeSideType);

        // Act
        await PressButtonsAndWaitUntilIdle(allButtons);

        // Assert
        AssertOnlySafeSideOpenedAndAllButtonsIdle(safeSideGraph, otherSideGraph);
    }

    [Theory]
    [MemberData(nameof(SafeSideTypeAndButtonIndexCombinations))]
    public async Task TrainExit_WhenDisabledThenButtonPressedOnSafeSideThenEnabled_GapFillerExtendedAndDoorOpenedAndButtonsIdleOnSafeSide(
        TrainSideType safeSideType, int buttonIndex)
    {
        // Arrange
        var (safeSideGraph, otherSideGraph) = GetSideGraphs(_graph, safeSideType);
        var button = safeSideGraph.Buttons.ElementAt(buttonIndex);
        var buttonsIdle = WaitUntilButtonsIdle(new List<IExitButton> { button });

        _graph.Exit.Disable();
        button.Press();

        // Act
        _graph.Exit.Enable(safeSideType);
        await buttonsIdle.WaitAsync(TimeSpan.FromSeconds(BUTTONS_IDLE_TIMEOUT_SECONDS));

        // Assert
        AssertOnlySafeSideOpenedAndAllButtonsIdle(safeSideGraph, otherSideGraph);
    }

    [Theory]
    [MemberData(nameof(SafeSideTypeAndButtonIndexCombinations))]
    public async Task TrainExit_WhenDisabledThenButtonPressedOnUnsafeSideThenEnabled_GapFillerExtendedAndDoorOpenedAndButtonsIdleOnSafeSide(
        TrainSideType safeSideType, int buttonIndex)
    {
        // Arrange
        var (safeSideGraph, otherSideGraph) = GetSideGraphs(_graph, safeSideType);
        var button = otherSideGraph.Buttons.ElementAt(buttonIndex);
        var buttonsIdle = WaitUntilButtonsIdle(new List<IExitButton> { button });

        _graph.Exit.Disable();
        button.Press();

        // Act
        _graph.Exit.Enable(safeSideType);
        await buttonsIdle.WaitAsync(TimeSpan.FromSeconds(BUTTONS_IDLE_TIMEOUT_SECONDS));

        // Assert
        AssertOnlySafeSideOpenedAndAllButtonsIdle(safeSideGraph, otherSideGraph);
    }

    [Theory]
    [InlineData(TrainSideType.A)]
    [InlineData(TrainSideType.B)]
    public async Task TrainExit_WhenDisabledThenAllButtonsPressedThenEnabled_GapFillerExtendedAndDoorOpenedAndButtonsIdleOnSafeSide(
        TrainSideType safeSideType)
    {
        // Arrange
        var (safeSideGraph, otherSideGraph) = GetSideGraphs(_graph, safeSideType);
        var allButtons = safeSideGraph.Buttons.Concat(otherSideGraph.Buttons);
        var buttonsIdle = WaitUntilButtonsIdle(allButtons);

        _graph.Exit.Disable();
        await PressButtonsSimultaneously(allButtons);

        // Act
        _graph.Exit.Enable(safeSideType);
        await buttonsIdle.WaitAsync(TimeSpan.FromSeconds(BUTTONS_IDLE_TIMEOUT_SECONDS));

        // Assert
        AssertOnlySafeSideOpenedAndAllButtonsIdle(safeSideGraph, otherSideGraph);
    }

    [Theory]
    [MemberData(nameof(SafeSideTypeAndButtonIndexCombinations))]
    public async Task TrainExit_WhenDisabledThenButtonPressed_AllButtonsActive(TrainSideType safeSideType, int buttonIndex)
    {
        // Arrange
        var (oneSideGraph, otherSideGraph) = GetSideGraphs(_graph, safeSideType);
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
    public async Task TrainExit_WhenDoorOpening_DoorIndicatorAndButtonsBusy(TrainSideType safeSideType)
    {
        // Arrange
        var (safeSideGraph, otherSideGraph) = GetSideGraphs(_graph, safeSideType);
        var allButtons = safeSideGraph.Buttons.Concat(otherSideGraph.Buttons);

        _graph.Exit.Enable(safeSideType);

        DoorIndicatorState? capturedIndicatorState  = null;
        ButtonState? capturedButtonState = null;
        safeSideGraph.Door.StateChanged += () =>
        {
            if (safeSideGraph.Door.State != DoorState.Opening)
            {
                return;
            }

            capturedIndicatorState = safeSideGraph.DoorIndicator.State;

            var buttonsStates = allButtons.Select(b => b.State).Distinct().ToList();
            capturedButtonState = buttonsStates is [var singleValue] ? singleValue : null;
        };

        // Act
        await _graph.Exit.OpenAsync();

        // Assert
        Assert.Equal(DoorIndicatorState.Busy, capturedIndicatorState);
        Assert.Equal(ButtonState.Busy, capturedButtonState);
    }

    [Theory]
    [InlineData(TrainSideType.A)]
    [InlineData(TrainSideType.B)]
    public async Task TrainExit_WhenDoorClosing_DoorIndicatorAndButtonsBusy(TrainSideType safeSideType)
    {
        // Arrange
        var (safeSideGraph, otherSideGraph) = GetSideGraphs(_graph, safeSideType);
        var allButtons = safeSideGraph.Buttons.Concat(otherSideGraph.Buttons);

        DoorIndicatorState? capturedIndicatorState  = null;
        ButtonState? capturedButtonState = null;
        safeSideGraph.Door.StateChanged += () =>
        {
            if (safeSideGraph.Door.State != DoorState.Closing)
            {
                return;
            }

            capturedIndicatorState = safeSideGraph.DoorIndicator.State;
            
            var buttonsStates = allButtons.Select(b => b.State).Distinct().ToList();
            capturedButtonState = buttonsStates is [var singleValue] ? singleValue : null;
        };

        _graph.Exit.Enable(safeSideType);
        await _graph.Exit.OpenAsync();

        // Act
        await _graph.Exit.CloseAsync();

        // Assert
        Assert.Equal(DoorIndicatorState.Busy, capturedIndicatorState);
        Assert.Equal(ButtonState.Busy, capturedButtonState);
    }

    [Theory]
    [InlineData(TrainSideType.A)]
    [InlineData(TrainSideType.B)]
    public async Task TrainExit_WhenEnabled_DoorClosedGapFillerRetracted(TrainSideType safeSideType)
    {
        // Arrange
        var (safeSideGraph, otherSideGraph) = GetSideGraphs(_graph, safeSideType);

        // Act
        _graph.Exit.Enable(safeSideType);

        // Assert
        Assert.Equal(DoorState.Closed, safeSideGraph.Door.State);
        Assert.Equal(GapFillerState.Retracted, safeSideGraph.GapFiller.State);
    }

    [Theory]
    [InlineData(TrainSideType.A)]
    [InlineData(TrainSideType.B)]
    public async Task TrainExit_WhenDoorCloses_GapFillerTimerResets(TrainSideType safeSideType)
    {
        // Arrange
        var graph = new TrainExitTestGraphBuilder()
            .WithMockGapFillerA(Substitute.For<ITrainGapFiller>())
            .WithMockGapFillerB(Substitute.For<ITrainGapFiller>())
            .Build();
        var (safeSideGraph, otherSideGraph) = GetSideGraphs(graph, safeSideType);

        graph.Exit.Enable(safeSideType);
        await graph.Exit.OpenAsync();
        safeSideGraph.GapFiller.ClearReceivedCalls();

        // Act
        safeSideGraph.Door.TimeOut();

        // Assert
        safeSideGraph.GapFiller.Received(1).ResetAutoRetractTimer();
    }

    [Theory]
    [InlineData(TrainSideType.A)]
    [InlineData(TrainSideType.B)]
    public async Task TrainExit_WhenDoorOpens_GapFillerTimerStops(TrainSideType safeSideType)
    {
        // Arrange
        var graph = new TrainExitTestGraphBuilder()
            .WithMockGapFillerA(Substitute.For<ITrainGapFiller>())
            .WithMockGapFillerB(Substitute.For<ITrainGapFiller>())
            .Build();
        var (safeSideGraph, otherSideGraph) = GetSideGraphs(graph, safeSideType);

        graph.Exit.Enable(safeSideType);
        await graph.Exit.OpenAsync();
        await safeSideGraph.Door.CloseAsync();
        safeSideGraph.GapFiller.ClearReceivedCalls();

        // Act
        await safeSideGraph.Door.OpenAsync();

        // Assert
        safeSideGraph.GapFiller.Received(1).StopAutoRetractTimer();
    }

    [Theory]
    [InlineData(TrainSideType.A)]
    [InlineData(TrainSideType.B)]
    public async Task TrainExit_WhenExitCloseCalled_DoorClosedGapFillerRetractedButtonsIdle(TrainSideType safeSideType)
    {
        // Arrange
        var (safeSideGraph, otherSideGraph) = GetSideGraphs(_graph, safeSideType);
        var allButtons = safeSideGraph.Buttons.Concat(otherSideGraph.Buttons);
        var buttonsIdle = WaitUntilButtonsIdle(allButtons);
        _graph.Exit.Enable(safeSideType);
        await _graph.Exit.OpenAsync();

        // Act
        await _graph.Exit.CloseAsync();
        await buttonsIdle.WaitAsync(TimeSpan.FromSeconds(BUTTONS_IDLE_TIMEOUT_SECONDS));

        // Assert
        AssertBothSidesClosedAndAllButtonsIdle(safeSideGraph, otherSideGraph);
    }

    [Theory]
    [InlineData(TrainSideType.A)]
    [InlineData(TrainSideType.B)]
    public async Task TrainExit_WhenExitCloseCalledAndButtonPressed_DoesNotReopenDoor(TrainSideType safeSideType)
    {
        // Arrange
        SetupDoorMechanismsDelayedClose();

        _graph.Exit.Enable(safeSideType);
        await _graph.Exit.OpenAsync();

        ClearDoorMechanismsReceivedCalls();

        // Act
        await PressButtonsWhileExitClosing(safeSideType);
        
        // Assert
        await _doorMechanismA.DidNotReceive().OpenAsync();
        await _doorMechanismB.DidNotReceive().OpenAsync();
    }

    [Theory]
    [MemberData(nameof(SafeSideTypeAndButtonIndexCombinations))]
    public async Task TrainExit_WhenDoorTimedOutAndButtonPressed_ReopensSafeSideDoor(TrainSideType safeSideType, int buttonIndex)
    {
        // Arrange
        var (safeSideDoorMechanism, otherSideDoorMechanism) = GetDoorMechanisms(safeSideType);

        SetupDoorMechanismsDelayedClose();

        _graph.Exit.Enable(safeSideType);
        await _graph.Exit.OpenAsync();

        ClearDoorMechanismsReceivedCalls();

        // Act
        await PressButtonsWhileDoorAutoClosing(safeSideType, buttonIndex);

        // Assert
        await safeSideDoorMechanism.Received(1).OpenAsync();
        await otherSideDoorMechanism.DidNotReceive().OpenAsync();
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

    private (TrainExitSideTestGraph, TrainExitSideTestGraph) GetSideGraphs(TrainExitTestGraph graph, TrainSideType safeSideType)
    {
        return safeSideType == TrainSideType.A ?
            (graph.SideA, graph.SideB) : (graph.SideB, graph.SideA);
    }

    private (IExitDoorMechanism, IExitDoorMechanism) GetDoorMechanisms(TrainSideType safeSideType)
    {
        return safeSideType == TrainSideType.A ?
            (_doorMechanismA, _doorMechanismB) : (_doorMechanismB, _doorMechanismA);
    }

    public static IEnumerable<object[]> SafeSideTypeAndButtonIndexCombinations()
    {
        var sideTypes = Enum.GetValues<TrainSideType>();
        var nOfButtons = TrainExitTestGraphBuilder.NOfButtonsPerSide;

        foreach (var sideType in sideTypes)
        {
            foreach (var buttonIndex in Enumerable.Range(0, nOfButtons))
            {
                yield return new object[] { sideType, buttonIndex };
            }
        }
    }

    private void AssertOnlySafeSideOpenedAndAllButtonsIdle(
        TrainExitSideTestGraph safeSideGraph, TrainExitSideTestGraph otherSideGraph)
    {
        Assert.Equal(DoorState.Opened, safeSideGraph.Door.State);
        Assert.Equal(DoorState.Closed, otherSideGraph.Door.State);

        Assert.Equal(GapFillerState.Extended, safeSideGraph.GapFiller.State);
        Assert.Equal(GapFillerState.Retracted, otherSideGraph.GapFiller.State);

        var allButtons = safeSideGraph.Buttons.Concat(otherSideGraph.Buttons);
        AssertButtonsIdle(allButtons);
    }

    private void AssertBothSidesClosedAndAllButtonsIdle(
        TrainExitSideTestGraph safeSideGraph, TrainExitSideTestGraph otherSideGraph)
    {
        Assert.Equal(DoorState.Closed, safeSideGraph.Door.State);
        Assert.Equal(DoorState.Closed, otherSideGraph.Door.State);

        Assert.Equal(GapFillerState.Retracted, safeSideGraph.GapFiller.State);
        Assert.Equal(GapFillerState.Retracted, otherSideGraph.GapFiller.State);

        var allButtons = safeSideGraph.Buttons.Concat(otherSideGraph.Buttons);
        AssertButtonsIdle(allButtons);
    }

    private void AssertButtonsIdle(IEnumerable<IExitButton> buttons)
    {
        foreach (var button in buttons)
        {
            Assert.Equal(ButtonState.Idle, button.State);   
        }
    }

    private void SetupDoorMechanismsDelayedClose()
    {
        _doorMechanismA.CloseAsync(Arg.Any<CancellationToken>()).Returns(async _ => await Task.Delay(200));
        _doorMechanismB.CloseAsync(Arg.Any<CancellationToken>()).Returns(async _ => await Task.Delay(200));
    }

    private void ClearDoorMechanismsReceivedCalls()
    {
        _doorMechanismA.ClearReceivedCalls();
        _doorMechanismB.ClearReceivedCalls();
    }

    private async Task PressButtonsWhileExitClosing(TrainSideType safeSideType)
    {
        var (safeSideGraph, otherSideGraph) = GetSideGraphs(_graph, safeSideType);
        var closeExitTask = _graph.Exit.CloseAsync();
        await Task.Delay(50); // headstart for the closeExitTask

        var allButtons = safeSideGraph.Buttons.Concat(otherSideGraph.Buttons);
        var pressTask = allButtons.Select(button => Task.Run(() => button.Press())).ToArray();
        await Task.WhenAll(pressTask.Append(closeExitTask));
    }

    private async Task PressButtonsWhileDoorAutoClosing(TrainSideType safeSideType, int buttonIndex)
    {
        var (safeSideGraph, otherSideGraph) = GetSideGraphs(_graph, safeSideType);
        safeSideGraph.Door.TimeOut();
        await Task.Delay(50); // headstart for the Door.TimeOut()

        var button = safeSideGraph.Buttons.ElementAt(buttonIndex);
        button.Press();
    }
}
