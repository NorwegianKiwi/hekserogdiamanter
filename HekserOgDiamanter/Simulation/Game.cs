using HekserOgDiamanter.Configuration;
using HekserOgDiamanter.Domain;
using HekserOgDiamanter.Strategies;

namespace HekserOgDiamanter.Simulation;

public sealed class Game
{
    private readonly ScenarioConfig _scenario;
    private readonly Random _random;
    private readonly IReadOnlyList<IPlayerStrategy> _strategies;
    private readonly Action<string>? _trace;

    public Game(ScenarioConfig scenario, int seed, Action<string>? trace = null)
        : this(scenario, seed, null, null, trace)
    {
    }

    internal Game(
        ScenarioConfig scenario,
        int seed,
        BoardState? board,
        IReadOnlyList<IPlayerStrategy>? strategies,
        Action<string>? trace = null)
    {
        _scenario = scenario;
        _random = new Random(seed);
        Seed = seed;
        _trace = trace;

        var players = scenario.Players.Select(StandardDeckFactory.CreatePlayer).ToArray();
        _strategies = strategies ?? scenario.Players.Select(player => StrategyFactory.Create(player.Strategy)).ToArray();
        if (_strategies.Count != players.Length) throw new ArgumentException("One strategy is required per player.", nameof(strategies));
        State = new GameState
        {
            Board = board ?? StandardDeckFactory.CreateBoard(scenario, _random),
            Players = players,
            Resources = new ResourceTracker(),
            CurrentPlayerIndex = scenario.StartingPlayerIndex
        };
        foreach (var color in players.SelectMany(player => player.ColoredDiamonds))
            State.FoundColoredDiamonds.Add(color);
        RecordStartingResources(players);
        State.Resources.Capture(players);
    }

    public int Seed { get; }
    public GameState State { get; }

    public GameResult Run(int runNumber = 1)
    {
        var turnOrder = Enumerable.Range(0, State.Players.Count)
            .Select(offset => (_scenario.StartingPlayerIndex + offset) % State.Players.Count)
            .ToArray();

        while (true)
        {
            State.Rounds++;
            foreach (var playerIndex in turnOrder)
            {
                State.CurrentPlayerIndex = playerIndex;
                var player = State.CurrentPlayer;
                var decision = _strategies[playerIndex].ChooseTurn(State, player, _random);
                ValidateDecision(player, decision);
                ExecuteDecision(player, _strategies[playerIndex], decision);
                State.Turns++;
                State.Resources.Capture(State.Players);
                UpdateEndTrigger();

                if (State.Turns >= _scenario.MaxTurns)
                    return BuildResult(runNumber, false, GameEndReason.TurnLimit);
            }

            if (State.PendingEndReason is not null)
                return BuildResult(runNumber, true, State.PendingEndReason.Value);
        }
    }

