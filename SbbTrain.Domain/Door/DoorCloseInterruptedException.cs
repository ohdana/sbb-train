public class DoorCloseInterruptedException : Exception
{
    public DoorCloseInterruptedException(Guid doorId)
        : base($"Door {doorId} close was interrupted.") {}
}