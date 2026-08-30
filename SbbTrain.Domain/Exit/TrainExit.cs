public class TrainExit : ITrainExit
{
    public Guid Id { get; }
    public TrainExitState State { get; private set; }

    public event Action? StateChanged;
    public event Action? OpenRequested;

    private readonly ITrainExitNotifier _notifier;
    private readonly ITrainExitSide _sideA;
    private readonly ITrainExitSide _sideB;
    private ITrainExitSide? _safeSide;

    private bool _isForceClosing;

    public TrainExit(Guid id, ITrainExitSide sideA, ITrainExitSide sideB, ITrainExitNotifier notifier)
    {
        Id = id;
        State = TrainExitState.Disabled;
        _sideA = sideA;
        _sideB = sideB;
        _notifier = notifier;
        _isForceClosing = false;

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
        try
        {
            await _safeSide.CloseAsync();
        }
        catch
        {
            HandleTrainExitSideException();
            throw;
        }
        finally
        {
            _isForceClosing = false;
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

    private void RaiseOpenRequested() => OpenRequested?.Invoke();
    private void RaiseStateChanged() => StateChanged?.Invoke();
}