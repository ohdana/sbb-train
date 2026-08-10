public interface ITimeoutTimerFactory
{
    ITimeoutTimer Create(TimeSpan duration, ITimeoutable client);
}