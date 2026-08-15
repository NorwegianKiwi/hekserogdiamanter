using HekserOgDiamanter.Domain;

namespace HekserOgDiamanter.Strategies;

public interface IPlayerStrategy
{
    TurnDecision ChooseTurn(GameState state, Player player, Random random);
    bool ShouldRetryWitch(GameState state, Player player, Random random);
    DiamondLossChoice ChooseDiamondToLose(GameState state, Player player, Random random);
}

public readonly record struct DiamondLossChoice(bool IsClear, DiamondColor? Color)
{
    public static DiamondLossChoice Clear => new(true, null);
    public static DiamondLossChoice Colored(DiamondColor color) => new(false, color);
}
