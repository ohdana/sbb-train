public interface IExitDoorMechanism
{
    Task OpenAsync();
    Task CloseAsync(CancellationToken token);
}