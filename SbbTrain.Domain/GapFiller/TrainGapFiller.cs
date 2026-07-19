public class TrainGapFiller : ITrainGapFiller
{
    public Guid Id { get; }
    public GapFillerState State { get; private set; }
    private readonly ITimer _timer;
    private readonly IGapFillerEventHandler _handler;

    public TrainGapFiller(Guid id, ITimer timer, IGapFillerEventHandler handler)
    {
        Id = id;
        State = GapFillerState.Retracted;
        _timer = timer;
        _handler = handler;
    }

    public void Extend()
    {
        SetState(GapFillerState.Extended);
        _timer.Reset();
    }

    public void Retract()
    {
        SetState(GapFillerState.Retracted);
        _timer.Stop();
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
            _timer.Reset();
        }
    }

    private void SetState(GapFillerState state) => State = state;
}