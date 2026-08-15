using NSubstitute;

public record TrainExitTestGraph(
    ITrainExit Exit,
    IReadOnlyList<IExitButton> ButtonsA,
    IReadOnlyList<IExitButton> ButtonsB);

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

        var (sideA, buttonsA) = CreateSide(
            TrainSideType.A, doorMechanismA, gapFillerMechanismA,
            notifier, logger, timerFactory);
        var (sideB, buttonsB) = CreateSide(
            TrainSideType.B, doorMechanismB, gapFillerMechanismB,
            notifier, logger, timerFactory);

        var exit = new TrainExit(Guid.NewGuid(), sideA, sideB, notifier);
        _ = new PendingOpenRequestResolver(exit, logger);

        return new TrainExitTestGraph(exit, buttonsA, buttonsB);
    }

    private static (ITrainExitSide side, IReadOnlyList<IExitButton> buttons) CreateSide(
        TrainSideType sideType, IExitDoorMechanism doorMechanism, IGapFillerMechanism gapFillerMechanism,
        ITrainNotifier notifier, ITrainLogger logger, ITimeoutTimerFactory timerFactory)
    {
        var door = CreateDoor(doorMechanism, notifier, timerFactory);
        var doorIndicator = CreateDoorIndicator(notifier);
        var gapFiller = CreateGapFiller(gapFillerMechanism, notifier, timerFactory);
        var buttons = CreateButtons(notifier);

        var side = new TrainExitSide(sideType, door, doorIndicator, gapFiller, buttons, logger);

        return (side, buttons);
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