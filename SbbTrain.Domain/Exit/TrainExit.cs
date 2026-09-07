public class TrainExit : ITrainExit
{
    public Guid Id { get; }
    public TrainExitState State { get; private set; }

    public event Action? StateChanged;
    public event Action? OpenRequested;

    private readonly ITrainLogger _logger;
    private readonly ITrainExitNotifier _notifier;
    private readonly ITrainExitSide _sideA;
    private readonly ITrainExitSide _sideB;
    private ITrainExitSide? _safeSide;

    private bool _isForceClosing;
    private int _autoCloseRetryCount;
    private const int AUTO_CLOSE_RETRY_COUNT_THRESHOLD = 1;

    public TrainExit(Guid id, ITrainExitSide sideA, ITrainExitSide sideB, ITrainExitNotifier notifier, ITrainLogger logger)
    {
        Id = id;
        State = TrainExitState.Disabled;
        _sideA = sideA;
        _sideB = sideB;
        _notifier = notifier;
        _logger = logger;
        _isForceClosing = false;
        _autoCloseRetryCount = 0;

        _sideA.OpenRequested += OnOpenRequested;
        _sideB.OpenRequested += OnOpenRequested;

        _sideA.ButtonNotificationRequested += OnButtonNotificationRequested;
        _sideB.ButtonNotificationRequested += OnButtonNotificationRequested;
    }

    public void Enable(TrainSideType sideType)
    {
        SetSafeSide(sideType);
        SetState(TrainExitState.Enabled);
    }

    public void Disable()
    {
        SetNoSafeSide();
        SetState(TrainExitState.Disabled);
    }

    public async Task OpenAsync()
    {
        if (_safeSide == null)
        {
            return;
        }

        try
        {
            await _safeSide.OpenAsync();
        }
        catch
        {
            HandleTrainExitSideException();
            throw;
        }
    }

    public async Task CloseAsync()
    {
        if (_safeSide == null)
        {
            return;
        }

        _isForceClosing = true;
        ResetAutoCloseRetryCount();

        try
        {
            OnButtonNotificationRequested(EventType.ExitForceClosing);
            await AttemptCloseWithRetry();
        }
        catch
        {
            HandleTrainExitSideException();
            throw;
        }
        finally
        {
            _isForceClosing = false;
            OnButtonNotificationRequested(EventType.ExitForceClosed);
        }
    }

    private void HandleTrainExitSideException()
    {
        SetNoSafeSide();
        SetState(TrainExitState.Faulted);
        _notifier.NotifyTrainExitStateChanged(Id, State);
    }

    private void OnOpenRequested()
    {
        if (_isForceClosing)
        {
            return;
        }

        RaiseOpenRequested();
    }

    private void OnButtonNotificationRequested(EventType eventType)
    {
        _sideA.HandleButtonNotificationRequest(eventType);
        _sideB.HandleButtonNotificationRequest(eventType);
    }

    private void SetNoSafeSide() => _safeSide = null;

    private void SetSafeSide(TrainSideType sideType)
    {
        _safeSide = sideType switch
        {
            TrainSideType.A => _sideA,
            TrainSideType.B => _sideB,
            _ => throw new ArgumentOutOfRangeException(nameof(sideType), $"Unknown side type: {sideType}")
        };
    }
    
    private void SetState(TrainExitState state)
    {
        State = state;
        RaiseStateChanged();
    }

    private async Task AttemptCloseWithRetry()
    {
        try
        {
            await _safeSide!.CloseAsync();
        }
        catch (DoorCloseInterruptedException exception)
        {
            _logger.LogError(Id, exception);

            if (_autoCloseRetryCount >= AUTO_CLOSE_RETRY_COUNT_THRESHOLD)
            {
                _notifier.NotifyAutoCloseRetryCountQuotaExceeded(Id);
                return;
            }

            IncrementAutoCloseRetryCount();
            await AttemptCloseWithRetry();
        }
    }
    
    private void IncrementAutoCloseRetryCount() => _autoCloseRetryCount++;
    private void ResetAutoCloseRetryCount() => _autoCloseRetryCount = 0;

    private void RaiseOpenRequested() => OpenRequested?.Invoke();
    private void RaiseStateChanged() => StateChanged?.Invoke();
}