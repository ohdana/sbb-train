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
    [InlineData(TrainSideType.A)]
    [InlineData(TrainSideType.B)]
    public async Task TrainExit_WhenEnabledThenButtonPressedOnSafeSide_GapFillerExtendedAndDoorOpenedAndButtonsIdleOnSafeSide(
        TrainSideType safeSideType)
    {
        // Arrange
        _graph.Exit.Enable(safeSideType);
        var (safeSideGraph, otherSideGraph) = GetSideGraphs(safeSideType);

        // Act
        safeSideGraph.Buttons.First().Press();

        // Assert
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

    /*[Theory]
    [InlineData(TrainSideType.A)]
    [InlineData(TrainSideType.B)]
    public async Task TrainExit_WhenDisabledThenButtonPressed_OpensNoDoor(TrainSideType sideType)
    {
        // Arrange
        _graph.Exit.Disable();
        var buttons = sideType == TrainSideType.A ? _graph.ButtonsA : _graph.ButtonsB;

        // Act
        buttons.First().Press();

        // Assert
        await _doorMechanismA.DidNotReceive().OpenAsync();
        await _doorMechanismB.DidNotReceive().OpenAsync();
        await _gapFillerMechanismA.DidNotReceive().ExtendAsync();
        await _gapFillerMechanismB.DidNotReceive().ExtendAsync();
    }

    [Theory]
    [InlineData(TrainSideType.A)]
    [InlineData(TrainSideType.B)]
    public async Task TrainExit_WhenEnabledThenButtonPressed_CallsGapFillerExtendAndDoorOpenOnSafeSide(TrainSideType safeSideType)
    {
        // Arrange
        _graph.Exit.Enable(safeSideType);
        var (safeSideButtons, otherSideButtons) = GetButtons(safeSideType);
        var (safeSideGapFillerMechanism, otherSideGapFillerMechanism) = GetGapFillerMechanisms(safeSideType);
        var (safeSideDoorMechanism, otherSideDoorMechanism) = GetDoorMechanisms(safeSideType);

        // Act
        safeSideButtons.First().Press();

        // Assert
        Received.InOrder(() =>
        {
            safeSideGapFillerMechanism.Received(1).ExtendAsync();
            safeSideDoorMechanism.Received(1).OpenAsync();
        });

        await otherSideDoorMechanism.DidNotReceive().OpenAsync();
        await otherSideGapFillerMechanism.DidNotReceive().ExtendAsync();
    }

    [Theory]
    [InlineData(TrainSideType.A)]
    [InlineData(TrainSideType.B)]
    public async Task TrainExit_WhenDisabledThenButtonPressedThenEnabled_OpensSafeSideDoor(TrainSideType safeSideType)
    {
        // Arrange
        var (safeSideButtons, otherSideButtons) = GetButtons(safeSideType);
        var (safeSideGapFillerMechanism, otherSideGapFillerMechanism) = GetGapFillerMechanisms(safeSideType);
        var (safeSideDoorMechanism, otherSideDoorMechanism) = GetDoorMechanisms(safeSideType);

        // Act
        _graph.Exit.Disable();
        safeSideButtons.First().Press();
        _graph.Exit.Enable(safeSideType);

        // Assert
        Received.InOrder(() =>
        {
            safeSideGapFillerMechanism.Received(1).ExtendAsync();
            safeSideDoorMechanism.Received(1).OpenAsync();
        });

        await otherSideDoorMechanism.DidNotReceive().OpenAsync();
        await otherSideGapFillerMechanism.DidNotReceive().ExtendAsync();
    }

    private (IEnumerable<IExitButton>, IEnumerable<IExitButton>) GetButtons(TrainSideType safeSideType)
    {
        return safeSideType == TrainSideType.A ? 
            (_graph.ButtonsA, _graph.ButtonsB) : (_graph.ButtonsB, _graph.ButtonsA);
    }

    private (IGapFillerMechanism, IGapFillerMechanism) GetGapFillerMechanisms(TrainSideType safeSideType)
    {
        return safeSideType == TrainSideType.A ?
            (_gapFillerMechanismA, _gapFillerMechanismB) : (_gapFillerMechanismB, _gapFillerMechanismA);
    }

    private (IExitDoorMechanism, IExitDoorMechanism) GetDoorMechanisms(TrainSideType safeSideType)
    {
        return safeSideType == TrainSideType.A ?
            (_doorMechanismA, _doorMechanismB) : (_doorMechanismB, _doorMechanismA);
    }*/

    private (TrainExitSideTestGraph, TrainExitSideTestGraph) GetSideGraphs(TrainSideType safeSideType)
    {
        return safeSideType == TrainSideType.A ?
            (_graph.SideA, _graph.SideB) : (_graph.SideB, _graph.SideA);
    }
}
