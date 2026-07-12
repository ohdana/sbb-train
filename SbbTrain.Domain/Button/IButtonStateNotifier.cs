public interface IButtonStateNotifier
{
    void NotifyButtonStateChanged(Guid buttonId, ButtonState newState);
}