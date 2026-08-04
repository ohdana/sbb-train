public class TrainExit : ITrainExit
{
    public Guid Id { get; }
    public TrainExitState State { get; private set; }

    private readonly ITrainExitStateNotifier _notifier;
    private readonly ITrainExitSide _sideA;
    private readonly ITrainExitSide _sideB;
    private ITrainExitSide? _safeSide;
    private bool _isOpenPending;

    public TrainExit(Guid id, ITrainExitSide sideA, ITrainExitSide sideB, ITrainExitStateNotifier notifier)
    {
        Id = id;
        State = TrainExitState.Disabled;
        _isOpenPending = false;
        _sideA = sideA;
        _sideB = sideB;
        _notifier = notifier;
    }

    public void Enable(TrainSideType sideType)
    {
        SetSafeSide(sideType);
        SetState(TrainExitState.Enabled);
        _ = TryResolvePendingOpenRequest();
    }

    public void Disable()
    {
        SetNoSafeSide();
        SetState(TrainExitState.Disabled);
    }

    public async Task RequestOpenAsync()
    {
        CreatePendingOpenRequest();
        await TryResolvePendingOpenRequest();
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

    private async Task TryResolvePendingOpenRequest()
    {
        if (!_isOpenPending)
        {
            return;
        }

        if (State != TrainExitState.Enabled)
        {
            return;
        }

        await OpenAsync();
        ClearPendingOpenRequest();
    }

    private async Task OpenAsync()
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

    private void HandleTrainExitSideException()
    {
        SetNoSafeSide();
        SetState(TrainExitState.Faulted);
        _notifier.NotifyTrainExitStateChanged(Id, State);
    }

    private void SetNoSafeSide()
    {
        _safeSide = null;
    }

    private void SetSafeSide(TrainSideType sideType)
    {
        _safeSide = sideType switch
        {
            TrainSideType.A => _sideA,
            TrainSideType.B => _sideB,
            _ => throw new ArgumentOutOfRangeException(nameof(sideType), $"Unknown side type: {sideType}")
        };
    }

    private void CreatePendingOpenRequest()
    {
        SetIsOpenPending(true);
        _sideA.HandleExitOpenPending();
        _sideB.HandleExitOpenPending();
    }

    private void ClearPendingOpenRequest() => SetIsOpenPending(false);

    private void SetIsOpenPending(bool isOpenPending) => _isOpenPending = isOpenPending;

    private void SetState(TrainExitState state) => State = state;
}