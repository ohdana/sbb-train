public class TrainDoor : ITrainDoor
{
    private readonly ITimeoutTimer _autoCloseTimer;
    private readonly IDoorStateNotifier _notifier;
    private readonly IDoorEventHandler _handler;
    private readonly ITrainDoorMechanism _mechanism;
    private CancellationTokenSource? _cts;

    public Guid Id { get; }
    public DoorState State { get; private set; }

    public TrainDoor(Guid id,
        ITimeoutTimer timer,
        ITrainDoorMechanism mechanism,
        IDoorEventHandler handler,
        IDoorStateNotifier notifier)
    {
        Id = id;
        State = DoorState.Closed;
        _autoCloseTimer = timer;
        _mechanism = mechanism;
        _handler = handler;
        _notifier = notifier;
    }

    public async Task OpenAsync()
    {
        SetState(DoorState.Opening);
        _notifier.NotifyDoorStateChanged(Id, State);
        try
        {
            await _mechanism.OpenAsync();
            SetState(DoorState.Opened);
            _notifier.NotifyDoorStateChanged(Id, State);
            _autoCloseTimer.Reset();
        }
        catch (OperationCanceledException)
        {
            // TODO
            return;
        }
    }

    public async Task CloseAsync()
    {
        _cts = new CancellationTokenSource();

        SetState(DoorState.Closing);
        _notifier.NotifyDoorStateChanged(Id, State);

        try
        {
            await _mechanism.CloseAsync(_cts.Token);
        }
        catch (OperationCanceledException)
        {
            await OpenAsync();
            return;
        }
        finally
        {
            DisposeCancellationTokenSource();
        }

        SetState(DoorState.Closed);
        _notifier.NotifyDoorStateChanged(Id, State);
        _autoCloseTimer.Stop();
    }

    public void TimeOut()
    {
        _handler.HandleTimeout(Id);
    }

    public void OnEventReceived(EventType eventType)
    {
        HandleEventReceived(eventType);
    }

    public void Dispose()
    {
        DisposeCancellationTokenSource();
    }

    private void SetState(DoorState state) => State = state;

    private void DisposeCancellationTokenSource()
    {
        if (_cts == null) return;
        _cts.Cancel();
        _cts.Dispose();
        _cts = null;
    }

    private void HandleEventReceived(EventType eventType)
    {
        if (eventType == EventType.ObstructionDetected)
        {
            HandleObstructionDetected();
        }
    }

    private void HandleObstructionDetected()
    {
        if (State == DoorState.Closing)
        {
            _cts?.Cancel();
        }

        _autoCloseTimer.Reset();
    }
}