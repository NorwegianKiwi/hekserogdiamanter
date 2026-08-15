using HekserOgDiamanter.Configuration;
using HekserOgDiamanter.Domain;
using HekserOgDiamanter.Reporting;
using HekserOgDiamanter.Simulation;

namespace HekserOgDiamanter.Tests;

public sealed class ConfigurationAndReportingTests
{
    private static readonly string[] ExpectedConfigFiles =
    [
        "example.json",
        "random.json",
        "stress-clear-diamond.json",
        "stress-gold.json",
        "stress-money.json",
        "stress-pickaxe.json",
        "stress-shovel.json"
    ];

    [Fact]
    public void BundledConfigurationsAreDiscoveredInNameOrder()
    {
        var files = ConfigFileSelector.DiscoverBundled(AppContext.BaseDirectory, Directory.GetCurrentDirectory());

        Assert.Equal(ExpectedConfigFiles, files.Select(Path.GetFileName));
    }

    [Fact]
    public void InteractiveSelectorAcceptsAValidNumber()
    {
        var files = new[] { "/configs/a.json", "/configs/b.json" };
        var output = new StringWriter();

        var selected = ConfigFileSelector.SelectInteractive(files, new StringReader("2\n"), output);

        Assert.Equal(files[1], selected);
        Assert.Contains("1. a.json", output.ToString());
        Assert.Contains("2. b.json", output.ToString());
    }

    [Fact]
    public void InteractiveSelectorExplainsInvalidInputAndTriesAgain()
    {
        var files = new[] { "/configs/a.json", "/configs/b.json" };
        var output = new StringWriter();

        var selected = ConfigFileSelector.SelectInteractive(files, new StringReader("wrong\n3\n1\n"), output);

        Assert.Equal(files[0], selected);
        Assert.Equal(2, CountOccurrences(output.ToString(), "Enter a number from 1 to 2."));
    }

    [Fact]
    public void DirectConfigArgumentBypassesInteractiveSelection()
    {
        var startup = StartupOptions.Parse(["--config", "custom.json"]);

        Assert.Equal(StartupMode.DirectConfig, startup.Mode);
        Assert.Equal("custom.json", startup.ConfigPath);
    }

    [Fact]
    public void AllBundledConfigurationsLoadAndUseUniqueOutputDirectories()
    {
        var configs = ConfigFiles().Select(path => (Path: path, Config: ConfigLoader.Load(path))).ToArray();

        Assert.Equal(7, configs.Length);
        Assert.Equal(7, configs.Select(item => item.Config.OutputDirectory).Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Theory]
    [InlineData("stress-clear-diamond.json", HoardingTarget.ClearDiamond)]
    [InlineData("stress-gold.json", HoardingTarget.Gold)]
    [InlineData("stress-pickaxe.json", HoardingTarget.Pickaxe)]
    [InlineData("stress-shovel.json", HoardingTarget.Shovel)]
    [InlineData("stress-money.json", HoardingTarget.Money)]
    public void StressProfilesContainTwoThreeAndFourPlayerScenarios(string fileName, HoardingTarget target)
    {
        var config = ConfigLoader.Load(ConfigFiles().Single(path => Path.GetFileName(path) == fileName));

        Assert.Equal([2, 3, 4], config.Scenarios.Select(scenario => scenario.Players.Count));
        Assert.All(config.Scenarios, scenario =>
        {
            Assert.Equal(DeckOrderMode.ColoredDiamondLast, scenario.DeckOrderMode);
            Assert.All(scenario.Players, player =>
            {
                Assert.Equal(StartingPreset.Standard, player.StartingPreset);
                Assert.Equal(StrategyType.ResourceHoarding, player.Strategy.Type);
                Assert.Equal(target, player.Strategy.Target);
            });
        });
    }

    [Fact]
    public void RandomProfileContainsComparableScenarios()
    {
        var config = ConfigLoader.Load(ConfigFiles().Single(path => Path.GetFileName(path) == "random.json"));

        Assert.Equal([2, 3, 4], config.Scenarios.Select(scenario => scenario.Players.Count));
        Assert.All(config.Scenarios, scenario =>
        {
            Assert.Equal(DeckOrderMode.Shuffled, scenario.DeckOrderMode);
            Assert.All(scenario.Players, player => Assert.Equal(StrategyType.Random, player.Strategy.Type));
        });
    }

    [Fact]
    public void SummaryHasOneRowPerScenarioAndResourceAndDetailsHaveNoNamedPeaks()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"witches-report-{Guid.NewGuid():N}");
        var config = new SimulationConfig
        {
            OutputDirectory = "output",
            WriteDetailedCsv = true,
            Scenarios =
            [
                Scenario("Two", 2),
                Scenario("Three", 3),
                Scenario("Four", 4)
            ]
        };
        var metrics = Enum.GetValues<ResourceMetric>().ToDictionary(metric => metric, _ => 1);
        var results = config.Scenarios.Select((scenario, index) => new ScenarioRunResult(
            scenario,
            [new GameResult(index + 1, index + 10, true, GameEndReason.EmptyTreasureDeck, 12, 6,
                metrics, metrics, metrics, [new PlayerResult("Player 1", 5, 2)], ["Player 1"])])).ToArray();

        ReportWriter.Write(config, results, tempDirectory);

        var outputDirectory = Path.Combine(tempDirectory, "output");
        var summaryPath = Assert.Single(Directory.GetFiles(outputDirectory, "summary-*.csv"));
        var detailsPath = Assert.Single(Directory.GetFiles(outputDirectory, "games-*.csv"));
        Assert.Equal(
            Path.GetFileName(summaryPath)["summary-".Length..^4],
            Path.GetFileName(detailsPath)["games-".Length..^4]);

        var summaryLines = File.ReadAllLines(summaryPath);
        Assert.Equal(28, summaryLines.Length);
        Assert.Equal(
            "Scenario,Resource,CompletedGames,TruncatedGames,AllPlayersMean,AllPlayersP95,AllPlayersP99,AllPlayersP99.9,AllPlayersMaximum,SinglePlayerP99.9,SinglePlayerMaximum,Recommended,Baseline,Saving",
            summaryLines[0].TrimStart('\uFEFF'));
        Assert.DoesNotContain("Scope", string.Join('\n', summaryLines));
        Assert.DoesNotContain("Player:", string.Join('\n', summaryLines));

        var detailsHeader = File.ReadLines(detailsPath).First().TrimStart('\uFEFF');
        Assert.DoesNotContain("PlayerPeaks", detailsHeader);
        Assert.Contains("PeakTotal_ClearDiamond", detailsHeader);
        Assert.Contains("PeakSingle_ClearDiamond", detailsHeader);
    }

    private static ScenarioConfig Scenario(string name, int playerCount) => new()
    {
        Name = name,
        Players = Enumerable.Range(1, playerCount)
            .Select(index => new PlayerConfig { Name = $"Player {index}" })
            .ToList()
    };

    private static IReadOnlyList<string> ConfigFiles() =>
        ConfigFileSelector.DiscoverBundled(AppContext.BaseDirectory, Directory.GetCurrentDirectory());

    private static int CountOccurrences(string text, string value) =>
        (text.Length - text.Replace(value, string.Empty, StringComparison.Ordinal).Length) / value.Length;
}
