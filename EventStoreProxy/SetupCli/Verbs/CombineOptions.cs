using CommandLine;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace EventStore.Setup.Verbs;

[Verb("combine-options", HelpText = "Combine YAML option files, taking the last value for each key")]
public class CombineOptions
{
    public CombineOptions(IEnumerable<string> files)
    {
        Files = files;
    }
    
    [property: Value(0, Min = 1, HelpText = "Filename(s) to combine. File may not exist.")]
    public IEnumerable<string> Files { get; }
    
    public void Combine(TextWriter output)
    {
        var combined = new Dictionary<string, YamlNode>(StringComparer.InvariantCultureIgnoreCase);
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(PascalCaseNamingConvention.Instance)
            .Build();
        foreach (var f in Files)
        {
            if (string.IsNullOrWhiteSpace(f)) continue;
            if (!File.Exists(f)) continue;
            var yaml = File.ReadAllText(f);
            if (string.IsNullOrWhiteSpace(yaml)) continue;
            var values = deserializer.Deserialize<Dictionary<string, YamlNode>>(yaml);
            foreach (var pair in values)
                combined[pair.Key] = pair.Value; //Add or update
        }
        if (combined.Count == 0)
        {
            output.WriteLine();
            return;
        }
        var serializer = new SerializerBuilder()
            .WithNamingConvention(PascalCaseNamingConvention.Instance)
            .Build();
        serializer.Serialize(output, combined);
    }
}