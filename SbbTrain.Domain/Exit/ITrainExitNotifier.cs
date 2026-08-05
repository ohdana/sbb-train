public interface ITrainExitNotifier
{
    void NotifyTrainExitOpenRequested(Guid exitId);
    void NotifyTrainExitStateChanged(Guid exitId, TrainExitState state);
}