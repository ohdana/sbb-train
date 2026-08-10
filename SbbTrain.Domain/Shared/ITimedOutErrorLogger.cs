public interface ITimedOutErrorLogger
{
    void LogError(Guid id, Exception exception);
}