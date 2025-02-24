using System.Reflection;

namespace DotnetTypeAnalysis;

internal sealed class BasicReferenceAssembliesResolver : MetadataAssemblyResolver
{
    private readonly Dictionary<string, byte[]> imageBytesByAssemblyName = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<string> AvailableAssemblyNames => imageBytesByAssemblyName.Keys;

    public BasicReferenceAssembliesResolver(
        IEnumerable<(string FileName, byte[] ImageBytes)> references)
    {
        foreach (var reference in references)
        {
            if (!reference.FileName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Reference assembly file names are all expected to end with .dll.", nameof(references));

            if (!imageBytesByAssemblyName.TryAdd(reference.FileName[..^".dll".Length], reference.ImageBytes))
                throw new ArgumentException("Reference assemblies are expected to have unique names.", nameof(references));
        }
    }

    public override Assembly? Resolve(MetadataLoadContext context, AssemblyName assemblyName)
    {
        return assemblyName.Name is { } name && imageBytesByAssemblyName.TryGetValue(name, out var imageBytes)
            ? context.LoadFromByteArray(imageBytes)
            : null;
    }
}