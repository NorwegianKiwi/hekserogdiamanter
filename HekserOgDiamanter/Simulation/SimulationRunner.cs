using HekserOgDiamanter.Configuration;

namespace HekserOgDiamanter.Simulation;

public sealed record ScenarioRunResult(ScenarioConfig Scenario, IReadOnlyList<GameResult> Games);

public static class SimulationRunner
{
    public static IReadOnlyList<ScenarioRunResult> Run(SimulationConfig config)
    {
        var results = new List<ScenarioRunResult>(config.Scenarios.Count);
        for (var scenarioIndex = 0; scenarioIndex < config.Scenarios.Count; scenarioIndex++)
        {
            var scenario = config.Scenarios[scenarioIndex];
            var games = new GameResult[scenario.Games];
            var currentScenarioIndex = scenarioIndex;

            if (scenario.TraceTurns)
            {
                for (var run = 0; run < scenario.Games; run++)
                {
                    var runNumber = run + 1;
                    Console.WriteLine($"\n--- {scenario.Name}, game {runNumber} ---");
                    games[run] = new Game(scenario, DeriveSeed(config.Seed, currentScenarioIndex, run), Console.WriteLine).Run(runNumber);
                }
            }
            else
            {
                var options = new ParallelOptions
                {
                    MaxDegreeOfParallelism = config.MaxDegreeOfParallelism == 0
                        ? Environment.ProcessorCount
                        : config.MaxDegreeOfParallelism
                };
                Parallel.For(0, scenario.Games, options, run =>
                {
                    games[run] = new Game(scenario, DeriveSeed(config.Seed, currentScenarioIndex, run)).Run(run + 1);
                });
            }

            results.Add(new ScenarioRunResult(scenario, games));
        }
        return results;
    }

    public static int DeriveSeed(int baseSeed, int scenarioIndex, int runIndex)
    {
        unchecked
        {
            var seed = baseSeed;
            seed = seed * 397 ^ scenarioIndex * 1_000_003;
            seed = seed * 397 ^ runIndex * 7_919;
            return seed;
        }
    }
}
