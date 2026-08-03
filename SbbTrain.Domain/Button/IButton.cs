public interface IButton
{
    Guid Id { get; }
    ButtonState State { get; }
    
    void Press();
}