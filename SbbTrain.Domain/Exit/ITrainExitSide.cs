public interface ITrainExitSide : IExitEventHandler
{
    Task OpenAsync();
    Task CloseAsync();
}