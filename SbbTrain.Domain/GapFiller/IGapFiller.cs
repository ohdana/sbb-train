public interface IGapFiller
{
    Guid Id { get; }
    GapFillerState State { get; }
    void Extend();
    void Retract();
}