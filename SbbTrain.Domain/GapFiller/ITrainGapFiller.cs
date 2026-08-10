public interface ITrainGapFiller :
    IGapFiller,
    IEventReceiver,
    ITimeoutable
{
    event Action? TimedOut;
}