using NSubstitute;

public record TrainExitSideTestGraph(
    ITrainExitSide Side,
    IExitDoor Door,
    IDoorIndicator DoorIndicator,
    ITrainGapFiller GapFiller,
    IReadOnlyList<IExitButton> Buttons
);

public record TrainExitTestGraph(
    ITrainExit Exit,
    TrainExitSideTestGraph SideA,
    TrainExitSideTestGraph SideB);