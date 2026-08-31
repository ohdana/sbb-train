public class ButtonStateMap
{
    private static IReadOnlyDictionary<EventType, ButtonState> Map { get; } =
        new Dictionary<EventType, ButtonState>()
    {
        { EventType.ExitOpenRequested, ButtonState.Active },
        { EventType.DoorBusy, ButtonState.Busy },
        { EventType.DoorIdle, ButtonState.Idle },
        { EventType.ExitForceClosing, ButtonState.Disabled },
        { EventType.ExitForceClosed, ButtonState.Idle }
    };

    public static ButtonState GetStateByEvent(EventType eventType) 
        => EventStateMap.GetStateByEvent(Map, eventType);
}