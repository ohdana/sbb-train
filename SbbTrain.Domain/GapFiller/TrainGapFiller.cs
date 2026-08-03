public class TrainGapFiller : ITrainGapFiller
{
    public Guid Id { get; }
    public GapFillerState State { get; private set; }

    private readonly ITimeoutTimer _autoRetractTimer;
    private readonly IGapFillerEventHandler _handler;
    private readonly IGapFillerMechanism _mechanism;
    private readonly IGapFillerStateNotifier _notifier;

    public TrainGapFiller(Guid id, 
        ITimeoutTimer autoRetractTimer,
        IGapFillerEventHandler handler,
        IGapFillerMechanism mechanism,
        IGapFillerStateNotifier notifier)
    {
        Id = id;
        State = GapFillerState.Retracted;
        _autoRetractTimer = autoRetractTimer;
        _handler = handler;
        _mechanism = mechanism;
        _notifier = notifier;
    }

    public async Task ExtendAsync()
    {
        if (State == GapFillerState.Extended)
        {
            return;
        }
        
        try
        {
            await _mechanism.ExtendAsync();
            SetState(GapFillerState.Extended);
            _autoRetractTimer.Reset();
        }
        catch (Exception)
        {
            HandleGapFillerMechanismException();
            throw;
        }
    }

    public async Task RetractAsync()
    {
        if (State == GapFillerState.Retracted)
        {
            return;
        }

        try
        {
            await _mechanism.RetractAsync();
            SetState(GapFillerState.Retracted);
            _autoRetractTimer.Stop();
        }
        catch (Exception)
        {
            HandleGapFillerMechanismException();
            throw;
        }
    }

    public void TimeOut()
    {
        _handler.HandleTimeout(Id);
    }

    public void OnEventReceived(EventType eventType)
    {
        HandleEventReceived(eventType);
    }

    private void HandleEventReceived(EventType eventType)
    {
        if (eventType == EventType.ExitOpenRequested) 
        {
            _autoRetractTimer.Reset();
        }
    }

    private void HandleGapFillerMechanismException()
    {
        SetState(GapFillerState.Faulted);
        _notifier.NotifyGapFillerStateChanged(Id, State);
        _autoRetractTimer.Stop();
    }

    private void SetState(GapFillerState state) => State = state;
}