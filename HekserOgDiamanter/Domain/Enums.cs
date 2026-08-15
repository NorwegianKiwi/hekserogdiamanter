namespace HekserOgDiamanter.Domain;

public enum TreasureLocation
{
    Ruins,
    Mine,
    Lava,
    Forest,
    Field,
    Beach
}

public enum ToolType { Pickaxe, Shovel }

public enum DiamondColor { Blue, Purple, Red, Green, Orange, Yellow }

public enum TreasureCardType
{
    Gold,
    Shovel,
    Pickaxe,
    Witch,
    ClearDiamond,
    ColoredDiamond,
    Money1,
    Money2,
    Money3,
    Money4,
    Money5
}

public enum WitchCardType { Lose, Win, Retry, Reshuffle }

public enum TurnActionType { Search, Sell, Buy, PoorRelief }

public enum ResourceMetric
{
    ClearDiamond,
    ColoredDiamond,
    Gold,
    Pickaxe,
    Shovel,
    Money,
    Coin1,
    Coin5,
    Coin10
}

public enum StrategyType { Random, ResourceHoarding }

public enum HoardingTarget { ClearDiamond, Gold, Pickaxe, Shovel, Money }

public enum DeckOrderMode { Shuffled, ColoredDiamondLast, Explicit }

public enum GameEndReason { EmptyTreasureDeck, AllColoredDiamondsFound, TurnLimit }

public static class GameRules
{
    public static ToolType ToolFor(TreasureLocation location) => location switch
    {
        TreasureLocation.Ruins or TreasureLocation.Mine or TreasureLocation.Lava => ToolType.Pickaxe,
        _ => ToolType.Shovel
    };

    public static DiamondColor ColorFor(TreasureLocation location) => location switch
    {
        TreasureLocation.Ruins => DiamondColor.Blue,
        TreasureLocation.Mine => DiamondColor.Purple,
        TreasureLocation.Lava => DiamondColor.Red,
        TreasureLocation.Forest => DiamondColor.Green,
        TreasureLocation.Field => DiamondColor.Orange,
        TreasureLocation.Beach => DiamondColor.Yellow,
        _ => throw new ArgumentOutOfRangeException(nameof(location))
    };

    public static int ColoredDiamondPoints(DiamondColor color) => color switch
    {
        DiamondColor.Blue or DiamondColor.Purple or DiamondColor.Red => 20,
        _ => 10
    };

    public static int MoneyValue(TreasureCardType card) => card switch
    {
        TreasureCardType.Money1 => 1,
        TreasureCardType.Money2 => 2,
        TreasureCardType.Money3 => 3,
        TreasureCardType.Money4 => 4,
        TreasureCardType.Money5 => 5,
        _ => 0
    };
}
