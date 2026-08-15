using HekserOgDiamanter.Simulation;

namespace HekserOgDiamanter.Domain;

public sealed class BoardState
{
    public required Dictionary<TreasureLocation, CardDeck<TreasureCardType>> TreasureDecks { get; init; }
    public required CardDeck<WitchCardType> WitchDeck { get; init; }
}

public sealed class GameState
{
    public required BoardState Board { get; init; }
    public required IReadOnlyList<Player> Players { get; init; }
    public required ResourceTracker Resources { get; init; }
    public HashSet<DiamondColor> FoundColoredDiamonds { get; } = [];
    public int CurrentPlayerIndex { get; internal set; }
    public int Turns { get; internal set; }
    public int Rounds { get; internal set; }
    public GameEndReason? PendingEndReason { get; internal set; }

    public Player CurrentPlayer => Players[CurrentPlayerIndex];

    public IEnumerable<TreasureLocation> SearchableLocations(Player player) =>
        Board.TreasureDecks
            .Where(pair => pair.Value.Count > 0 && player.ToolCount(GameRules.ToolFor(pair.Key)) > 0)
            .Select(pair => pair.Key);
}

public sealed record TurnDecision(
    TurnActionType Action,
    TreasureLocation? Location = null,
    int SellClearDiamonds = 0,
    int SellGold = 0,
    int BuyPickaxes = 0,
    int BuyShovels = 0);
