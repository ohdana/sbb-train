public interface ITrainExitSide : IExitEventHandler
{
    TrainSideType SideType { get; }

    event Action? OpenRequested;
    event Action<EventType>? ButtonNotificationRequested;
    event Action? ObstructionDetected;
    
    Task OpenAsync();
    Task CloseAsync();
}