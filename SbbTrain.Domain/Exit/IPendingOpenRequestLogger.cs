public interface IPendingOpenRequestLogger
{
    void LogResolveFailure(Guid exitId, Exception exception);
}