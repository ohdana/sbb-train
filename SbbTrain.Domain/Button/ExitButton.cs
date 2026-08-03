public class ExitButton : IExitButton
{
    public Guid Id { get; }
    public ButtonState State { get; private set; }

    private readonly IButtonEventHandler _handler;
    private readonly IButtonStateNotifier _notifier;
    
    public ExitButton(Guid id, 
        IButtonEventHandler handler,
        IButtonStateNotifier notifier)
    {
        Id = id;
        State = ButtonState.Idle;
        _handler = handler;
        _notifier = notifier;
    }

    public void Press()
    {
        _handler.HandleButtonPressed(Id);
    }

    public void OnEventReceived(EventType eventType)
    {
        HandleEventReceived(eventType);
    }

    private void HandleEventReceived(EventType eventType)
    {
        var newState = GetNewState(eventType);
        SetState(newState);
        _notifier.NotifyButtonStateChanged(Id, State);
    }

    private ButtonState GetNewState(EventType eventType)
    {   
        return ButtonStateMap.GetStateByEvent(eventType);
    }

    private void SetState(ButtonState state)
    {
        State = state;
    }
}