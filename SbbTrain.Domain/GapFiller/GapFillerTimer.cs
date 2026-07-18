public class GapFillerTimer : ITimer
{
    private readonly TimeSpan _duration;
    private readonly ITimeoutable _client;
    private CancellationTokenSource? _cts;

    public GapFillerTimer(TimeSpan duration, ITimeoutable client)
    {
        _duration = duration;
        _client = client;
    }

    public void Reset()
    {
        Stop();
        _cts = new CancellationTokenSource();
        ScheduleTimeout();
    }

    public void Stop()
    {
        if (_cts == null) return;
        _cts.Cancel();
        _cts.Dispose();
        _cts = null;
    }

    public void Dispose() => Stop();

    private void ScheduleTimeout()
    {
        Task.Delay(_duration, _cts!.Token).ContinueWith(task =>
        {
            if (!task.IsCanceled)
            {
                _client.TimeOut();
            }
        }, TaskScheduler.Default);
    }
}