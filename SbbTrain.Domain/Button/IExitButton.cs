public interface IExitButton : IButton, IEventReceiver
{
    event Action? ButtonPressed;
}