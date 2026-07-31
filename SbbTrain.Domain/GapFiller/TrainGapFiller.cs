public class TrainGapFiller : ITrainGapFiller
{
    private readonly ITimeoutTimer _autoRetractTimer;
    private readonly IGapFillerStateNotifier _notifier;
    private readonly IGapFillerMechanism _mechanism;
    private readonly IGapFillerEventHandler _handler;
    
    public Guid Id { get; }
    public GapFillerState State { get; private set; }

    public TrainGapFiller(Guid id, 
        ITimeoutTimer timer, 
        IGapFillerStateNotifier notifier,
        IGapFillerMechanism mechanism,
        IGapFillerEventHandler handler)
    {
        Id = id;
        State = GapFillerState.Retracted;
        _autoRetractTimer = timer;
        _notifier = notifier;
        _mechanism = mechanism;
        _handler = handler;
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