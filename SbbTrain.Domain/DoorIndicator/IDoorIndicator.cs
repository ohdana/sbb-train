public interface IDoorIndicator : IEventReceiver
{
    Guid Id { get; }
    DoorIndicatorState State { get; }
}