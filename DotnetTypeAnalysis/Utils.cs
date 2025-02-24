using NuGet.Common;
using NuGet.Protocol.Core.Types;
using System.IO;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DotnetTypeAnalysis;

internal static class Utils
{
    public static bool IsManagedAssembly(byte[] assemblyBytes)
    {
        using var peReader = new PEReader(ImmutableCollectionsMarshal.AsImmutableArray(assemblyBytes));
        return peReader.HasMetadata;
    }

    public static MetadataLoadContext CreateMetadataLoadContextWithAllAssembliesLoaded(IEnumerable<(string FileName, byte[] ImageBytes)> references)
    {
        var resolver = new BasicReferenceAssembliesResolver(references);
        var context = new MetadataLoadContext(resolver);

        foreach (var assemblyName in resolver.AvailableAssemblyNames)
            context.LoadFromAssemblyName(assemblyName);

        return context;
    }

    public static async Task<byte[]> ReadToEndAsync(this Stream stream, CancellationToken cancellationToken = default)
    {
        if (stream.CanSeek)
        {
            var finalArray = new byte[stream.Length];
            await stream.ReadExactlyAsync(finalArray, cancellationToken);
            return finalArray;
        }

        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, cancellationToken);
        return memoryStream.ToArray();
    }

    public static async IAsyncEnumerable<IPackageSearchMetadata> SearchMultiPagedAsync(
        this PackageSearchResource resource,
        string? searchTerm,
        SearchFilter filters,
        int skip,
        int take,
        ILogger log,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while (take > 0)
        {
            var page = await resource.SearchAsync(searchTerm, filters, skip, Math.Min(take, 1000), log, cancellationToken);

            foreach (var item in page)
                yield return item;

            skip += 1000;
            take -= 1000;
        }
    }
}
