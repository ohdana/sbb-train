public interface ITrainGapFiller :
    IGapFiller,
    ITimeoutable
{
    event Action? TimedOut;

    void ResetAutoRetractTimer();
    void StopAutoRetractTimer();
}