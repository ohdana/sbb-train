public interface IDoorIndicatorStateNotifier
{
    void NotifyDoorIndicatorStateChanged(Guid doorIndicatorId, DoorIndicatorState state);
}