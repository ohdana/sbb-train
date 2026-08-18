public interface IExitEventHandler
{
    void HandlePendingOpenRequest();
    void HandleButtonNotificationRequest(EventType eventType);
}