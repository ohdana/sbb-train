public interface IGapFiller
{
    Guid Id { get; }
    GapFillerState State { get; }
    Task ExtendAsync();
    Task RetractAsync();
}