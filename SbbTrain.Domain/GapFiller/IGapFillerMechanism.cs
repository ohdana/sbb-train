public interface IGapFillerMechanism
{
    Task ExtendAsync();
    Task RetractAsync();
}