public class ExitDoor : IExitDoor
{
    public Guid Id { get; }
    public DoorState State { get; private set; }

    public event Action? StateChanged;
    public event Action? TimedOut;

    private readonly ITimeoutTimer _autoCloseTimer;
    private readonly IExitDoorMechanism _mechanism;
    private readonly IDoorStateNotifier _notifier;
    private CancellationTokenSource? _cts;

    public ExitDoor(Guid id,
        ITimeoutTimerFactory timerFactory,
        TimeSpan autoCloseTimerDuration,
        IExitDoorMechanism mechanism,
        IDoorStateNotifier notifier)
    {
        Id = id;
        State = DoorState.Closed;
        _autoCloseTimer = timerFactory.Create(autoCloseTimerDuration, this);
        _mechanism = mechanism;
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
            throw new DoorCloseInterruptedException(Id);
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
        RaiseTimedOut();
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
        if (State == DoorState.Closed)
        {
            return;
        }

        if (State == DoorState.Closing)
        {
            StopDoorMechanism();
        }
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

    private void SetState(DoorState state)
    {
        State = state;
        RaiseStateChanged();
    }

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

    private void RaiseStateChanged() => StateChanged?.Invoke();
    private void RaiseTimedOut() => TimedOut?.Invoke();
}