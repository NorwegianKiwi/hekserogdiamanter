using System.Text.Json;
using System.Text.Json.Serialization;
using HekserOgDiamanter.Domain;

namespace HekserOgDiamanter.Configuration;

public static class ConfigLoader
{
    public static SimulationConfig Load(string path)
    {
        if (!File.Exists(path)) throw new FileNotFoundException("Configuration file was not found.", path);

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
        options.Converters.Add(new JsonStringEnumConverter());

        var config = JsonSerializer.Deserialize<SimulationConfig>(File.ReadAllText(path), options)
            ?? throw new InvalidDataException("Configuration file is empty.");
        Validate(config);
        return config;
    }

    public static void Validate(SimulationConfig config)
    {
        if (config.Scenarios.Count == 0) throw new InvalidDataException("At least one scenario is required.");
        if (config.MaxDegreeOfParallelism < 0) throw new InvalidDataException("MaxDegreeOfParallelism cannot be negative.");

        var duplicateNames = config.Scenarios.GroupBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateNames is not null) throw new InvalidDataException($"Duplicate scenario name: {duplicateNames.Key}.");

        foreach (var scenario in config.Scenarios)
        {
            if (string.IsNullOrWhiteSpace(scenario.Name)) throw new InvalidDataException("Scenario names cannot be empty.");
            if (scenario.Games <= 0) throw new InvalidDataException($"Scenario '{scenario.Name}' must run at least one game.");
            if (scenario.MaxTurns <= 0) throw new InvalidDataException($"Scenario '{scenario.Name}' must have a positive turn limit.");
            if (scenario.Players.Count is < 2 or > 4) throw new InvalidDataException($"Scenario '{scenario.Name}' must have 2-4 players.");
            if (scenario.StartingPlayerIndex < 0 || scenario.StartingPlayerIndex >= scenario.Players.Count)
                throw new InvalidDataException($"Scenario '{scenario.Name}' has an invalid StartingPlayerIndex.");
            if (scenario.Players.Select(p => p.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() != scenario.Players.Count)
                throw new InvalidDataException($"Scenario '{scenario.Name}' has duplicate player names.");

            foreach (var player in scenario.Players)
            {
                if (player.Strategy.Type == StrategyType.ResourceHoarding && player.Strategy.Target is null)
                    throw new InvalidDataException($"Player '{player.Name}' needs a hoarding target.");
                if (player.StartingPreset == StartingPreset.Custom && player.StartingResources is null)
                    throw new InvalidDataException($"Player '{player.Name}' uses Custom starting resources without values.");
                ValidateNonNegative(player);
            }

            var startingColors = scenario.Players
                .SelectMany(player => player.StartingResources?.ColoredDiamonds ?? [])
                .ToArray();
            if (startingColors.Distinct().Count() != startingColors.Length)
                throw new InvalidDataException($"Scenario '{scenario.Name}' assigns the same colored diamond to multiple players.");

            if (scenario.DeckOrderMode == DeckOrderMode.Explicit)
                StandardDeckFactory.ValidateExplicitOrder(scenario.ExplicitDeckOrder, scenario.Name);
        }
    }

    private static void ValidateNonNegative(PlayerConfig player)
    {
        var resources = player.StartingResources;
        if (resources is null) return;
        var values = new[] { resources.Money, resources.ClearDiamonds, resources.Gold, resources.Pickaxes, resources.Shovels };
        if (values.Any(value => value < 0)) throw new InvalidDataException($"Player '{player.Name}' has negative starting resources.");
        if (resources.ColoredDiamonds?.Distinct().Count() != resources.ColoredDiamonds?.Count)
            throw new InvalidDataException($"Player '{player.Name}' has duplicate colored diamonds.");
    }
}
