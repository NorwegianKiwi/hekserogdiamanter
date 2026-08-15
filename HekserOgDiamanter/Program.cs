using HekserOgDiamanter.Configuration;
using HekserOgDiamanter.Reporting;
using HekserOgDiamanter.Simulation;

try
{
    var configPath = ParseConfigPath(args);
    var fullConfigPath = Path.GetFullPath(configPath);
    var config = ConfigLoader.Load(fullConfigPath);
    Console.WriteLine($"Using configuration: {fullConfigPath}");
    Console.WriteLine($"Running {config.Scenarios.Count} scenario(s), {config.Scenarios.Sum(scenario => scenario.Games):N0} game(s)...");

    var started = DateTime.UtcNow;
    var results = SimulationRunner.Run(config);
    ReportWriter.Write(config, results, Path.GetDirectoryName(fullConfigPath) ?? Environment.CurrentDirectory);
    Console.WriteLine($"Completed in {(DateTime.UtcNow - started).TotalSeconds:F2} seconds.");
    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine($"Error: {exception.Message}");
    return 1;
}

static string ParseConfigPath(string[] arguments)
{
    if (arguments.Length == 0) return "simulation-config.json";
    if (arguments.Length == 2 && arguments[0] == "--config") return arguments[1];
    throw new ArgumentException("Usage: HekserOgDiamanter [--config <path>]");
}
