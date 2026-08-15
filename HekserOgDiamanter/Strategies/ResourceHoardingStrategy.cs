using HekserOgDiamanter.Domain;

namespace HekserOgDiamanter.Strategies;

public sealed class ResourceHoardingStrategy(HoardingTarget target) : IPlayerStrategy
{
    public HoardingTarget Target { get; } = target;

    public TurnDecision ChooseTurn(GameState state, Player player, Random random)
    {
        var searchable = state.SearchableLocations(player).ToArray();
        if (searchable.Length > 0)
            return new TurnDecision(TurnActionType.Search, BestLocation(state, searchable, random));

        var purchasableTool = BestPurchasableTool(state, player);
        if (purchasableTool is not null)
        {
            if (Target == HoardingTarget.Pickaxe && purchasableTool == ToolType.Pickaxe)
                return new TurnDecision(TurnActionType.Buy, BuyPickaxes: player.Money / 3);
            if (Target == HoardingTarget.Shovel && purchasableTool == ToolType.Shovel)
                return new TurnDecision(TurnActionType.Buy, BuyShovels: player.Money / 2);
            return purchasableTool == ToolType.Pickaxe
                ? new TurnDecision(TurnActionType.Buy, BuyPickaxes: 1)
                : new TurnDecision(TurnActionType.Buy, BuyShovels: 1);
        }

        var sellClear = Target == HoardingTarget.ClearDiamond ? 0 : player.ClearDiamonds;
        var sellGold = Target == HoardingTarget.Gold ? 0 : player.Gold;
        if (sellClear + sellGold > 0)
            return new TurnDecision(TurnActionType.Sell, SellClearDiamonds: sellClear, SellGold: sellGold);

        return new TurnDecision(TurnActionType.PoorRelief);
    }

    public bool ShouldRetryWitch(GameState state, Player player, Random random)
    {
        if (player.Money < 2 || player.ClearDiamonds + player.ColoredDiamonds.Count == 0) return false;
        return Target == HoardingTarget.ClearDiamond || player.ColoredDiamonds.Count > 0 && Target != HoardingTarget.Money;
    }

    public DiamondLossChoice ChooseDiamondToLose(GameState state, Player player, Random random)
    {
        if (Target == HoardingTarget.ClearDiamond && player.ColoredDiamonds.Count > 0)
            return DiamondLossChoice.Colored(player.ColoredDiamonds.Order().First());
        if (player.ClearDiamonds > 0) return DiamondLossChoice.Clear;
        if (player.ColoredDiamonds.Count > 0) return DiamondLossChoice.Colored(player.ColoredDiamonds.Order().First());
        throw new InvalidOperationException("The player has no diamond to lose.");
    }

    private TreasureLocation BestLocation(GameState state, TreasureLocation[] locations, Random random)
    {
        var scored = locations
            .Select(location => (Location: location, Score: ExpectedTargetValue(state.Board.TreasureDecks[location])))
            .ToArray();
        var best = scored.Max(item => item.Score);
        var ties = scored.Where(item => Math.Abs(item.Score - best) < 0.000001).Select(item => item.Location).ToArray();
        return ties[random.Next(ties.Length)];
    }

    private ToolType? BestPurchasableTool(GameState state, Player player)
    {
        var candidates = Enum.GetValues<ToolType>()
            .Where(tool => player.Money >= (tool == ToolType.Pickaxe ? 3 : 2))
            .Where(tool => state.Board.TreasureDecks.Any(pair => pair.Value.Count > 0 && GameRules.ToolFor(pair.Key) == tool))
            .ToArray();
        if (candidates.Length == 0) return null;
        if (Target == HoardingTarget.Pickaxe && candidates.Contains(ToolType.Pickaxe)) return ToolType.Pickaxe;
        if (Target == HoardingTarget.Shovel && candidates.Contains(ToolType.Shovel)) return ToolType.Shovel;

        return candidates
            .OrderByDescending(tool => state.Board.TreasureDecks
                .Where(pair => pair.Value.Count > 0 && GameRules.ToolFor(pair.Key) == tool)
                .Max(pair => ExpectedTargetValue(pair.Value)))
            .ThenBy(tool => tool == ToolType.Shovel ? 0 : 1)
            .First();
    }

    private double ExpectedTargetValue(CardDeck<TreasureCardType> deck)
    {
        if (deck.Count == 0) return double.NegativeInfinity;
        var value = Target switch
        {
            HoardingTarget.ClearDiamond => deck.CountOf(TreasureCardType.ClearDiamond),
            HoardingTarget.Gold => deck.CountOf(TreasureCardType.Gold),
            HoardingTarget.Pickaxe => deck.CountOf(TreasureCardType.Pickaxe),
            HoardingTarget.Shovel => deck.CountOf(TreasureCardType.Shovel),
            HoardingTarget.Money => Enum.GetValues<TreasureCardType>().Sum(card => deck.CountOf(card) * GameRules.MoneyValue(card)),
            _ => 0
        };
        return (double)value / deck.Count;
    }
}
