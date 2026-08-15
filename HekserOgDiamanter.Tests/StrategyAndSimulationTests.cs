using HekserOgDiamanter.Configuration;
using HekserOgDiamanter.Domain;
using HekserOgDiamanter.Simulation;
using HekserOgDiamanter.Strategies;

namespace HekserOgDiamanter.Tests;

public sealed class StrategyAndSimulationTests
{
    [Fact]
    public void HoardingStrategyProgressesFromIncomeToPurchaseToSearch()
    {
        var strategy = new ResourceHoardingStrategy(HoardingTarget.ClearDiamond);
        var player = new Player("P");
        var state = State(player);

        Assert.Equal(TurnActionType.PoorRelief, strategy.ChooseTurn(state, player, new Random(1)).Action);

        player.Money = 2;
        var purchase = strategy.ChooseTurn(state, player, new Random(1));
        Assert.Equal(TurnActionType.Buy, purchase.Action);
        Assert.Equal(1, purchase.BuyShovels);

        player.Money = 0;
        player.Shovels = 1;
        Assert.Equal(TurnActionType.Search, strategy.ChooseTurn(state, player, new Random(1)).Action);
    }

    [Fact]
    public void SameSeedProducesSameResultsAtDifferentParallelism()
    {
        var config = Config(games: 50, parallelism: 1);
        var sequential = SimulationRunner.Run(config)[0].Games;
        config.MaxDegreeOfParallelism = 4;
        var parallel = SimulationRunner.Run(config)[0].Games;

        Assert.Equal(sequential.Select(game => game.Seed), parallel.Select(game => game.Seed));
        Assert.Equal(sequential.Select(game => game.Turns), parallel.Select(game => game.Turns));
        Assert.Equal(sequential.Select(game => game.EndReason), parallel.Select(game => game.EndReason));
        Assert.Equal(
            sequential.Select(game => string.Join(',', game.PeakTotal.OrderBy(pair => pair.Key).Select(pair => pair.Value))),
            parallel.Select(game => string.Join(',', game.PeakTotal.OrderBy(pair => pair.Key).Select(pair => pair.Value))));
    }

    [Fact]
    public void ResourceTrackerCapturesTotalAndLargestSinglePlayer()
    {
        var players = new[]
        {
            new Player("A") { Money = 19, ClearDiamonds = 3 },
            new Player("B") { Money = 7, ClearDiamonds = 5 }
        };
        var tracker = new ResourceTracker();
        tracker.Capture(players);

        Assert.Equal(8, tracker.PeakTotal[ResourceMetric.ClearDiamond]);
        Assert.Equal(5, tracker.PeakSinglePlayer[ResourceMetric.ClearDiamond]);
        Assert.Equal(3, tracker.PeakByPlayer["A"][ResourceMetric.ClearDiamond]);
        Assert.Equal(5, tracker.PeakByPlayer["B"][ResourceMetric.ClearDiamond]);
        Assert.Equal(26, tracker.PeakTotal[ResourceMetric.Money]);
        Assert.Equal(6, tracker.PeakTotal[ResourceMetric.Coin1]);
        Assert.Equal(2, tracker.PeakTotal[ResourceMetric.Coin5]);
        Assert.Equal(1, tracker.PeakTotal[ResourceMetric.Coin10]);
    }

    private static SimulationConfig Config(int games, int parallelism) => new()
    {
        Seed = 42,
        MaxDegreeOfParallelism = parallelism,
        Scenarios =
        [
            new ScenarioConfig
            {
                Name = "Deterministic",
                Games = games,
                MaxTurns = 5000,
                Players =
                [
                    new PlayerConfig { Name = "A", Strategy = new StrategyConfig { Type = StrategyType.Random } },
                    new PlayerConfig { Name = "B", Strategy = new StrategyConfig { Type = StrategyType.Random } }
                ]
            }
        ]
    };

    private static GameState State(Player player)
    {
        var board = new BoardState
        {
            TreasureDecks = Enum.GetValues<TreasureLocation>().ToDictionary(
                location => location,
                _ => new CardDeck<TreasureCardType>([TreasureCardType.ClearDiamond])),
            WitchDeck = new CardDeck<WitchCardType>([WitchCardType.Win])
        };
        return new GameState { Board = board, Players = [player], Resources = new ResourceTracker() };
    }
}
