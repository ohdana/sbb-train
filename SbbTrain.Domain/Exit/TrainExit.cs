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

    public TrainExit(Guid id, ITrainExitSide sideA, ITrainExitSide sideB, ITrainExitNotifier notifier)
    {
        Id = id;
        State = TrainExitState.Disabled;
        _sideA = sideA;
        _sideB = sideB;
        _notifier = notifier;
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

    public void RequestOpen()
    {
        RaiseOpenRequested();
        NotifyOpenRequested();
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

        try
        {
            await _safeSide.CloseAsync();
        }
        catch
        {
            HandleTrainExitSideException();
            throw;
        }
    }

    private void HandleTrainExitSideException()
    {
        SetNoSafeSide();
        SetState(TrainExitState.Faulted);
        _notifier.NotifyTrainExitStateChanged(Id, State);
    }
    
    private void NotifyOpenRequested()
    {
        _notifier.NotifyTrainExitOpenRequested(Id);
        _sideA.HandlePendingOpenRequest();
        _sideB.HandlePendingOpenRequest();
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