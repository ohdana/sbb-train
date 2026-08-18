using NSubstitute;

public record TrainExitSideTestGraph(
    ITrainExitSide Side,
    IExitDoor Door,
    IDoorIndicator DoorIndicator,
    ITrainGapFiller GapFiller,
    IReadOnlyList<IExitButton> Buttons
);

public record TrainExitTestGraph(
    ITrainExit Exit,
    TrainExitSideTestGraph SideA,
    TrainExitSideTestGraph SideB);

public static class TestCompositionRoot
{
    private static readonly TimeSpan AutoCloseTimerDuration = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan AutoRetractTimerDuration = TimeSpan.FromSeconds(300);
    private const int NOfButtons = 5;

    public static TrainExitTestGraph CreateTrainExit(
        IExitDoorMechanism doorMechanismA,
        IExitDoorMechanism doorMechanismB,
        IGapFillerMechanism gapFillerMechanismA,
        IGapFillerMechanism gapFillerMechanismB,
        ITrainNotifier notifier,
        ITrainLogger logger)
    {
        var timerFactory = new TimeoutTimerFactory();

        var graphSideA = CreateSide(
            TrainSideType.A, doorMechanismA, gapFillerMechanismA,
            notifier, logger, timerFactory);
        var graphSideB = CreateSide(
            TrainSideType.B, doorMechanismB, gapFillerMechanismB,
            notifier, logger, timerFactory);

        var exit = new TrainExit(Guid.NewGuid(), graphSideA.Side, graphSideB.Side, notifier);
        _ = new PendingOpenRequestResolver(exit, logger);

        return new TrainExitTestGraph(exit, graphSideA, graphSideB);
    }

    private static TrainExitSideTestGraph CreateSide(
        TrainSideType sideType, IExitDoorMechanism doorMechanism, IGapFillerMechanism gapFillerMechanism,
        ITrainNotifier notifier, ITrainLogger logger, ITimeoutTimerFactory timerFactory)
    {
        var door = CreateDoor(doorMechanism, notifier, timerFactory);
        var doorIndicator = CreateDoorIndicator(notifier);
        var gapFiller = CreateGapFiller(gapFillerMechanism, notifier, timerFactory);
        var buttons = CreateButtons(notifier);

        var side = new TrainExitSide(sideType, door, doorIndicator, gapFiller, buttons, logger);
        var sideGraph = new TrainExitSideTestGraph(
            side, door, doorIndicator, gapFiller, buttons);

        return sideGraph;
    }

    private static IExitDoor CreateDoor(IExitDoorMechanism mechanism,
        ITrainNotifier notifier, ITimeoutTimerFactory timerFactory)
    {
        return new ExitDoor(Guid.NewGuid(), timerFactory,
            AutoCloseTimerDuration, mechanism, notifier);
    }

    private static IDoorIndicator CreateDoorIndicator(ITrainNotifier notifier)
    {
        return new DoorIndicator(Guid.NewGuid(), notifier);
    }

    private static ITrainGapFiller CreateGapFiller(IGapFillerMechanism mechanism,
        ITrainNotifier notifier, ITimeoutTimerFactory timerFactory)
    {
        return new TrainGapFiller(Guid.NewGuid(),
            timerFactory, AutoRetractTimerDuration, mechanism, notifier);
    }

    private static IReadOnlyList<IExitButton> CreateButtons(ITrainNotifier notifier)
    {
        return Enumerable.Range(0, NOfButtons)
                         .Select(i => CreateButton(notifier))
                         .ToList();
    }

    private static IExitButton CreateButton(ITrainNotifier notifier)
    {
        return new ExitButton(Guid.NewGuid(), notifier);
    }
}