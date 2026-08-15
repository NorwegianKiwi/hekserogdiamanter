namespace HekserOgDiamanter.Configuration;

public static class ConfigFileSelector
{
    public static IReadOnlyList<string> DiscoverBundled(string baseDirectory, string currentDirectory)
    {
        var candidates = new[]
        {
            Path.Combine(baseDirectory, "Configs"),
            Path.Combine(currentDirectory, "HekserOgDiamanter", "Configs"),
            Path.Combine(currentDirectory, "Configs")
        };

        var configDirectory = candidates.FirstOrDefault(Directory.Exists)
            ?? throw new DirectoryNotFoundException("Could not find the bundled Configs directory.");

        var files = Directory.GetFiles(configDirectory, "*.json")
            .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (files.Length == 0)
            throw new FileNotFoundException($"No JSON configuration files were found in '{configDirectory}'.");
        return files;
    }

    public static string SelectInteractive(
        IReadOnlyList<string> files,
        TextReader input,
        TextWriter output)
    {
        WriteList(files, output);
        while (true)
        {
            output.Write("Select configuration: ");
            var answer = input.ReadLine();
            if (answer is null)
                throw new EndOfStreamException("Input ended before a configuration was selected.");
            if (int.TryParse(answer, out var selection) && selection >= 1 && selection <= files.Count)
                return files[selection - 1];
            output.WriteLine($"Enter a number from 1 to {files.Count}.");
        }
    }

    public static void WriteList(IReadOnlyList<string> files, TextWriter output)
    {
        output.WriteLine("Available configurations:");
        for (var index = 0; index < files.Count; index++)
            output.WriteLine($"  {index + 1}. {Path.GetFileName(files[index])}");
    }
}
