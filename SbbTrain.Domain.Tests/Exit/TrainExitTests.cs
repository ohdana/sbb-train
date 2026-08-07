using Xunit;
using NSubstitute;

public class TrainExitTests
{
    private readonly Guid _exitId;
    private readonly TrainExit _exit;
    private readonly ITrainExitNotifier _notifier;
    private readonly ITrainExitSide _sideA;
    private readonly ITrainExitSide _sideB;

    public TrainExitTests()
    {
        _notifier = Substitute.For<ITrainExitNotifier>();
        _sideA = Substitute.For<ITrainExitSide>();
        _sideB = Substitute.For<ITrainExitSide>();
        _exitId = Guid.NewGuid();
        _exit = new TrainExit(_exitId, _sideA, _sideB, _notifier);
    }

    [Fact]
    public void TrainExit_WhenCreated_IsDisabled()
    {
        // Arrange
        // Act
        // Assert
        Assert.Equal(TrainExitState.Disabled, _exit.State);
    }

    [Theory]
    [MemberData(nameof(TrainSideTypes))]
    public void TrainExit_WhenEnableCalledWithValidSideType_BecomesEnabled(TrainSideType sideType)
    {
        // Arrange
        _exit.Disable();

        // Act
        _exit.Enable(sideType);

        // Assert
        Assert.Equal(TrainExitState.Enabled, _exit.State);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(99)]
    public void TrainExit_WhenEnableCalledWithInvalidSideType_Throws(int invalidTypeValue)
    {
        // Arrange
        var invalidSideType = (TrainSideType)invalidTypeValue;

        // Act
        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => _exit.Enable(invalidSideType)); 
    }

    [Theory]
    [MemberData(nameof(TrainSideTypes))]
    public void TrainExit_WhenDisableCalled_BecomesDisabled(TrainSideType sideType)
    {
        // Arrange
        _exit.Enable(sideType);

        // Act
        _exit.Disable();

        // Assert
        Assert.Equal(TrainExitState.Disabled, _exit.State);
    }

    [Fact]
    public void TrainExit_WhenRequestOpenCalled_CallsSidesToHandle()
    {
        // Arrange
        // Act
        _exit.RequestOpen();

        // Assert
        _sideA.Received(1).HandlePendingOpenRequest();
        _sideB.Received(1).HandlePendingOpenRequest();
    }

    [Fact]
    public void TrainExit_WhenRequestOpenCalled_NotifiesExitOpenRequested()
    {
        // Arrange
        // Act
        _exit.RequestOpen();

        // Assert
        _notifier.Received(1).NotifyTrainExitOpenRequested(_exitId);
    }

    [Theory]
    [MemberData(nameof(TrainSideTypes))]
    public async Task TrainExit_WhenEnabledAndOpenCalled_OpensSafeSide(TrainSideType sideType)
    {
        // Arrange
        var (safeSide, anotherSide) = GetSides(sideType);
        _exit.Enable(sideType);

        // Act
        await _exit.OpenAsync();

        // Assert
        await safeSide.Received(1).OpenAsync();
        await anotherSide.DidNotReceive().OpenAsync();
    }

    [Fact]
    public async Task TrainExit_WhenDisabledAndOpenCalled_DoesntOpen()
    {
        // Arrange
        // Act
        await _exit.OpenAsync();

        // Assert
        await _sideA.DidNotReceive().OpenAsync();
        await _sideB.DidNotReceive().OpenAsync();
    }

    [Theory]
    [MemberData(nameof(TrainSideTypes))]
    public async Task TrainExit_WhenCloseCalled_ClosesSafeSide(TrainSideType sideType)
    {
        // Arrange
        var (safeSide, anotherSide) = GetSides(sideType);
        _exit.Enable(sideType);
        await _exit.OpenAsync();
        safeSide.ClearReceivedCalls();

        // Act
        await _exit.CloseAsync();

        // Assert
        await safeSide.Received(1).CloseAsync();
        await anotherSide.DidNotReceive().CloseAsync();
    }

    [Theory]
    [MemberData(nameof(TrainSideTypes))]
    public async Task TrainExit_WhenSideThrowsExceptionOnOpen_BecomesFaultedAndRethrows(
        TrainSideType sideType)
    {
        // Arrange
        var (safeSide, anotherSide) = GetSides(sideType);
        var sideException = new InvalidOperationException("Exit Side mechanism failed");
        safeSide.OpenAsync().Returns<Task>(_ => throw sideException);

        _exit.Enable(sideType);
        _notifier.ClearReceivedCalls();

        // Act
        var thrown = await Record.ExceptionAsync(() => _exit.OpenAsync());

        // Assert
        Assert.Same(sideException, thrown);
        Assert.Equal(TrainExitState.Faulted, _exit.State);
        _notifier.Received(1).NotifyTrainExitStateChanged(_exitId, _exit.State);
    }

    [Theory]
    [MemberData(nameof(TrainSideTypes))]
    public async Task TrainExit_WhenSideThrowsExceptionOnClose_BecomesFaultedAndRethrows(
        TrainSideType sideType)
    {
        // Arrange
        var (safeSide, anotherSide) = GetSides(sideType);
        var sideException = new InvalidOperationException("Exit Side mechanism failed");
        safeSide.CloseAsync().Returns<Task>(_ => throw sideException);

        _exit.Enable(sideType);
        await _exit.OpenAsync();
        _notifier.ClearReceivedCalls();

        // Act
        var thrown = await Record.ExceptionAsync(() => _exit.CloseAsync());

        // Assert
        Assert.Same(sideException, thrown);
        Assert.Equal(TrainExitState.Faulted, _exit.State);
        _notifier.Received(1).NotifyTrainExitStateChanged(_exitId, _exit.State);
    }

    private (ITrainExitSide safeSide, ITrainExitSide anotherSide) GetSides(TrainSideType sideType) =>
        sideType == TrainSideType.A ? (_sideA, _sideB) : (_sideB, _sideA);

    public static IEnumerable<object[]> TrainSideTypes =>
        new List<object[]>
        {
            new object[] { TrainSideType.A },
            new object[] { TrainSideType.B }
        };
}