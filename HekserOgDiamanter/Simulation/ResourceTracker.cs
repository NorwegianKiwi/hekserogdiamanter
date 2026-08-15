using HekserOgDiamanter.Domain;

namespace HekserOgDiamanter.Simulation;

public sealed class ResourceTracker
{
    public Dictionary<ResourceMetric, int> PeakTotal { get; } = NewMetricDictionary();
    public Dictionary<ResourceMetric, int> PeakSinglePlayer { get; } = NewMetricDictionary();
    public Dictionary<ResourceMetric, int> DistributedFromBoard { get; } = NewMetricDictionary();
    public Dictionary<string, Dictionary<ResourceMetric, int>> PeakByPlayer { get; } = new(StringComparer.Ordinal);

    public void Capture(IReadOnlyList<Player> players)
    {
        foreach (var metric in Enum.GetValues<ResourceMetric>())
        {
            var values = players.Select(player => ValueFor(player, metric)).ToArray();
            PeakTotal[metric] = Math.Max(PeakTotal[metric], values.Sum());
            PeakSinglePlayer[metric] = Math.Max(PeakSinglePlayer[metric], values.Max());
            foreach (var player in players)
            {
                if (!PeakByPlayer.TryGetValue(player.Name, out var playerPeaks))
                {
                    playerPeaks = NewMetricDictionary();
                    PeakByPlayer.Add(player.Name, playerPeaks);
                }
                playerPeaks[metric] = Math.Max(playerPeaks[metric], ValueFor(player, metric));
            }
        }
    }

    public void RecordDistribution(ResourceMetric metric, int count) =>
        DistributedFromBoard[metric] += count;

    public static int ValueFor(Player player, ResourceMetric metric)
    {
        var wallet = player.GetCoinWallet();
        return metric switch
        {
            ResourceMetric.ClearDiamond => player.ClearDiamonds,
            ResourceMetric.ColoredDiamond => player.ColoredDiamonds.Count,
            ResourceMetric.Gold => player.Gold,
            ResourceMetric.Pickaxe => player.Pickaxes,
            ResourceMetric.Shovel => player.Shovels,
            ResourceMetric.Money => player.Money,
            ResourceMetric.Coin1 => wallet.Ones,
            ResourceMetric.Coin5 => wallet.Fives,
            ResourceMetric.Coin10 => wallet.Tens,
            _ => throw new ArgumentOutOfRangeException(nameof(metric))
        };
    }

    private static Dictionary<ResourceMetric, int> NewMetricDictionary() =>
        Enum.GetValues<ResourceMetric>().ToDictionary(metric => metric, _ => 0);
}
