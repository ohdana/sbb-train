public class PendingOpenRequestResolver : IPendingOpenRequestResolver
{
    private readonly ITrainExit _exit;
    private readonly IPendingOpenRequestLogger _logger;
    private bool _hasPendingOpenRequest;

    public PendingOpenRequestResolver(ITrainExit exit, IPendingOpenRequestLogger logger)
    {
        _exit = exit;
        _logger = logger;
        _exit.OpenRequested += OnOpenRequested;
        _exit.StateChanged += OnStateChanged;
    }

    private void OnOpenRequested()
    {
        SetHasPendingOpenRequest(true);
        TryResolvePendingRequest();
    }

    private void OnStateChanged()
    {
        if (!_hasPendingOpenRequest)
        {
            return;
        }

        TryResolvePendingRequest();
    }

    private void TryResolvePendingRequest()
    {
        if (_exit.State != TrainExitState.Enabled)
        {
            return;
        }

        _ = ResolvePendingRequest();
    }

    private async Task ResolvePendingRequest()
    {
        try
        {
            await _exit.OpenAsync();
        }
        catch (Exception exception)
        {
            _logger.LogResolveFailure(_exit.Id, exception);
        }
        finally
        {
            SetHasPendingOpenRequest(false);
        }
    }

    private void SetHasPendingOpenRequest(bool value) => _hasPendingOpenRequest = value;
}