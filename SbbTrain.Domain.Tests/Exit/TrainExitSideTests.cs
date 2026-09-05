using Xunit;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

public class TrainExitSideTests
{
    private readonly ITrainExitSide _exitSide;
    private readonly IExitDoor _door;
    private readonly IDoorIndicator _doorIndicator;
    private readonly ITrainGapFiller _gapFiller;
    private readonly IEnumerable<IExitButton> _buttons;
    private readonly ITrainLogger _logger;
    private readonly TrainSideType _sideType;

    public TrainExitSideTests()
    {
        _sideType = TrainSideType.A;
        _door = Substitute.For<IExitDoor>();
        _doorIndicator = Substitute.For<IDoorIndicator>();
        _gapFiller = Substitute.For<ITrainGapFiller>();
        _buttons = GetButtonSubstitutes(5);
        _logger = Substitute.For<ITrainLogger>();
        _exitSide = new TrainExitSide(_sideType, _door, _doorIndicator, _gapFiller, _buttons, _logger);
    }

    [Fact]
    public async Task TrainExitSide_WhenOpenCalled_ExtendsGapFillerThenOpensDoor()
    {
        // Arrange
        // Act
        await _exitSide.OpenAsync();

        // Assert
        Received.InOrder(() =>
        {
            _gapFiller.ExtendAsync();
            _door.OpenAsync();
        });
    }

    [Fact]
    public async Task TrainExitSide_WhenCloseCalled_ClosesDoorThenRetractsGapFiller()
    {
        // Arrange
        // Act
        await _exitSide.CloseAsync();

        // Assert
        Received.InOrder(() =>
        {
            _door.CloseAsync();
            _gapFiller.RetractAsync();
        });
    }

    [Fact]
    public async Task TrainExitSide_WhenCloseCalledAndDoorCloseUnsuccessful_DoesntRetractGapFiller()
    {
        // Arrange
        _door.CloseAsync().Returns<Task>(_ => throw new DoorCloseInterruptedException(_door.Id));

        // Act
        var closeTask = _exitSide.CloseAsync();
        var exception = await Record.ExceptionAsync(() => closeTask);

        // Assert
        Assert.IsType<DoorCloseInterruptedException>(exception);
        await _gapFiller.DidNotReceive().RetractAsync();
    }

    [Fact]
    public async Task TrainExitSide_WhenDoorFaultsOnOpen_Rethrows()
    {
        // Arrange
        var exception = new InvalidOperationException("Door jammed");
        _door.OpenAsync().ThrowsAsync(exception);

        // Act
        var thrown = await Record.ExceptionAsync(() => _exitSide.OpenAsync());

        // Assert
        Assert.Same(exception, thrown);
    }

    [Fact]
    public async Task TrainExitSide_WhenDoorFaultsOnClose_Rethrows()
    {
        // Arrange
        var exception = new InvalidOperationException("Door jammed");
        _door.CloseAsync().ThrowsAsync(exception);

        // Act
        var thrown = await Record.ExceptionAsync(() => _exitSide.CloseAsync());

        // Assert
        Assert.Same(exception, thrown);
    }

    [Fact]
    public async Task TrainExitSide_WhenGapFillerFaultsOnExtend_DoesntOpenDoorAndRethrows()
    {
        // Arrange
        var exception = new InvalidOperationException("Gap filler jammed");
        _gapFiller.ExtendAsync().ThrowsAsync(exception);

        // Act
        var thrown = await Record.ExceptionAsync(() => _exitSide.OpenAsync());

        // Assert
        await _door.DidNotReceive().OpenAsync();
        Assert.Same(exception, thrown);
    }

    [Fact]
    public async Task TrainExitSide_WhenGapFillerFaultsOnRetract_Rethrows()
    {
        // Arrange
        var exception = new InvalidOperationException("Gap filler jammed");
        _gapFiller.RetractAsync().ThrowsAsync(exception);

        // Act
        var thrown = await Record.ExceptionAsync(() => _exitSide.CloseAsync());

        // Assert
        Assert.Same(exception, thrown);
    }

    private IEnumerable<IExitButton> GetButtonSubstitutes(int quantity)
    {
        return Enumerable.Range(0, quantity)
                         .Select(i => Substitute.For<IExitButton>());
    }
}
