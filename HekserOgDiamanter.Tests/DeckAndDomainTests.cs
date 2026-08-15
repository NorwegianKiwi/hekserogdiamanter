using HekserOgDiamanter.Configuration;
using HekserOgDiamanter.Domain;
using HekserOgDiamanter.Reporting;
using HekserOgDiamanter.Simulation;

namespace HekserOgDiamanter.Tests;

public sealed class DeckAndDomainTests
{
    [Fact]
    public void StandardDecksHaveExactCurrentDistribution()
    {
        var decks = StandardDeckFactory.TreasureDecks();
        Assert.Equal(6, decks.Count);

        foreach (var (location, cards) in decks)
        {
            var pickaxeDeck = GameRules.ToolFor(location) == ToolType.Pickaxe;
            Assert.Equal(20, cards.Count);
            Assert.Equal(3, cards.Count(card => card == TreasureCardType.Gold));
            Assert.Equal(2, cards.Count(card => card == TreasureCardType.Pickaxe));
            Assert.Equal(2, cards.Count(card => card == TreasureCardType.Shovel));
            Assert.Equal(pickaxeDeck ? 2 : 3, cards.Count(card => card == TreasureCardType.Witch));
            Assert.Equal(pickaxeDeck ? 5 : 4, cards.Count(card => card == TreasureCardType.ClearDiamond));
            Assert.Single(cards, card => card == TreasureCardType.ColoredDiamond);
            Assert.Equal(5, cards.Count(card => GameRules.MoneyValue(card) > 0));
            Assert.Equal(15, cards.Sum(GameRules.MoneyValue));
        }

        var witches = StandardDeckFactory.WitchDeck();
        Assert.Equal(22, witches.Count);
        Assert.Equal(6, witches.Count(card => card == WitchCardType.Lose));
        Assert.Equal(9, witches.Count(card => card == WitchCardType.Win));
        Assert.Equal(6, witches.Count(card => card == WitchCardType.Retry));
        Assert.Single(witches, card => card == WitchCardType.Reshuffle);
    }

    [Fact]
    public void PlayerScoreIncludesResourcesColoredValuesAndCompleteSets()
    {
        var player = new Player("Test") { ClearDiamonds = 2, Gold = 3 };
        player.ColoredDiamonds.UnionWith([DiamondColor.Blue, DiamondColor.Green]);
        foreach (var location in Enum.GetValues<TreasureLocation>()) player.CollectedCards[location] = 2;

        Assert.Equal(10 + 6 + 20 + 10 + 12, player.Score());
    }

    [Theory]
    [InlineData(0, 0, 0, 0)]
    [InlineData(4, 4, 0, 0)]
    [InlineData(5, 0, 1, 0)]
    [InlineData(19, 4, 1, 1)]
    [InlineData(27, 2, 1, 2)]
    public void MoneyUsesCanonicalCoinWallet(int amount, int ones, int fives, int tens)
    {
        Assert.Equal(new CoinWallet(ones, fives, tens), CoinWallet.FromAmount(amount));
    }

    [Fact]
    public void NearestRankUsesIntegerEmpiricalPercentiles()
    {
        var values = Enumerable.Range(1, 1000).ToArray();
        Assert.Equal(950, Statistics.NearestRank(values, .95));
        Assert.Equal(990, Statistics.NearestRank(values, .99));
        Assert.Equal(999, Statistics.NearestRank(values, .999));
    }

    [Fact]
    public void ReshuffleReturnsAllDiscardedWitchCardsToDrawPile()
    {
        var deck = new CardDeck<WitchCardType>([WitchCardType.Win, WitchCardType.Lose]);
        deck.Discard(deck.Draw());
        deck.ShuffleDiscardIntoDrawPile(new Random(1));

        Assert.Equal(2, deck.Count);
        Assert.Empty(deck.DiscardedCards);
        Assert.Equal(1, deck.CountOf(WitchCardType.Win));
        Assert.Equal(1, deck.CountOf(WitchCardType.Lose));
    }

    [Fact]
    public void ExplicitOrderMustContainTheExactStandardMultisets()
    {
        var valid = new ExplicitDeckOrder
        {
            TreasureDecks = StandardDeckFactory.TreasureDecks().ToDictionary(pair => pair.Key, pair => pair.Value.ToList()),
            WitchDeck = StandardDeckFactory.WitchDeck()
        };
        StandardDeckFactory.ValidateExplicitOrder(valid, "valid");

        valid.TreasureDecks[TreasureLocation.Ruins].RemoveAt(0);
        Assert.Throws<InvalidDataException>(() => StandardDeckFactory.ValidateExplicitOrder(valid, "invalid"));
    }
}
