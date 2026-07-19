public static class EventStateMap
{
    public static TState GetStateByEvent<TState>(
        IReadOnlyDictionary<EventType, TState> map, 
        EventType eventType)
    {
        if (!map.ContainsKey(eventType))
        {
            throw new ArgumentOutOfRangeException(
                nameof(eventType),
                $"Unknown event type: {eventType}");
        }
        
        return map[eventType];
    }
}