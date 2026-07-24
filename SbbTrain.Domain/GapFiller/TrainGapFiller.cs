public class TrainGapFiller : ITrainGapFiller
{
    private readonly IGapFillerTimer _autoRetractTimer;
    private readonly IGapFillerEventHandler _handler;
    
    public Guid Id { get; }
    public GapFillerState State { get; private set; }

    public TrainGapFiller(Guid id, IGapFillerTimer timer, IGapFillerEventHandler handler)
    {
        Id = id;
        State = GapFillerState.Retracted;
        _autoRetractTimer = timer;
        _handler = handler;
    }

    public void Extend()
    {
        SetState(GapFillerState.Extended);
        _autoRetractTimer.Reset();
    }

    public void Retract()
    {
        SetState(GapFillerState.Retracted);
        _autoRetractTimer.Stop();
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

    private void SetState(GapFillerState state) => State = state;
}