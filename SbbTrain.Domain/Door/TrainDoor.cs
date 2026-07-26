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
        if (State == DoorState.Opening || State == DoorState.Opened)
        {
            return;
        }

        TransitionTo(DoorState.Opening);
        try
        {
            await _mechanism.OpenAsync();
            TransitionTo(DoorState.Opened);
            _autoCloseTimer.Reset();
        }
        catch (Exception)
        {
            TransitionTo(DoorState.Faulted);
            throw;
        }
    }

    public async Task CloseAsync()
    {
        if (State == DoorState.Closing || State == DoorState.Closed)
        {
            return;
        }

        _cts = new CancellationTokenSource();
        TransitionTo(DoorState.Closing);

        try
        {
            await _mechanism.CloseAsync(_cts.Token);
        }
        catch (OperationCanceledException)
        {
            await OpenAsync();
            return;
        }
        catch (Exception)
        {
            TransitionTo(DoorState.Faulted);
            throw;
        }
        finally
        {
            DisposeCancellationTokenSource();
        }

        TransitionTo(DoorState.Closed);
        _autoCloseTimer.Stop();
    }

    public void TimeOut()
    {
        _handler.HandleTimeout(Id);
    }

    public void Dispose()
    {
        DisposeCancellationTokenSource();
        _autoCloseTimer.Dispose();
    }

    public void OnEventReceived(EventType eventType)
    {
        HandleEventReceived(eventType);
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

    private void DisposeCancellationTokenSource()
    {
        if (_cts == null) return;
        _cts.Cancel();
        _cts.Dispose();
        _cts = null;
    }

    private void TransitionTo(DoorState state)
    {
        SetState(state);
        _notifier.NotifyDoorStateChanged(Id, State);
    }

    private void SetState(DoorState state) => State = state;
}