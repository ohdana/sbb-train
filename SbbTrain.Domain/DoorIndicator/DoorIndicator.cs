public class DoorIndicator : IDoorIndicator
{
    public Guid Id { get; }
    public DoorIndicatorState State { get; private set; }
    
    public IDoorIndicatorStateNotifier _notifier;

    public DoorIndicator(Guid id, IDoorIndicatorStateNotifier notifier)
    {
        Id = id;
        State = DoorIndicatorState.Idle;
        _notifier = notifier;
    }
    
    public void OnEventReceived(EventType eventType)
    {
        HandleEventReceived(eventType);
    }

    private void HandleEventReceived(EventType eventType)
    {
        var newState = GetNewState(eventType);
        SetState(newState);
        NotifyStateChanged();
    }

    private void NotifyStateChanged()
    {
        _notifier.NotifyDoorIndicatorStateChanged(Id, State);
    }

    private DoorIndicatorState GetNewState(EventType eventType)
    {   
        return DoorIndicatorStateMap.GetStateByEvent(eventType);
    }

    private void SetState(DoorIndicatorState state) => State = state;
}