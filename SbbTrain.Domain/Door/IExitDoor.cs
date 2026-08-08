public interface IExitDoor :
    IDoor,
    IEventReceiver,
    ITimeoutable,
    IDisposable
{
    event Action? StateChanged;
}