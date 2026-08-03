public interface ITrainExitStateNotifier
{
    void NotifyTrainExitStateChanged(Guid exitId, TrainExitState state);
}