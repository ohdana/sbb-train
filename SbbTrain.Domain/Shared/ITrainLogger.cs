public interface ITrainLogger
{
    void LogError(Guid id, Exception exception);
}