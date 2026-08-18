public interface ITrainExitSide : IExitEventHandler
{
    TrainSideType SideType { get; }

    event Action? OpenRequested;
    event Action<EventType>? ButtonNotificationRequested;
    
    Task OpenAsync();
    Task CloseAsync();
}