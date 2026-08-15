using System.Globalization;
using System.Text;
using HekserOgDiamanter.Configuration;
using HekserOgDiamanter.Domain;
using HekserOgDiamanter.Simulation;

namespace HekserOgDiamanter.Reporting;

public static class ReportWriter
{
    public static void Write(
        SimulationConfig config,
        IReadOnlyList<ScenarioRunResult> results,
        string workingDirectory)
    {
        var outputDirectory = Path.GetFullPath(config.OutputDirectory, workingDirectory);
        Directory.CreateDirectory(outputDirectory);
        var timestamp = DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture);
        var summaryPath = Path.Combine(outputDirectory, $"summary-{timestamp}.csv");
        WriteConsole(config, results);
        WriteSummaryCsv(config, results, summaryPath);
        Console.WriteLine($"\nSummary CSV written to: {summaryPath}");
        if (config.WriteDetailedCsv)
        {
            var detailsPath = Path.Combine(outputDirectory, $"games-{timestamp}.csv");
            WriteDetailedCsv(results, detailsPath);
            Console.WriteLine($"Detailed CSV written to: {detailsPath}");
        }
    }

    private static void WriteConsole(SimulationConfig config, IEnumerable<ScenarioRunResult> results)
    {
        Console.WriteLine("\nHekser & Diamanter – simulation results");
        foreach (var result in results)
        {
            var completed = result.Games.Where(game => game.Completed).ToArray();
            var truncated = result.Games.Count - completed.Length;
            Console.WriteLine($"\n{result.Scenario.Name}: {completed.Length:N0} completed, {truncated:N0} stopped by turn limit");
            if (completed.Length == 0)
            {
                Console.WriteLine("  No completed games; no recommendation can be calculated.");
                continue;
            }

            Console.WriteLine($"  Turns: average {completed.Average(game => game.Turns):F1}, max {completed.Max(game => game.Turns):N0}");
            Console.WriteLine("  Resource             P95   P99  P99.9    Max  Recommend  Current  Saving");
            foreach (var metric in Enum.GetValues<ResourceMetric>())
            {
                var stats = Statistics.Calculate(completed.Select(game => game.PeakTotal[metric]));
                var baseline = config.BaselineInventory.For(metric);
                var recommended = Recommended(metric, stats.P999);
                var current = baseline == 0 ? "-" : baseline.ToString(CultureInfo.InvariantCulture);
                var saving = baseline == 0 ? "-" : (baseline - recommended).ToString(CultureInfo.InvariantCulture);
                Console.WriteLine($"  {metric,-20} {stats.P95,4} {stats.P99,5} {stats.P999,6} {stats.Maximum,6} {recommended,10} {current,8} {saving,7}");
            }
            if (completed.Length < 10_000)
                Console.WriteLine("  Warning: fewer than 10,000 completed games makes the 99.9 percentile less stable.");
        }
    }

    private static void WriteSummaryCsv(SimulationConfig config, IEnumerable<ScenarioRunResult> results, string path)
    {
        using var writer = new StreamWriter(path, false, new UTF8Encoding(true));
        writer.WriteLine("Scenario,Resource,CompletedGames,TruncatedGames,AllPlayersMean,AllPlayersP95,AllPlayersP99,AllPlayersP99.9,AllPlayersMaximum,SinglePlayerP99.9,SinglePlayerMaximum,Recommended,Baseline,Saving");
        foreach (var result in results)
        {
            var completed = result.Games.Where(game => game.Completed).ToArray();
            foreach (var metric in Enum.GetValues<ResourceMetric>())
            {
                if (completed.Length == 0)
                {
                    var emptyBaseline = config.BaselineInventory.For(metric);
                    var emptyRecommended = metric == ResourceMetric.ColoredDiamond ? "6" : "";
                    var emptySaving = metric == ResourceMetric.ColoredDiamond ? "0" : "";
                    writer.WriteLine(string.Join(',',
                        Csv(result.Scenario.Name), metric, 0, result.Games.Count,
                        "", "", "", "", "", "", "", emptyRecommended,
                        emptyBaseline == 0 ? "" : emptyBaseline, emptySaving));
                    continue;
                }

                var allPlayers = Statistics.Calculate(completed.Select(game => game.PeakTotal[metric]));
                var singlePlayer = Statistics.Calculate(completed.Select(game => game.PeakSinglePlayer[metric]));
                var baseline = config.BaselineInventory.For(metric);
                var recommended = Recommended(metric, allPlayers.P999);
                writer.WriteLine(string.Join(',',
                    Csv(result.Scenario.Name), metric, completed.Length, result.Games.Count - completed.Length,
                    allPlayers.Mean.ToString("F3", CultureInfo.InvariantCulture), allPlayers.P95,
                    allPlayers.P99, allPlayers.P999, allPlayers.Maximum,
                    singlePlayer.P999, singlePlayer.Maximum, recommended,
                    baseline == 0 ? "" : baseline,
                    baseline == 0 ? "" : baseline - recommended));
            }
        }
    }

    private static void WriteDetailedCsv(IEnumerable<ScenarioRunResult> results, string path)
    {
        using var writer = new StreamWriter(path, false, new UTF8Encoding(true));
        var metrics = Enum.GetValues<ResourceMetric>();
        var resourceHeaders = metrics.Select(metric => $"PeakTotal_{metric}")
            .Concat(metrics.Select(metric => $"PeakSingle_{metric}"))
            .Concat(metrics.Select(metric => $"Distributed_{metric}"));
        writer.WriteLine("Scenario,Run,Seed,Completed,EndReason,Turns,Rounds,Winners,PlayerScores," + string.Join(',', resourceHeaders));

        foreach (var result in results)
            foreach (var game in result.Games)
            {
                var scores = string.Join(';', game.Players.Select(player => $"{player.Name}:{player.Score}"));
                var values = metrics.Select(metric => game.PeakTotal[metric])
                    .Concat(metrics.Select(metric => game.PeakSinglePlayer[metric]))
                    .Concat(metrics.Select(metric => game.DistributedFromBoard[metric]));
                writer.WriteLine(string.Join(',',
                Csv(result.Scenario.Name), game.RunNumber, game.Seed, game.Completed, game.EndReason,
                game.Turns, game.Rounds, Csv(string.Join(';', game.Winners)), Csv(scores), string.Join(',', values)));
            }
    }

    private static string Csv(string value) => $"\"{value.Replace("\"", "\"\"")}\"";

    private static int Recommended(ResourceMetric metric, int percentile) =>
        metric == ResourceMetric.ColoredDiamond ? 6 : percentile;
}
