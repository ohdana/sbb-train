public class ButtonStateMap
{
    private static IReadOnlyDictionary<EventType, ButtonState> Map { get; } =
        new Dictionary<EventType, ButtonState>()
    {
        { EventType.ExitOpenRequested, ButtonState.Active },
        { EventType.DoorBusy, ButtonState.Busy },
        { EventType.DoorIdle, ButtonState.Idle },
        { EventType.DoorDisabled, ButtonState.Disabled }
    };

    public static ButtonState GetStateByEvent(EventType eventType) 
        => EventStateMap.GetStateByEvent(Map, eventType);
}