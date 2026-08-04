public interface IExit
{
    Guid Id { get; }
    TrainExitState State { get; }

    Task RequestOpenAsync();
    Task CloseAsync();
}