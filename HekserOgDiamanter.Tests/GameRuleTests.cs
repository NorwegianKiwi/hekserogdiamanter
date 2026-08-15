using HekserOgDiamanter.Configuration;
using HekserOgDiamanter.Domain;
using HekserOgDiamanter.Simulation;
using HekserOgDiamanter.Strategies;

namespace HekserOgDiamanter.Tests;

public sealed class GameRuleTests
{
    [Theory]
    [InlineData(TreasureCardType.Gold, 0, 1, 0, 0, 0)]
    [InlineData(TreasureCardType.ClearDiamond, 1, 0, 0, 0, 0)]
    [InlineData(TreasureCardType.Pickaxe, 0, 0, 1, 0, 0)]
    [InlineData(TreasureCardType.Shovel, 0, 0, 0, 1, 0)]
    [InlineData(TreasureCardType.Money5, 0, 0, 0, 0, 5)]
    public void TreasureCardsAwardExpectedResource(
        TreasureCardType card,
        int clear,
        int gold,
        int pickaxes,
        int shovels,
        int money)
    {
        var scenario = Scenario(2, maxTurns: 1, first: new StartingResourcesConfig { Pickaxes = 1 });
        var game = InjectedGame(scenario, Board(card), [new FixedStrategy(new TurnDecision(TurnActionType.Search, TreasureLocation.Ruins)), new FixedStrategy()]);

        game.Run();
        var player = game.State.Players[0];

        Assert.Equal(clear, player.ClearDiamonds);
        Assert.Equal(gold, player.Gold);
        Assert.Equal(pickaxes, player.Pickaxes);
        Assert.Equal(shovels, player.Shovels);
        Assert.Equal(money, player.Money);
        Assert.Equal(1, player.CollectedCards[TreasureLocation.Ruins]);
    }

    [Fact]
    public void ColoredDiamondIsRecordedAsFoundEvenIfOwnershipCanLaterChange()
    {
        var scenario = Scenario(2, maxTurns: 1, first: new StartingResourcesConfig { Pickaxes = 1 });
        var game = InjectedGame(scenario, Board(TreasureCardType.ColoredDiamond), [new FixedStrategy(new TurnDecision(TurnActionType.Search, TreasureLocation.Ruins)), new FixedStrategy()]);

        game.Run();

        Assert.Contains(DiamondColor.Blue, game.State.FoundColoredDiamonds);
        Assert.Contains(DiamondColor.Blue, game.State.Players[0].ColoredDiamonds);
    }

    [Fact]
    public void WitchWinReturnsUsedTool()
    {
        var game = WitchGame([WitchCardType.Win]);
        game.Run();
        Assert.Equal(1, game.State.Players[0].Pickaxes);
    }

    [Fact]
    public void WitchLossReturnsChosenDiamond()
    {
        var game = WitchGame([WitchCardType.Lose]);
        game.Run();
        Assert.Equal(0, game.State.Players[0].ClearDiamonds);
    }

    [Fact]
    public void WitchRetryCanPayAndDrawAgain()
    {
        var game = WitchGame([WitchCardType.Retry, WitchCardType.Win], shouldRetry: true);
        game.Run();
        Assert.Equal(0, game.State.Players[0].Money);
        Assert.Equal(1, game.State.Players[0].Pickaxes);
    }

    [Fact]
    public void WitchRetryDeclinedCountsAsLoss()
    {
        var game = WitchGame([WitchCardType.Retry], shouldRetry: false);
        game.Run();
        Assert.Equal(2, game.State.Players[0].Money);
        Assert.Equal(0, game.State.Players[0].ClearDiamonds);
    }

    [Fact]
    public void SaleReturnsResourcesAndPaysConfiguredValues()
    {
        var scenario = Scenario(2, maxTurns: 1, first: new StartingResourcesConfig { ClearDiamonds = 2, Gold = 1 });
        var game = InjectedGame(scenario, Board(TreasureCardType.Gold),
            [new FixedStrategy(new TurnDecision(TurnActionType.Sell, SellClearDiamonds: 2, SellGold: 1)), new FixedStrategy()]);

        game.Run();

        Assert.Equal(24, game.State.Players[0].Money);
        Assert.Equal(0, game.State.Players[0].ClearDiamonds);
        Assert.Equal(0, game.State.Players[0].Gold);
    }

    [Fact]
    public void PurchasePaysThreeForPickaxeAndTwoForShovel()
    {
        var scenario = Scenario(2, maxTurns: 1, first: new StartingResourcesConfig { Money = 8 });
        var game = InjectedGame(scenario, Board(TreasureCardType.Gold),
            [new FixedStrategy(new TurnDecision(TurnActionType.Buy, BuyPickaxes: 2, BuyShovels: 1)), new FixedStrategy()]);

        game.Run();

        Assert.Equal(0, game.State.Players[0].Money);
        Assert.Equal(2, game.State.Players[0].Pickaxes);
        Assert.Equal(1, game.State.Players[0].Shovels);
    }

