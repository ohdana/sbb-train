public interface IExit
{
    Guid Id { get; }
    TrainExitState State { get; }

    Task OpenAsync();
    Task CloseAsync();
}