public class ExitDoor : IExitDoor
{
    private readonly ITimeoutTimer _autoCloseTimer;
    private readonly IDoorStateNotifier _notifier;
    private readonly IDoorEventHandler _handler;
    private readonly IExitDoorMechanism _mechanism;
    private CancellationTokenSource? _cts;

    public Guid Id { get; }
    public DoorState State { get; private set; }

    public ExitDoor(Guid id,
        ITimeoutTimer timer,
        IExitDoorMechanism mechanism,
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
            HandleDoorMechanismException();
            throw;
        }
    }

    public async Task CloseAsync()
    {
        if (State == DoorState.Closing || State == DoorState.Closed)
        {
            return;
        }

        RefreshCancellationTokenSource();
        TransitionTo(DoorState.Closing);

        try
        {
            await _mechanism.CloseAsync(_cts!.Token);
        }
        catch (OperationCanceledException)
        {
            await OpenAsync();
            return;
        }
        catch (Exception)
        {
            HandleDoorMechanismException();
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
            StopDoorMechanism();
        }

        _autoCloseTimer.Reset();
    }

    private void StopDoorMechanism()
    {
        _cts?.Cancel();
    }

    private void HandleDoorMechanismException()
    {
        TransitionTo(DoorState.Faulted);
        _autoCloseTimer.Stop();
    }

    private void TransitionTo(DoorState state)
    {
        SetState(state);
        _notifier.NotifyDoorStateChanged(Id, State);
    }

    private void SetState(DoorState state) => State = state;

    private void RefreshCancellationTokenSource()
    {
        _cts = new CancellationTokenSource();
    }

    private void DisposeCancellationTokenSource()
    {
        if (_cts == null) return;
        _cts.Cancel();
        _cts.Dispose();
        _cts = null;
    }
}