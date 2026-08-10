public class TimeoutTimerFactory : ITimeoutTimerFactory
{
    public ITimeoutTimer Create(TimeSpan duration, ITimeoutable client)
    {
        return new TimeoutTimer(duration, client);
    }
}