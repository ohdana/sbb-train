public interface ITrainExit : IExit
{
    void Enable(TrainSideType sideType);
    void Disable();

    event Action? StateChanged;
    event Action? OpenRequested;
}