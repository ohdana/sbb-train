public interface ITrainDoorMechanism
{
    Task OpenAsync();
    Task CloseAsync(CancellationToken token);
}