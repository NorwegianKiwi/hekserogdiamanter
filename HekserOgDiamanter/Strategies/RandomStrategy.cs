using HekserOgDiamanter.Domain;

namespace HekserOgDiamanter.Strategies;

public sealed class RandomStrategy : IPlayerStrategy
{
    public TurnDecision ChooseTurn(GameState state, Player player, Random random)
    {
        var actions = LegalActionTypes(state, player);
        return actions[random.Next(actions.Count)] switch
        {
            TurnActionType.Search => ChooseSearch(state, player, random),
            TurnActionType.Sell => ChooseSale(player, random),
            TurnActionType.Buy => ChoosePurchase(player, random),
            _ => new TurnDecision(TurnActionType.PoorRelief)
        };
    }

    public bool ShouldRetryWitch(GameState state, Player player, Random random) =>
        player.Money >= 2 && random.Next(2) == 0;

    public DiamondLossChoice ChooseDiamondToLose(GameState state, Player player, Random random)
    {
        var choices = new List<DiamondLossChoice>();
        if (player.ClearDiamonds > 0) choices.Add(DiamondLossChoice.Clear);
        choices.AddRange(player.ColoredDiamonds.Select(DiamondLossChoice.Colored));
        if (choices.Count == 0) throw new InvalidOperationException("The player has no diamond to lose.");
        return choices[random.Next(choices.Count)];
    }

    public static List<TurnActionType> LegalActionTypes(GameState state, Player player)
    {
        var actions = new List<TurnActionType> { TurnActionType.PoorRelief };
        if (state.SearchableLocations(player).Any()) actions.Add(TurnActionType.Search);
        if (player.ClearDiamonds > 0 || player.Gold > 0) actions.Add(TurnActionType.Sell);
        if (player.Money >= 2) actions.Add(TurnActionType.Buy);
        return actions;
    }

    private static TurnDecision ChooseSearch(GameState state, Player player, Random random)
    {
        var locations = state.SearchableLocations(player).ToArray();
        return new TurnDecision(TurnActionType.Search, locations[random.Next(locations.Length)]);
    }

    private static TurnDecision ChooseSale(Player player, Random random)
    {
        var combinations = (
            from clear in Enumerable.Range(0, player.ClearDiamonds + 1)
            from gold in Enumerable.Range(0, player.Gold + 1)
            where clear + gold > 0
            select new TurnDecision(TurnActionType.Sell, SellClearDiamonds: clear, SellGold: gold)).ToArray();
        return combinations[random.Next(combinations.Length)];
    }

    private static TurnDecision ChoosePurchase(Player player, Random random)
    {
        var combinations = new List<TurnDecision>();
        for (var picks = 0; picks <= player.Money / 3; picks++)
            for (var shovels = 0; shovels <= (player.Money - picks * 3) / 2; shovels++)
                if (picks + shovels > 0)
                    combinations.Add(new TurnDecision(TurnActionType.Buy, BuyPickaxes: picks, BuyShovels: shovels));
        return combinations[random.Next(combinations.Count)];
    }
}
