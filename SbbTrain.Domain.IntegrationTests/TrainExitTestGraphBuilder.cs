using NSubstitute;

public class TrainExitTestGraphBuilder
{
    public const int NOfButtonsPerSide = 3;
    private static readonly TimeSpan AutoCloseTimerDuration = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan AutoRetractTimerDuration = TimeSpan.FromSeconds(300);

    private IExitDoorMechanism _doorMechanismA = Substitute.For<IExitDoorMechanism>();
    private IExitDoorMechanism _doorMechanismB = Substitute.For<IExitDoorMechanism>();
    private IGapFillerMechanism _gapFillerMechanismA = Substitute.For<IGapFillerMechanism>();
    private IGapFillerMechanism _gapFillerMechanismB = Substitute.For<IGapFillerMechanism>();
    private ITrainNotifier _notifier = Substitute.For<ITrainNotifier>();
    private ITrainLogger _logger = Substitute.For<ITrainLogger>();

    private ITrainGapFiller? _mockGapFillerA;
    private ITrainGapFiller? _mockGapFillerB;

    public TrainExitTestGraph Build()
    {
        var timerFactory = new TimeoutTimerFactory();

        var sideA = BuildSide(TrainSideType.A, _doorMechanismA, _gapFillerMechanismA, _mockGapFillerA, timerFactory);
        var sideB = BuildSide(TrainSideType.B, _doorMechanismB, _gapFillerMechanismB, _mockGapFillerB, timerFactory);

        var exit = new TrainExit(Guid.NewGuid(), sideA.Side, sideB.Side, _notifier);
        _ = new PendingOpenRequestResolver(exit, _logger);

        return new TrainExitTestGraph(
            exit,
            new TrainExitSideTestGraph(sideA.Side, sideA.Door, sideA.DoorIndicator, sideA.GapFiller, sideA.Buttons),
            new TrainExitSideTestGraph(sideB.Side, sideB.Door, sideB.DoorIndicator, sideB.GapFiller, sideB.Buttons)
        );
    }

    public TrainExitTestGraphBuilder WithMockGapFillerA(ITrainGapFiller gapFiller) { _mockGapFillerA = gapFiller; return this; }
    public TrainExitTestGraphBuilder WithMockGapFillerB(ITrainGapFiller gapFiller){ _mockGapFillerB = gapFiller; return this; }

    public TrainExitTestGraphBuilder WithDoorMechanismA(IExitDoorMechanism mechanism){ _doorMechanismA = mechanism; return this; }
    public TrainExitTestGraphBuilder WithDoorMechanismB(IExitDoorMechanism mechanism){ _doorMechanismB = mechanism; return this; }

    public TrainExitTestGraphBuilder WithGapFillerMechanismA(IGapFillerMechanism mechanism){ _gapFillerMechanismA = mechanism; return this; }
    public TrainExitTestGraphBuilder WithGapFillerMechanismB(IGapFillerMechanism mechanism){ _gapFillerMechanismB = mechanism; return this; }

    public TrainExitTestGraphBuilder WithNotifier(ITrainNotifier notifier) { _notifier = notifier; return this; }
    
    public TrainExitTestGraphBuilder WithLogger(ITrainLogger logger) { _logger = logger; return this; }
    
    private (ITrainExitSide Side, IExitDoor Door, IDoorIndicator DoorIndicator, ITrainGapFiller GapFiller, IReadOnlyList<IExitButton> Buttons) BuildSide(
            TrainSideType sideType,
            IExitDoorMechanism doorMechanism,
            IGapFillerMechanism gapFillerMechanism,
            ITrainGapFiller? mockGapFiller,
            ITimeoutTimerFactory timerFactory)
    {
        var door = new ExitDoor(Guid.NewGuid(), timerFactory, AutoCloseTimerDuration, doorMechanism, _notifier);
        var doorIndicator = new DoorIndicator(Guid.NewGuid(), _notifier);
        var gapFiller = mockGapFiller 
            ?? new TrainGapFiller(Guid.NewGuid(), timerFactory, AutoRetractTimerDuration, gapFillerMechanism, _notifier);
        var buttons = CreateButtons();

        var side = new TrainExitSide(sideType, door, doorIndicator, gapFiller, buttons, _logger);
        
        return (side, door, doorIndicator, gapFiller, buttons);
    }

    private IReadOnlyList<IExitButton> CreateButtons()
    {
        return Enumerable.Range(0, NOfButtonsPerSide)
                         .Select(i => new ExitButton(Guid.NewGuid(), _notifier))
                         .ToList();
    }
}