namespace HekserOgDiamanter.Configuration;

public enum StartupMode
{
    Interactive,
    ListConfigs,
    DirectConfig
}

public sealed record StartupOptions(StartupMode Mode, string? ConfigPath = null)
{
    public static StartupOptions Parse(string[] arguments) => arguments switch
    {
        [] => new StartupOptions(StartupMode.Interactive),
        ["--list-configs"] => new StartupOptions(StartupMode.ListConfigs),
        ["--config", var path] => new StartupOptions(StartupMode.DirectConfig, path),
        _ => throw new ArgumentException("Usage: HekserOgDiamanter [--config <path> | --list-configs]")
    };
}
