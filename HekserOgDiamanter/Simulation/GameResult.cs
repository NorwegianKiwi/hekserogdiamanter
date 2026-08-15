using HekserOgDiamanter.Domain;

namespace HekserOgDiamanter.Simulation;

public sealed record PlayerResult(string Name, int Score, int Money);

public sealed record GameResult(
    int RunNumber,
    int Seed,
    bool Completed,
    GameEndReason EndReason,
    int Turns,
    int Rounds,
    IReadOnlyDictionary<ResourceMetric, int> PeakTotal,
    IReadOnlyDictionary<ResourceMetric, int> PeakSinglePlayer,
    IReadOnlyDictionary<ResourceMetric, int> DistributedFromBoard,
    IReadOnlyList<PlayerResult> Players,
    IReadOnlyList<string> Winners);
