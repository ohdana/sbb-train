public interface ITrainExit : IExit
{
    void Enable(TrainSideType sideType);
    void Disable();
}