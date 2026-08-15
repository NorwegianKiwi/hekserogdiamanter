using HekserOgDiamanter.Configuration;
using HekserOgDiamanter.Domain;

namespace HekserOgDiamanter.Strategies;

public static class StrategyFactory
{
    public static IPlayerStrategy Create(StrategyConfig config) => config.Type switch
    {
        StrategyType.Random => new RandomStrategy(),
        StrategyType.ResourceHoarding => new ResourceHoardingStrategy(config.Target
            ?? throw new InvalidDataException("ResourceHoarding requires a target.")),
        _ => throw new ArgumentOutOfRangeException(nameof(config.Type))
    };
}