    private void ExecuteDecision(Player player, IPlayerStrategy strategy, TurnDecision decision)
    {
        switch (decision.Action)
        {
            case TurnActionType.Search:
                Search(player, strategy, decision.Location!.Value);
                break;
            case TurnActionType.Sell:
                player.ClearDiamonds -= decision.SellClearDiamonds;
                player.Gold -= decision.SellGold;
                var income = decision.SellClearDiamonds * 10 + decision.SellGold * 4;
                player.Money += income;
                Trace($"{player.Name} sells {decision.SellClearDiamonds} clear diamond(s) and {decision.SellGold} gold for {income} kr.");
                break;
            case TurnActionType.Buy:
                var cost = decision.BuyPickaxes * 3 + decision.BuyShovels * 2;
                player.Money -= cost;
                player.Pickaxes += decision.BuyPickaxes;
                player.Shovels += decision.BuyShovels;
                State.Resources.RecordDistribution(ResourceMetric.Pickaxe, decision.BuyPickaxes);
                State.Resources.RecordDistribution(ResourceMetric.Shovel, decision.BuyShovels);
                Trace($"{player.Name} buys {decision.BuyPickaxes} pickaxe(s) and {decision.BuyShovels} shovel(s) for {cost} kr.");
                break;
            case TurnActionType.PoorRelief:
                player.Money++;
                State.Resources.RecordDistribution(ResourceMetric.Money, 1);
                Trace($"{player.Name} takes 1 kr from poor relief.");
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void Search(Player player, IPlayerStrategy strategy, TreasureLocation location)
    {
        var tool = GameRules.ToolFor(location);
        player.RemoveTool(tool);
        var card = State.Board.TreasureDecks[location].Draw();
        player.CollectedCards[location]++;
        Trace($"{player.Name} searches {location} with a {tool} and draws {card}.");

        switch (card)
        {
            case TreasureCardType.Gold:
                player.Gold++;
                State.Resources.RecordDistribution(ResourceMetric.Gold, 1);
                break;
            case TreasureCardType.Shovel:
                player.Shovels++;
                State.Resources.RecordDistribution(ResourceMetric.Shovel, 1);
                break;
            case TreasureCardType.Pickaxe:
                player.Pickaxes++;
                State.Resources.RecordDistribution(ResourceMetric.Pickaxe, 1);
                break;
            case TreasureCardType.ClearDiamond:
                player.ClearDiamonds++;
                State.Resources.RecordDistribution(ResourceMetric.ClearDiamond, 1);
                break;
            case TreasureCardType.ColoredDiamond:
                var color = GameRules.ColorFor(location);
                if (!State.FoundColoredDiamonds.Add(color) || !player.ColoredDiamonds.Add(color))
                    throw new InvalidOperationException($"Colored diamond {color} was awarded twice.");
                State.Resources.RecordDistribution(ResourceMetric.ColoredDiamond, 1);
                break;
            case TreasureCardType.Witch:
                ResolveWitchFight(player, strategy, tool);
                break;
            default:
                var money = GameRules.MoneyValue(card);
                player.Money += money;
                State.Resources.RecordDistribution(ResourceMetric.Money, money);
                break;
        }
    }

    private void ResolveWitchFight(Player player, IPlayerStrategy strategy, ToolType tool)
    {
        while (true)
        {
            var card = State.Board.WitchDeck.Draw();
            Trace($"  Witch card: {card}.");
            State.Board.WitchDeck.Discard(card);

            switch (card)
            {
                case WitchCardType.Win:
                    player.AddTool(tool);
                    State.Resources.RecordDistribution(tool == ToolType.Pickaxe ? ResourceMetric.Pickaxe : ResourceMetric.Shovel, 1);
                    Trace($"  {player.Name} wins and gets the {tool} back.");
                    return;
                case WitchCardType.Lose:
                    LoseDiamond(player, strategy);
                    return;
                case WitchCardType.Retry:
                    if (player.Money >= 2 && strategy.ShouldRetryWitch(State, player, _random))
                    {
                        player.Money -= 2;
                        Trace($"  {player.Name} pays 2 kr to retry.");
                        continue;
                    }
                    LoseDiamond(player, strategy);
                    return;
                case WitchCardType.Reshuffle:
                    State.Board.WitchDeck.ShuffleDiscardIntoDrawPile(_random);
                    Trace("  Used witch cards are shuffled back into the deck.");
                    continue;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    private void LoseDiamond(Player player, IPlayerStrategy strategy)
    {
        if (player.ClearDiamonds + player.ColoredDiamonds.Count == 0)
        {
            Trace($"  {player.Name} loses, but has no diamond.");
            return;
        }

        var choice = strategy.ChooseDiamondToLose(State, player, _random);
        if (choice.IsClear)
        {
            if (player.ClearDiamonds <= 0) throw new InvalidOperationException("Strategy selected a clear diamond the player does not own.");
            player.ClearDiamonds--;
            Trace($"  {player.Name} loses a clear diamond.");
        }
        else
        {
            if (choice.Color is null || !player.ColoredDiamonds.Remove(choice.Color.Value))
                throw new InvalidOperationException("Strategy selected a colored diamond the player does not own.");
            Trace($"  {player.Name} loses the {choice.Color} diamond.");
        }
    }

    private void ValidateDecision(Player player, TurnDecision decision)
    {
        switch (decision.Action)
        {
            case TurnActionType.Search:
                if (decision.Location is null || !State.SearchableLocations(player).Contains(decision.Location.Value))
                    throw new InvalidOperationException($"Illegal search decision by {player.Name}.");
                break;
            case TurnActionType.Sell:
                if (decision.SellClearDiamonds < 0 || decision.SellGold < 0 ||
                    decision.SellClearDiamonds > player.ClearDiamonds || decision.SellGold > player.Gold ||
                    decision.SellClearDiamonds + decision.SellGold == 0)
                    throw new InvalidOperationException($"Illegal sale decision by {player.Name}.");
                break;
            case TurnActionType.Buy:
                var cost = decision.BuyPickaxes * 3 + decision.BuyShovels * 2;
                if (decision.BuyPickaxes < 0 || decision.BuyShovels < 0 ||
                    decision.BuyPickaxes + decision.BuyShovels == 0 || cost > player.Money)
                    throw new InvalidOperationException($"Illegal purchase decision by {player.Name}.");
                break;
            case TurnActionType.PoorRelief:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void UpdateEndTrigger()
    {
        if (State.PendingEndReason is not null) return;
        if (State.Board.TreasureDecks.Values.Any(deck => deck.Count == 0))
            State.PendingEndReason = GameEndReason.EmptyTreasureDeck;
        else if (State.FoundColoredDiamonds.Count == 6)
            State.PendingEndReason = GameEndReason.AllColoredDiamondsFound;
    }

    private GameResult BuildResult(int runNumber, bool completed, GameEndReason reason)
    {
        var playerResults = State.Players.Select(player => new PlayerResult(player.Name, player.Score(), player.Money)).ToArray();
        var topScore = playerResults.Max(player => player.Score);
        var finalists = playerResults.Where(player => player.Score == topScore).ToArray();
        var topMoney = finalists.Max(player => player.Money);
        var winners = completed
            ? finalists.Where(player => player.Money == topMoney).Select(player => player.Name).ToArray()
            : [];
        return new GameResult(
            runNumber,
            Seed,
            completed,
            reason,
            State.Turns,
            State.Rounds,
            new Dictionary<ResourceMetric, int>(State.Resources.PeakTotal),
            new Dictionary<ResourceMetric, int>(State.Resources.PeakSinglePlayer),
            new Dictionary<ResourceMetric, int>(State.Resources.DistributedFromBoard),
            playerResults,
            winners);
    }

    private void RecordStartingResources(IEnumerable<Player> players)
    {
        foreach (var player in players)
        {
            State.Resources.RecordDistribution(ResourceMetric.Money, player.Money);
            State.Resources.RecordDistribution(ResourceMetric.ClearDiamond, player.ClearDiamonds);
            State.Resources.RecordDistribution(ResourceMetric.ColoredDiamond, player.ColoredDiamonds.Count);
            State.Resources.RecordDistribution(ResourceMetric.Gold, player.Gold);
            State.Resources.RecordDistribution(ResourceMetric.Pickaxe, player.Pickaxes);
            State.Resources.RecordDistribution(ResourceMetric.Shovel, player.Shovels);
        }
    }

    private void Trace(string message) => _trace?.Invoke($"Round {State.Rounds}, turn {State.Turns + 1}: {message}");
}
