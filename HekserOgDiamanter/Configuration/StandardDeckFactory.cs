using HekserOgDiamanter.Domain;

namespace HekserOgDiamanter.Configuration;

public static class StandardDeckFactory
{
    public static IReadOnlyDictionary<TreasureLocation, List<TreasureCardType>> TreasureDecks() =>
        Enum.GetValues<TreasureLocation>().ToDictionary(location => location, BuildTreasureDeck);

    public static List<WitchCardType> WitchDeck() =>
        Repeat(WitchCardType.Lose, 6)
            .Concat(Repeat(WitchCardType.Win, 9))
            .Concat(Repeat(WitchCardType.Retry, 6))
            .Append(WitchCardType.Reshuffle)
            .ToList();

    public static BoardState CreateBoard(ScenarioConfig scenario, Random random)
    {
        var sourceDecks = scenario.DeckOrderMode == DeckOrderMode.Explicit
            ? scenario.ExplicitDeckOrder!.TreasureDecks.ToDictionary(pair => pair.Key, pair => pair.Value.ToList())
            : TreasureDecks().ToDictionary(pair => pair.Key, pair => pair.Value.ToList());
        var witchCards = scenario.DeckOrderMode == DeckOrderMode.Explicit
            ? scenario.ExplicitDeckOrder!.WitchDeck.ToList()
            : WitchDeck();

        foreach (var cards in sourceDecks.Values)
        {
            if (scenario.DeckOrderMode == DeckOrderMode.Shuffled)
                CardDeck<TreasureCardType>.Shuffle(cards, random);
            else if (scenario.DeckOrderMode == DeckOrderMode.ColoredDiamondLast)
            {
                cards.Remove(TreasureCardType.ColoredDiamond);
                CardDeck<TreasureCardType>.Shuffle(cards, random);
                cards.Add(TreasureCardType.ColoredDiamond);
            }
        }

        if (scenario.DeckOrderMode != DeckOrderMode.Explicit)
            CardDeck<WitchCardType>.Shuffle(witchCards, random);

        return new BoardState
        {
            TreasureDecks = sourceDecks.ToDictionary(pair => pair.Key, pair => new CardDeck<TreasureCardType>(pair.Value)),
            WitchDeck = new CardDeck<WitchCardType>(witchCards)
        };
    }

    public static Player CreatePlayer(PlayerConfig config)
    {
        var player = new Player(config.Name);
        var defaults = config.StartingPreset switch
        {
            StartingPreset.Standard => new StartingResourcesConfig { Money = 2, ClearDiamonds = 1 },
            StartingPreset.GoldVariant => new StartingResourcesConfig { Money = 8, Gold = 1 },
            _ => new StartingResourcesConfig()
        };
        var overrides = config.StartingResources;

        player.Money = overrides?.Money ?? defaults.Money ?? 0;
        player.ClearDiamonds = overrides?.ClearDiamonds ?? defaults.ClearDiamonds ?? 0;
        player.Gold = overrides?.Gold ?? defaults.Gold ?? 0;
        player.Pickaxes = overrides?.Pickaxes ?? defaults.Pickaxes ?? 0;
        player.Shovels = overrides?.Shovels ?? defaults.Shovels ?? 0;
        foreach (var color in overrides?.ColoredDiamonds ?? defaults.ColoredDiamonds ?? [])
            player.ColoredDiamonds.Add(color);
        return player;
    }

    public static void ValidateExplicitOrder(ExplicitDeckOrder? order, string scenarioName)
    {
        if (order is null) throw new InvalidDataException($"Scenario '{scenarioName}' requires ExplicitDeckOrder.");
        var expected = TreasureDecks();
        foreach (var location in Enum.GetValues<TreasureLocation>())
        {
            if (!order.TreasureDecks.TryGetValue(location, out var actual))
                throw new InvalidDataException($"Explicit order in '{scenarioName}' is missing {location}.");
            ValidateMultiset(expected[location], actual, $"{scenarioName}/{location}");
        }
        if (order.TreasureDecks.Count != expected.Count)
            throw new InvalidDataException($"Explicit order in '{scenarioName}' contains unknown treasure decks.");
        ValidateMultiset(WitchDeck(), order.WitchDeck, $"{scenarioName}/WitchDeck");
    }

    private static List<TreasureCardType> BuildTreasureDeck(TreasureLocation location)
    {
        var witchCount = GameRules.ToolFor(location) == ToolType.Pickaxe ? 2 : 3;
        var diamondCount = GameRules.ToolFor(location) == ToolType.Pickaxe ? 5 : 4;
        return Repeat(TreasureCardType.Gold, 3)
            .Concat(Repeat(TreasureCardType.Shovel, 2))
            .Concat(Repeat(TreasureCardType.Pickaxe, 2))
            .Concat(Repeat(TreasureCardType.Witch, witchCount))
            .Concat(Repeat(TreasureCardType.ClearDiamond, diamondCount))
            .Append(TreasureCardType.ColoredDiamond)
            .Concat([TreasureCardType.Money1, TreasureCardType.Money2, TreasureCardType.Money3, TreasureCardType.Money4, TreasureCardType.Money5])
            .ToList();
    }

    private static IEnumerable<T> Repeat<T>(T value, int count) => Enumerable.Repeat(value, count);

    private static void ValidateMultiset<T>(IEnumerable<T> expected, IEnumerable<T> actual, string name) where T : notnull
    {
        var expectedCounts = expected.GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count());
        var actualCounts = actual.GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count());
        if (expectedCounts.Count != actualCounts.Count || expectedCounts.Any(pair => !actualCounts.TryGetValue(pair.Key, out var count) || count != pair.Value))
            throw new InvalidDataException($"Explicit deck '{name}' does not match the standard card distribution.");
    }
}
