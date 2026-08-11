public interface ITrainExitSide : IExitEventHandler
{
    TrainSideType SideType { get; }

    event Action? OpenRequested;
    
    Task OpenAsync();
    Task CloseAsync();
}