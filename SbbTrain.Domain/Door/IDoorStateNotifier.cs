public interface IDoorStateNotifier
{
    void NotifyDoorStateChanged(Guid doorId, DoorState state);
}