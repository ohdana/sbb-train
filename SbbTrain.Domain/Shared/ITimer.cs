public interface ITimer : IDisposable
{
    void Reset();
    void Stop();
}