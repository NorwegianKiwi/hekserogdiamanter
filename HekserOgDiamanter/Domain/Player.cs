namespace HekserOgDiamanter.Domain;

public sealed class Player
{
    public Player(string name) => Name = name;

    public string Name { get; }
    public int Money { get; set; }
    public int ClearDiamonds { get; set; }
    public int Gold { get; set; }
    public int Pickaxes { get; set; }
    public int Shovels { get; set; }
    public HashSet<DiamondColor> ColoredDiamonds { get; } = [];
    public Dictionary<TreasureLocation, int> CollectedCards { get; } =
        Enum.GetValues<TreasureLocation>().ToDictionary(location => location, _ => 0);

    public int ToolCount(ToolType tool) => tool == ToolType.Pickaxe ? Pickaxes : Shovels;

    public void AddTool(ToolType tool, int count = 1)
    {
        if (tool == ToolType.Pickaxe) Pickaxes += count;
        else Shovels += count;
    }

    public void RemoveTool(ToolType tool)
    {
        if (ToolCount(tool) <= 0) throw new InvalidOperationException($"{Name} has no {tool}.");
        AddTool(tool, -1);
    }

    public int Score()
    {
        var coloredPoints = ColoredDiamonds.Sum(GameRules.ColoredDiamondPoints);
        var completedSets = CollectedCards.Values.Min();
        return ClearDiamonds * 5 + Gold * 2 + coloredPoints + completedSets * 6;
    }

    public CoinWallet GetCoinWallet() => Domain.CoinWallet.FromAmount(Money);
}

public readonly record struct CoinWallet(int Ones, int Fives, int Tens)
{
    public static CoinWallet FromAmount(int amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        var tens = amount / 10;
        var remainder = amount % 10;
        return new CoinWallet(remainder % 5, remainder / 5, tens);
    }
}
