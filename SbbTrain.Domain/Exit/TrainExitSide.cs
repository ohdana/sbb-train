public class TrainExitSide : ITrainExitSide
{
    public TrainSideType SideType { get; }

    public event Action<EventType>? OpenRequested;

    private readonly IExitDoor _door;
    private readonly IDoorIndicator _doorIndicator;
    private readonly ITrainGapFiller _gapFiller;
    private readonly IEnumerable<IExitButton> _buttons;

    public TrainExitSide(TrainSideType sideType,
        IExitDoor door,
        IDoorIndicator doorIndicator,
        ITrainGapFiller gapFiller,
        IEnumerable<IExitButton> buttons)
    {
        SideType = sideType;
        _door = door;
        _doorIndicator = doorIndicator;
        _gapFiller = gapFiller;
        _buttons = buttons;

        _door.StateChanged += OnDoorStateChanged;

        foreach (var button in _buttons)
        {
            OpenRequested += button.OnEventReceived;   
        }
    }

    public async Task OpenAsync()
    {
        await _gapFiller.ExtendAsync();
        await _door.OpenAsync();
    }
    
    public async Task CloseAsync()
    {
        await _door.CloseAsync();
        await _gapFiller.RetractAsync();
    }

    public void HandlePendingOpenRequest()
    {
        RaiseOpenRequested();
    }

    private void RaiseOpenRequested() => OpenRequested?.Invoke(EventType.ExitOpenRequested);

    private void OnDoorStateChanged()
    {
        switch (_door.State)
        {
            case DoorState.Opening:
            case DoorState.Closing:
                HandleDoorBusy();
                break;
            case DoorState.Opened:
            case DoorState.Closed:
                HandleDoorIdle();
                break;
            default:
                break;
        }
    }

    private void HandleDoorBusy() => _doorIndicator.OnEventReceived(EventType.DoorBusy);
    private void HandleDoorIdle() => _doorIndicator.OnEventReceived(EventType.DoorIdle);
}