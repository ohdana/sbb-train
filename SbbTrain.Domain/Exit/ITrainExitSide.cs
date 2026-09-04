public interface ITrainExitSide : IExitEventHandler
{
    TrainSideType SideType { get; }

    event Action? OpenRequested;
    event Action<EventType>? ButtonNotificationRequested;
    event Action? DoorReopened;
    
    Task OpenAsync();
    Task CloseAsync();
}