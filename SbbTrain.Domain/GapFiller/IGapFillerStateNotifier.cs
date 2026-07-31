public interface IGapFillerStateNotifier
{
    void NotifyGapFillerStateChanged(Guid gapFillerId, GapFillerState state);
}