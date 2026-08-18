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
    [MemberData(nameof(SafeSideTypeAndButtonIndexCombinations))]
    public async Task TrainExit_WhenEnabledThenButtonPressedOnSafeSide_GapFillerExtendedAndDoorOpenedAndButtonsIdleOnSafeSide(
        TrainSideType safeSideType, int buttonIndex)
    {
        // Arrange
        _graph.Exit.Enable(safeSideType);
        var (safeSideGraph, otherSideGraph) = GetSideGraphs(safeSideType);

        // Act
        safeSideGraph.Buttons.ElementAt(buttonIndex).Press();

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

    private (TrainExitSideTestGraph, TrainExitSideTestGraph) GetSideGraphs(TrainSideType safeSideType)
    {
        return safeSideType == TrainSideType.A ?
            (_graph.SideA, _graph.SideB) : (_graph.SideB, _graph.SideA);
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
}
