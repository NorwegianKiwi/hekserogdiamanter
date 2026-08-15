using HekserOgDiamanter.Configuration;
using HekserOgDiamanter.Reporting;
using HekserOgDiamanter.Simulation;

try
{
    var startup = StartupOptions.Parse(args);
    var bundledConfigs = startup.Mode is StartupMode.Interactive or StartupMode.ListConfigs
        ? ConfigFileSelector.DiscoverBundled(AppContext.BaseDirectory, Environment.CurrentDirectory)
        : null;

    if (startup.Mode == StartupMode.ListConfigs)
    {
        ConfigFileSelector.WriteList(bundledConfigs!, Console.Out);
        return 0;
    }

    string configPath;
    if (startup.Mode == StartupMode.Interactive)
    {
        if (Console.IsInputRedirected)
            throw new InvalidOperationException(
                "Interactive configuration selection is unavailable when input is redirected. " +
                "Use --config <path>, or use --list-configs to see the bundled choices.");
        configPath = ConfigFileSelector.SelectInteractive(bundledConfigs!, Console.In, Console.Out);
    }
    else
    {
        configPath = startup.ConfigPath!;
    }

    var fullConfigPath = Path.GetFullPath(configPath);
    var config = ConfigLoader.Load(fullConfigPath);
    Console.WriteLine($"Using configuration: {fullConfigPath}");
    Console.WriteLine($"Running {config.Scenarios.Count} scenario(s), {config.Scenarios.Sum(scenario => scenario.Games):N0} game(s)...");

    var started = DateTime.UtcNow;
    var results = SimulationRunner.Run(config);
    ReportWriter.Write(config, results, Environment.CurrentDirectory);
    Console.WriteLine($"Completed in {(DateTime.UtcNow - started).TotalSeconds:F2} seconds.");
    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine($"Error: {exception.Message}");
    return 1;
}
