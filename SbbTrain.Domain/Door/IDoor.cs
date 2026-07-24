public interface IDoor
{
    Guid Id { get; }
    DoorState State { get; }
    Task OpenAsync();
    Task CloseAsync();
}