    [Fact]
    public void PoorReliefPaysOneKrone()
    {
        var scenario = Scenario(2, maxTurns: 1, first: new StartingResourcesConfig());
        var game = InjectedGame(scenario, Board(TreasureCardType.Gold), [new FixedStrategy(), new FixedStrategy()]);

        game.Run();

        Assert.Equal(1, game.State.Players[0].Money);
    }

    [Fact]
    public void EmptyDeckFinishesRoundSoAllPlayersGetEqualTurns()
    {
        var scenario = Scenario(3, maxTurns: 20, first: new StartingResourcesConfig { Pickaxes = 1 });
        var strategies = new IPlayerStrategy[]
        {
            new FixedStrategy(new TurnDecision(TurnActionType.Search, TreasureLocation.Ruins)),
            new FixedStrategy(),
            new FixedStrategy()
        };
        var game = InjectedGame(scenario, Board(TreasureCardType.Gold), strategies);

        var result = game.Run();

        Assert.True(result.Completed);
        Assert.Equal(GameEndReason.EmptyTreasureDeck, result.EndReason);
        Assert.Equal(3, result.Turns);
        Assert.Equal(1, result.Rounds);
    }

    [Fact]
    public void FindingSixthColoredDiamondFinishesRound()
    {
        var firstResources = new StartingResourcesConfig
        {
            Shovels = 1,
            ColoredDiamonds = [DiamondColor.Blue, DiamondColor.Purple, DiamondColor.Red]
        };
        var scenario = Scenario(2, maxTurns: 20, first: firstResources);
        scenario.Players[1].StartingResources = new StartingResourcesConfig
        {
            ColoredDiamonds = [DiamondColor.Green, DiamondColor.Orange]
        };
        var strategies = new IPlayerStrategy[]
        {
            new FixedStrategy(new TurnDecision(TurnActionType.Search, TreasureLocation.Beach)),
            new FixedStrategy()
        };
        var game = InjectedGame(scenario, Board(TreasureCardType.ColoredDiamond, avoidEmptyAfterFirst: true), strategies);

        var result = game.Run();

        Assert.Equal(GameEndReason.AllColoredDiamondsFound, result.EndReason);
        Assert.Equal(2, result.Turns);
        Assert.Contains(DiamondColor.Yellow, game.State.Players[0].ColoredDiamonds);
    }

    private static Game WitchGame(IReadOnlyList<WitchCardType> witchCards, bool shouldRetry = false)
    {
        var scenario = Scenario(2, maxTurns: 1, first: new StartingResourcesConfig { Pickaxes = 1, ClearDiamonds = 1, Money = 2 });
        var strategies = new IPlayerStrategy[]
        {
            new FixedStrategy(new TurnDecision(TurnActionType.Search, TreasureLocation.Ruins), shouldRetry),
            new FixedStrategy()
        };
        return InjectedGame(scenario, Board(TreasureCardType.Witch, witchCards), strategies);
    }

    private static ScenarioConfig Scenario(int players, int maxTurns, StartingResourcesConfig first)
    {
        var configs = Enumerable.Range(1, players).Select(index => new PlayerConfig
        {
            Name = $"P{index}",
            StartingPreset = StartingPreset.Custom,
            StartingResources = index == 1 ? first : new StartingResourcesConfig(),
            Strategy = new StrategyConfig { Type = StrategyType.Random }
        }).ToList();
        return new ScenarioConfig { Name = "Test", Games = 1, MaxTurns = maxTurns, Players = configs };
    }

    private static BoardState Board(
        TreasureCardType firstCard,
        IReadOnlyList<WitchCardType>? witches = null,
        bool avoidEmptyAfterFirst = false)
    {
        var decks = Enum.GetValues<TreasureLocation>().ToDictionary(
            location => location,
            location => new CardDeck<TreasureCardType>(avoidEmptyAfterFirst
                ? [location is TreasureLocation.Ruins or TreasureLocation.Beach ? firstCard : TreasureCardType.Gold, TreasureCardType.Gold]
                : [location is TreasureLocation.Ruins or TreasureLocation.Beach ? firstCard : TreasureCardType.Gold]));
        return new BoardState
        {
            TreasureDecks = decks,
            WitchDeck = new CardDeck<WitchCardType>(witches ?? [WitchCardType.Win])
        };
    }

    private static Game InjectedGame(ScenarioConfig scenario, BoardState board, IReadOnlyList<IPlayerStrategy> strategies) =>
        new(scenario, 123, board, strategies);

    private sealed class FixedStrategy(TurnDecision? decision = null, bool retry = false) : IPlayerStrategy
    {
        public TurnDecision ChooseTurn(GameState state, Player player, Random random) =>
            decision ?? new TurnDecision(TurnActionType.PoorRelief);

        public bool ShouldRetryWitch(GameState state, Player player, Random random) => retry;

        public DiamondLossChoice ChooseDiamondToLose(GameState state, Player player, Random random) =>
            player.ClearDiamonds > 0
                ? DiamondLossChoice.Clear
                : DiamondLossChoice.Colored(player.ColoredDiamonds.First());
    }
}
