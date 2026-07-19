public class DoorIndicatorStateMap
{
    private static IReadOnlyDictionary<EventType, DoorIndicatorState> Map { get; } =
        new Dictionary<EventType, DoorIndicatorState>()
    {
        { EventType.DoorBusy, DoorIndicatorState.Busy },
        { EventType.DoorIdle, DoorIndicatorState.Idle }
    };

    public static DoorIndicatorState GetStateByEvent(EventType eventType) 
        => EventStateMap.GetStateByEvent(Map, eventType);
}