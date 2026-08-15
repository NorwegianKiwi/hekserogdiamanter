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
        string configDirectory)
    {
        var outputDirectory = Path.GetFullPath(config.OutputDirectory, configDirectory);
        Directory.CreateDirectory(outputDirectory);
        WriteConsole(config, results);
        WriteSummaryCsv(config, results, Path.Combine(outputDirectory, "summary.csv"));
        if (config.WriteDetailedCsv)
            WriteDetailedCsv(results, Path.Combine(outputDirectory, "games.csv"));
        Console.WriteLine($"\nCSV written to: {outputDirectory}");
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
                var recommended = Recommended(metric, "AllPlayers", stats.P999);
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
        writer.WriteLine("Scenario,Scope,Resource,CompletedGames,TruncatedGames,Mean,P95,P99,P99.9,Maximum,Recommended,Baseline,Saving");
        foreach (var result in results)
        {
            var completed = result.Games.Where(game => game.Completed).ToArray();
            if (completed.Length == 0) continue;
            foreach (var metric in Enum.GetValues<ResourceMetric>())
            {
                WriteSummaryRow(writer, config, result, completed, metric, "AllPlayers", game => game.PeakTotal[metric]);
                WriteSummaryRow(writer, config, result, completed, metric, "SinglePlayer", game => game.PeakSinglePlayer[metric]);
                foreach (var player in result.Scenario.Players)
                    WriteSummaryRow(writer, config, result, completed, metric, $"Player:{player.Name}", game => game.PeakByPlayer[player.Name][metric]);
            }
        }
    }

    private static void WriteSummaryRow(
        TextWriter writer,
        SimulationConfig config,
        ScenarioRunResult result,
        IReadOnlyCollection<GameResult> completed,
        ResourceMetric metric,
        string scope,
        Func<GameResult, int> selector)
    {
        var stats = Statistics.Calculate(completed.Select(selector));
        var baseline = scope == "AllPlayers" ? config.BaselineInventory.For(metric) : 0;
        var recommended = Recommended(metric, scope, stats.P999);
        writer.WriteLine(string.Join(',',
            Csv(result.Scenario.Name), Csv(scope), metric, completed.Count, result.Games.Count - completed.Count,
            stats.Mean.ToString("F3", CultureInfo.InvariantCulture), stats.P95, stats.P99, stats.P999,
            stats.Maximum, recommended, baseline == 0 ? "" : baseline,
            baseline == 0 ? "" : baseline - recommended));
    }

    private static void WriteDetailedCsv(IEnumerable<ScenarioRunResult> results, string path)
    {
        using var writer = new StreamWriter(path, false, new UTF8Encoding(true));
        var metrics = Enum.GetValues<ResourceMetric>();
        var resourceHeaders = metrics.Select(metric => $"PeakTotal_{metric}")
            .Concat(metrics.Select(metric => $"PeakSingle_{metric}"))
            .Concat(metrics.Select(metric => $"Distributed_{metric}"));
        writer.WriteLine("Scenario,Run,Seed,Completed,EndReason,Turns,Rounds,Winners,PlayerScores,PlayerPeaks," + string.Join(',', resourceHeaders));

        foreach (var result in results)
            foreach (var game in result.Games)
            {
                var scores = string.Join(';', game.Players.Select(player => $"{player.Name}:{player.Score}"));
                var playerPeaks = string.Join(';', game.PeakByPlayer.Select(player =>
                    $"{player.Key}[{string.Join('|', player.Value.OrderBy(pair => pair.Key).Select(pair => $"{pair.Key}:{pair.Value}"))}]"));
                var values = metrics.Select(metric => game.PeakTotal[metric])
                    .Concat(metrics.Select(metric => game.PeakSinglePlayer[metric]))
                    .Concat(metrics.Select(metric => game.DistributedFromBoard[metric]));
                writer.WriteLine(string.Join(',',
                Csv(result.Scenario.Name), game.RunNumber, game.Seed, game.Completed, game.EndReason,
                game.Turns, game.Rounds, Csv(string.Join(';', game.Winners)), Csv(scores), Csv(playerPeaks), string.Join(',', values)));
            }
    }

    private static string Csv(string value) => $"\"{value.Replace("\"", "\"\"")}\"";

    private static int Recommended(ResourceMetric metric, string scope, int percentile) =>
        scope == "AllPlayers" && metric == ResourceMetric.ColoredDiamond ? 6 : percentile;
}
