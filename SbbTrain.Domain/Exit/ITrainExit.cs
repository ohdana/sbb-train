public interface ITrainExit : IExit
{
    void Enable(TrainSideType sideType);
    void Disable();
    void RequestOpen();

    event Action? StateChanged;
    event Action? OpenRequested;
}