public interface IExitDoor :
    IDoor,
    IEventReceiver,
    ITimeoutable,
    IDisposable
{
    event Action? StateChanged;
    event Action? TimedOut;
}