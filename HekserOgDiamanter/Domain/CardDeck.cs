namespace HekserOgDiamanter.Domain;

public sealed class CardDeck<T> where T : struct, Enum
{
    private readonly List<T> _drawPile;
    private readonly List<T> _discardPile = [];

    public CardDeck(IEnumerable<T> cards) => _drawPile = [.. cards];

    public int Count => _drawPile.Count;
    public IReadOnlyList<T> Cards => _drawPile;
    public IReadOnlyList<T> DiscardedCards => _discardPile;

    public T Draw()
    {
        if (_drawPile.Count == 0) throw new InvalidOperationException("Cannot draw from an empty deck.");
        var card = _drawPile[0];
        _drawPile.RemoveAt(0);
        return card;
    }

    public void Discard(T card) => _discardPile.Add(card);

    public void ShuffleDiscardIntoDrawPile(Random random)
    {
        _drawPile.AddRange(_discardPile);
        _discardPile.Clear();
        Shuffle(_drawPile, random);
    }

    public int CountOf(T card) => _drawPile.Count(item => EqualityComparer<T>.Default.Equals(item, card));

    public static void Shuffle(List<T> cards, Random random)
    {
        for (var i = cards.Count - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (cards[i], cards[j]) = (cards[j], cards[i]);
        }
    }
}
