public interface ITrainExitSide : IExitEventHandler
{
    TrainSideType SideType { get; }
    
    Task OpenAsync();
    Task CloseAsync();
}