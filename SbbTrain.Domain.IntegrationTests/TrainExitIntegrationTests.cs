using Xunit;
using NSubstitute;

public class TrainExitIntegrationTests
{
    private readonly IExitDoorMechanism _doorMechanismA;
    private readonly IExitDoorMechanism _doorMechanismB;
    private readonly IGapFillerMechanism _gapFillerMechanismA;
    private readonly IGapFillerMechanism _gapFillerMechanismB;
    private readonly TrainExitTestGraph _graph;

    public TrainExitIntegrationTests()
    {
        _doorMechanismA = Substitute.For<IExitDoorMechanism>();
        _doorMechanismB = Substitute.For<IExitDoorMechanism>();
        _gapFillerMechanismA = Substitute.For<IGapFillerMechanism>();
        _gapFillerMechanismB = Substitute.For<IGapFillerMechanism>();

        _graph = TestCompositionRoot.CreateTrainExit(
            _doorMechanismA, _doorMechanismB,
            _gapFillerMechanismA, _gapFillerMechanismB);
    }

    [Theory]
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
    public async Task TrainExit_WhenEnabledThenButtonPressed_OpensSafeSideDoor(TrainSideType safeSideType)
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
    }
}
