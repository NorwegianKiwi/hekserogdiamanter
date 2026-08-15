using System.Text.Json.Serialization;
using HekserOgDiamanter.Domain;

namespace HekserOgDiamanter.Configuration;

public sealed class SimulationConfig
{
    public int Seed { get; set; } = 20260815;
    public string OutputDirectory { get; set; } = "simulation-results";
    public int MaxDegreeOfParallelism { get; set; } = 0;
    public bool WriteDetailedCsv { get; set; }
    public ComponentBaseline BaselineInventory { get; set; } = new();
    public List<ScenarioConfig> Scenarios { get; set; } = [];
}

public sealed class ComponentBaseline
{
    public int ClearDiamonds { get; set; } = 32;
    public int Gold { get; set; } = 20;
    public int Pickaxes { get; set; } = 40;
    public int Shovels { get; set; } = 40;
    public int Coin1 { get; set; } = 40;
    public int Coin5 { get; set; } = 20;
    public int Coin10 { get; set; } = 20;

    public int For(ResourceMetric resource) => resource switch
    {
        ResourceMetric.ClearDiamond => ClearDiamonds,
        ResourceMetric.ColoredDiamond => 6,
        ResourceMetric.Gold => Gold,
        ResourceMetric.Pickaxe => Pickaxes,
        ResourceMetric.Shovel => Shovels,
        ResourceMetric.Coin1 => Coin1,
        ResourceMetric.Coin5 => Coin5,
        ResourceMetric.Coin10 => Coin10,
        _ => 0
    };
}

public sealed class ScenarioConfig
{
    public string Name { get; set; } = "Scenario";
    public int Games { get; set; } = 10_000;
    public int MaxTurns { get; set; } = 10_000;
    public bool TraceTurns { get; set; }
    public int StartingPlayerIndex { get; set; }
    public DeckOrderMode DeckOrderMode { get; set; } = DeckOrderMode.Shuffled;
    public ExplicitDeckOrder? ExplicitDeckOrder { get; set; }
    public List<PlayerConfig> Players { get; set; } = [];
}

public sealed class PlayerConfig
{
    public string Name { get; set; } = "Player";
    public StartingPreset StartingPreset { get; set; } = StartingPreset.Standard;
    public StartingResourcesConfig? StartingResources { get; set; }
    public StrategyConfig Strategy { get; set; } = new();
}

public enum StartingPreset { Standard, GoldVariant, Custom }

public sealed class StartingResourcesConfig
{
    public int? Money { get; set; }
    public int? ClearDiamonds { get; set; }
    public int? Gold { get; set; }
    public int? Pickaxes { get; set; }
    public int? Shovels { get; set; }
    public List<DiamondColor>? ColoredDiamonds { get; set; }
}

public sealed class StrategyConfig
{
    public StrategyType Type { get; set; } = StrategyType.Random;
    public HoardingTarget? Target { get; set; }
}

public sealed class ExplicitDeckOrder
{
    public Dictionary<TreasureLocation, List<TreasureCardType>> TreasureDecks { get; set; } = [];
    public List<WitchCardType> WitchDeck { get; set; } = [];
}

[JsonSerializable(typeof(SimulationConfig))]
[JsonSourceGenerationOptions(WriteIndented = true, UseStringEnumConverter = true)]
internal partial class SimulationJsonContext : JsonSerializerContext;
